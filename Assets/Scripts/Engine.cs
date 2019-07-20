using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game
// TODO: can also evaluate moves and play with other engines

public partial class Engine
{
    public int nRanks { get; private set; } = 8;
    public int nFiles { get; private set; } = 8;

    // board state
    public HashSet<int> WhitePawns   { get; private set; } = new HashSet<int> { 8,9,10,11,12,13,14,15 };
    public HashSet<int> WhiteRooks   { get; private set; } = new HashSet<int> { 0,7 };
    public HashSet<int> WhiteKnights { get; private set; } = new HashSet<int> { 1,6 };
    public HashSet<int> WhiteBishops { get; private set; } = new HashSet<int> { 2,5 };
    public HashSet<int> WhiteQueens  { get; private set; } = new HashSet<int> { 3 };
    public int WhiteKing             { get; private set; } = 4;

    public HashSet<int> BlackPawns   { get; private set; } = new HashSet<int> { 48,49,50,51,52,53,54,55 };
    public HashSet<int> BlackRooks   { get; private set; } = new HashSet<int> { 56,63 };
    public HashSet<int> BlackKnights { get; private set; } = new HashSet<int> { 57,62 };
    public HashSet<int> BlackBishops { get; private set; } = new HashSet<int> { 58,61 };
    public HashSet<int> BlackQueens  { get; private set; } = new HashSet<int> { 59 };
    public int BlackKing             { get; private set; } = 60;

    // for where castled kings go
    public int WhiteLeftCastledFile  { get; private set; } = 2;
    public int WhiteRightCastledFile { get; private set; } = 6;
    public int BlackLeftCastledFile  { get; private set; } = 2;
    public int BlackRightCastledFile { get; private set; } = 6;
    
    public enum MoveType { None=0, Normal, Castle, EnPassant };
    public enum PieceType { None=0, Pawn, Rook, Knight, Bishop, Queen, King,
                                           VirginPawn, VirginRook, VirginKing };
                                           // for castling, en passant etc.

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public Move Previous = null;
        public bool WhiteMove { get; set; } = false;
        public int Source { get; set; } = 0;
        public int Target { get; set; } = 0;
        public MoveType Type { get; set; } = MoveType.None;
        public PieceType Moved { get; set; } = PieceType.None;
        public PieceType Captured { get; set; } = PieceType.None;
        public PieceType Promotion { get; set; } = PieceType.None;

        public Move DeepCopy() // to make promotion simpler
        {
            var copy = new Move() {
                WhiteMove = WhiteMove,
                Source = Source,
                Target = Target,
                Type = Type,
                Moved = Moved,
                Captured = Captured,
                Promotion = Promotion
            };
            return copy;
        }
    }

    // for checking captures
    private Dictionary<int, PieceType> whiteOccupancy, blackOccupancy;

    // current to evaluate
    private Move current;
    Dictionary<string, Move> legalMoves;

    public Engine()
    {
        InitOccupancy();

        current = new Move();
        legalMoves = FindLegalMoves(current);
    }
    // for other game types
    // public Engine(int ranks, int files,
    //               IEnumerable<int> whitePawns, IEnumerable<int> blackPawns,
    //               IEnumerable<int> whiteRooks, IEnumerable<int> blackRooks,
    //               IEnumerable<int> whiteKnights, IEnumerable<int> blackKnights,
    //               IEnumerable<int> whiteBishops, IEnumerable<int> blackBishops,
    //               IEnumerable<int> whiteQueens, IEnumerable<int> blackQueens,
    //               int whiteKing, int blackKing,
    //               int whiteLeftCastledFile, int blackLeftCastledFile,
    //               int whiteRightCastledFile, int blackRightCastledFile)
    // {
    //     nRanks = ranks;
    //     nFiles = files;
    //     WhitePawns = new HashSet<int>(whitePawns);
    //     BlackPawns = new HashSet<int>(blackPawns);
    // }

    private void InitOccupancy()
    {
        whiteOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in WhitePawns)   whiteOccupancy[pos] = PieceType.VirginPawn;
        foreach (int pos in WhiteRooks)   whiteOccupancy[pos] = PieceType.VirginRook;
        foreach (int pos in WhiteKnights) whiteOccupancy[pos] = PieceType.Knight;
        foreach (int pos in WhiteBishops) whiteOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in WhiteQueens)  whiteOccupancy[pos] = PieceType.Queen;
        whiteOccupancy[WhiteKing] = PieceType.VirginKing;
        
        blackOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in BlackPawns)   blackOccupancy[pos] = PieceType.VirginPawn;
        foreach (int pos in BlackRooks)   blackOccupancy[pos] = PieceType.VirginRook;
        foreach (int pos in BlackKnights) blackOccupancy[pos] = PieceType.Knight;
        foreach (int pos in BlackBishops) blackOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in BlackQueens)  blackOccupancy[pos] = PieceType.Queen;
        blackOccupancy[BlackKing] = PieceType.VirginKing;
    }
    
    // convenience functions
    private int GetFile(int pos)
    {
        return pos % nFiles;
    }
    private int GetRank(int pos)
    {
        return pos / nFiles;
    }
    private int GetPos(int file, int rank)
    {
        return rank * nFiles + file;
    }
    private bool Occupied(int pos)
    {
        return whiteOccupancy.ContainsKey(pos) || blackOccupancy.ContainsKey(pos);
    }

    // for finding candidate bishop, rook, queen moves
    private IEnumerable<int> SliderAttacks(int slider, int fileSlide, int rankSlide, bool whiteToMove)
    {
        int startFile = GetFile(slider);
        int startRank = GetRank(slider);
        int targetFile = startFile + fileSlide;
        int targetRank = startRank + rankSlide;
        int targetPos = GetPos(targetFile, targetRank);

        while (targetFile >= 0 && targetFile < nFiles &&
               targetRank >= 0 && targetRank < nRanks &&
               !Occupied(targetPos))
        {
            yield return targetPos;
            targetFile += fileSlide;
            targetRank += rankSlide;
            targetPos = GetPos(targetFile, targetRank);
        }

        if (targetFile >= 0 && targetFile < nFiles &&
            targetRank >= 0 && targetRank < nRanks &&
            (whiteToMove? blackOccupancy.ContainsKey(targetPos)
                        : whiteOccupancy.ContainsKey(targetPos)))
        {
            yield return targetPos;
        }
    }
    private IEnumerable<int> BishopAttacks(int bishop, bool whiteToMove)
    {
        return         SliderAttacks(bishop,  1,  1, whiteToMove)
               .Concat(SliderAttacks(bishop,  1, -1, whiteToMove))
               .Concat(SliderAttacks(bishop, -1, -1, whiteToMove))
               .Concat(SliderAttacks(bishop, -1,  1, whiteToMove));
    }
    private IEnumerable<int> RookAttacks(int rook, bool whiteToMove)
    {
        return         SliderAttacks(rook,  0,  1, whiteToMove)
               .Concat(SliderAttacks(rook,  1,  0, whiteToMove))
               .Concat(SliderAttacks(rook,  0, -1, whiteToMove))
               .Concat(SliderAttacks(rook, -1,  0, whiteToMove));
    }
    private IEnumerable<int> QueenAttacks(int queen, bool whiteToMove)
    {
        return       BishopAttacks(queen, whiteToMove)
               .Concat(RookAttacks(queen, whiteToMove));
    }
    // for candidate knight, king, pawn moves
    private IEnumerable<int> HopperAttack(int hopper, int fileHop, int rankHop, bool whiteToMove)
    {
        int startFile = GetFile(hopper);
        int startRank = GetRank(hopper);
        int targetFile = startFile + fileHop;
        int targetRank = startRank + rankHop;
        int targetPos = GetPos(targetFile, targetRank);

        if (targetFile >= 0 && targetFile < nFiles &&
            targetRank >= 0 && targetRank < nRanks &&
            (whiteToMove? !whiteOccupancy.ContainsKey(targetPos)
                        : !blackOccupancy.ContainsKey(targetPos)))
            // only blocked by own pieces
        {
            yield return targetPos;
        }
    }
    private IEnumerable<int> PawnAttacks(int pawn, bool whiteToMove)
    {
        if (whiteToMove)
        {
            return         HopperAttack(pawn,  1, 1, whiteToMove)
                   .Concat(HopperAttack(pawn, -1, 1, whiteToMove));
        }
        else
        {
            return         HopperAttack(pawn,  1, -1, whiteToMove)
                   .Concat(HopperAttack(pawn, -1, -1, whiteToMove));
        }
    }
    private IEnumerable<int> KnightAttacks(int knight, bool whiteToMove)
    {
        return         HopperAttack(knight,  1,  2, whiteToMove)
               .Concat(HopperAttack(knight,  2,  1, whiteToMove))
               .Concat(HopperAttack(knight,  2, -1, whiteToMove))
               .Concat(HopperAttack(knight,  1, -2, whiteToMove))
               .Concat(HopperAttack(knight, -1, -2, whiteToMove))
               .Concat(HopperAttack(knight, -2, -1, whiteToMove))
               .Concat(HopperAttack(knight, -2,  1, whiteToMove))
               .Concat(HopperAttack(knight, -1,  2, whiteToMove));
    }
    private IEnumerable<int> KingAttacks(int king, bool whiteToMove)
    {
        return         HopperAttack(king,  0,  1, whiteToMove)
               .Concat(HopperAttack(king,  1,  1, whiteToMove))
               .Concat(HopperAttack(king,  1,  0, whiteToMove))
               .Concat(HopperAttack(king,  1, -1, whiteToMove))
               .Concat(HopperAttack(king,  0, -1, whiteToMove))
               .Concat(HopperAttack(king, -1,  1, whiteToMove))
               .Concat(HopperAttack(king, -1,  0, whiteToMove))
               .Concat(HopperAttack(king, -1, -1, whiteToMove));
    }

    // all for castling
    private int FindVirginRook(int source, bool left, bool whiteToMove)
    {
        int startFile = GetFile(source);
        int startRank = GetRank(source);
        int fileSlide = left? -1 : 1;
        int targetFile = startFile + fileSlide;
        int targetPos = GetPos(targetFile, startRank);

        while (targetFile >= 0 && targetFile < nFiles)
        {
            if (Occupied(targetPos))
            {
                var allies = whiteToMove? whiteOccupancy
                                        : blackOccupancy;
                PieceType ally;
                allies.TryGetValue(targetPos, out ally);
                return ally==PieceType.VirginRook? targetPos 
                                                 : -1;
            }
            targetFile += fileSlide;
            targetPos = GetPos(targetFile, startRank);
        }
        return -1;
    }
    private int GetCastledPos(int king, bool left, bool whiteToMove)
    {
        int rank = GetRank(king);
        int file;
        if (whiteToMove)
        {
            if (left) file = WhiteLeftCastledFile;
            else      file = WhiteRightCastledFile;
        }
        else
        {
            if (left) file = BlackLeftCastledFile;
            else      file = BlackRightCastledFile;
        }
        return GetPos(file, rank);
    }
    private bool FindCastlingRook(int king, bool left, bool whiteToMove, out int virginRook)
    {
        virginRook = FindVirginRook(king, left, whiteToMove);
        if (virginRook < 0)
            return false;
        int castledPos = GetCastledPos(king, left, whiteToMove);
        if (castledPos < 0)
            return false;
        
        // check whether all squares involved are free
        int leftmostPos = left? Math.Min(virginRook, castledPos)
                              : Math.Min(castledPos-1, king);
        int rightmostPos = left? Math.Max(castledPos+1, king)
                               : Math.Max(virginRook, castledPos);

        var allies = whiteToMove? whiteOccupancy : blackOccupancy;
        for (int pos=leftmostPos; pos<=rightmostPos; pos++)
        {
            if (pos!=king && pos!=virginRook && Occupied(pos))
                return false;
        }
        return true;
    }

    ///////////////////////////////////
    // for interface from the outside

    private string Algebraic(Move move)
    {
        var sb = new StringBuilder();
        if (move.Type == MoveType.Castle)
        {
            if (move.Target > move.Source) sb.Append('>');
            else sb.Append('<');
        }
        else if (move.Moved == PieceType.Pawn
                 || move.Moved == PieceType.VirginPawn)
        {
            sb.Append((char)('a'+(move.Source%nFiles)));
            if (move.Captured != PieceType.None)
            {
                sb.Append('x').Append((char)('a'+(move.Target%nFiles)));
            }
            sb.Append(move.Target/nFiles + 1);
            if (move.Promotion != PieceType.None
                && move.Promotion != PieceType.Pawn)
            {
                sb.Append('=');
                if (move.Promotion == PieceType.Rook) sb.Append('R');
                else if (move.Promotion == PieceType.Knight) sb.Append('N');
                else if (move.Promotion == PieceType.Bishop) sb.Append('B');
                else if (move.Promotion == PieceType.Queen) sb.Append('Q');
            }
        }
        else
        {
            if (move.Moved == PieceType.Rook
                || move.Moved == PieceType.VirginRook) sb.Append('R');
            else if (move.Moved == PieceType.Knight) sb.Append('N');
            else if (move.Moved == PieceType.Bishop) sb.Append('B');
            else if (move.Moved == PieceType.Queen) sb.Append('Q');
            else if (move.Moved == PieceType.King
                     || move.Moved == PieceType.VirginKing) sb.Append('K');

            if (move.Captured != PieceType.None) sb.Append('x');

            sb.Append((char)('a'+(move.Target%nFiles)));
            sb.Append(move.Target/nFiles + 1);
        }
        // UnityEngine.Debug.Log(sb.ToString());
        return sb.ToString();
    }
    private Dictionary<string, Move> FindLegalMoves(Move previous)
    {
        var pseudolegal = new List<Move>(PseudoLegalMoves(current));
        var ambiguous = new Dictionary<string, List<Move>>();
        // check if legal
        foreach (Move next in pseudolegal)
        {
            PlayMove(next);
            bool legal = true;

            if (next.Type != MoveType.Castle)
            {
                // simply check for king being captured
                foreach (Move nextnext in PseudoLegalMoves(next))
                {
                    if (nextnext.Captured == PieceType.King
                        || nextnext.Captured == PieceType.VirginKing)
                    {
                        legal = false;
                        break;
                    }
                }
            }
            else // need to check all squares moved through
            {
                var kingSquares = new HashSet<int>();
                int kingBefore = next.Source;
                int kingAfter = next.WhiteMove? WhiteKing : BlackKing;

                if (kingBefore > kingAfter) // move left
                {
                    for (int pos=kingBefore; pos>kingAfter; pos--)
                        kingSquares.Add(pos);
                }
                else
                {
                    for (int pos=kingBefore; pos<kingAfter; pos++)
                        kingSquares.Add(pos);
                }
                foreach (Move nextnext in PseudoLegalMoves(next))
                {
                    if (nextnext.Captured == PieceType.King
                        || nextnext.Captured == PieceType.VirginKing
                        || (next.Type == MoveType.Castle
                            && kingSquares.Contains(nextnext.Target)))
                    {
                        legal = false;
                        break;
                    }
                }
            }
            UndoMove(next);

            if (legal)
            {
                string algebraic = Algebraic(next);
                if (ambiguous.ContainsKey(algebraic))
                {
                    ambiguous[algebraic].Add(next);
                }
                else
                {
                    ambiguous[algebraic] = new List<Move> { next };
                }
            }
        }
        var unambiguous = new Dictionary<string, Move>();
        foreach (string algebraic in ambiguous.Keys)
        {
            if (ambiguous[algebraic].Count == 1) // unambiguous
            {
                unambiguous[algebraic] = ambiguous[algebraic][0];
            }
            else
            {
                // ambiguous
                foreach (Move move in ambiguous[algebraic])
                {
                    // check which coordinates (file/rank) clash
                    int file = GetFile(move.Source);
                    int rank = GetRank(move.Source);
                    bool repeatFile = false;
                    bool repeatRank = false;
                    foreach (Move clash in ambiguous[algebraic].Where(x=> x!=move))
                    {
                        repeatFile |= file == GetFile(clash.Source);
                        repeatRank |= rank == GetRank(clash.Source);
                    }
                    // if no shared file, use file
                    string disambiguated;
                    if (!repeatFile)
                    {
                        disambiguated = algebraic.Insert(1, ((char)('a'+file)).ToString());
                    }
                    else if (!repeatRank) // use rank
                    {
                        disambiguated = algebraic.Insert(1, ((char)('1'+rank)).ToString());
                    }
                    else // use both
                    {
                        disambiguated = algebraic.Insert(1, ((char)('a'+file)).ToString()
                                                            + ((char)('1'+rank)).ToString());
                    }
                    unambiguous[disambiguated] = move;
                }
            }
        }
        return unambiguous;
    }
    public IEnumerable<string> GetLegalMovesAlgebraic()
    {
        return legalMoves.Keys;
    }

    // returns if check
    public bool PlayMoveAlgebraic(string algebraic)
    {
        Move next;
        if (legalMoves.TryGetValue(algebraic, out next))
        {
            PlayMove(next);
            current = next;
            legalMoves = FindLegalMoves(current);
            return Check();
        }
        else
        {
            throw new Exception("move not evaluated as legal");
        }
    }
    private bool Check()
    {
        // temporarily assume empty move for black
        Move empty = new Move() { WhiteMove = !current.WhiteMove };
        foreach (Move checkTest in PseudoLegalMoves(empty))
        {
            if (checkTest.Captured == PieceType.King)
            {
                return true;
            }
        }
        return false;
    }
    public void UndoLastMove()
    {
        UndoMove(current);
        current = current.Previous;
    }
}
