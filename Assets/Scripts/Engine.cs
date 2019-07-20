using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game
// TODO: can also evaluate moves and play with other engines

public partial class Engine
{
    public static int nRanks = 8;
    public static int nFiles = 8;

    // board state
    public HashSet<int> WhitePawns   { get; private set; } = new HashSet<int> { 8,9,10,11,12,13,14,15 };
    public HashSet<int> WhiteRooks   { get; private set; } = new HashSet<int> { 0,7 };
    public HashSet<int> WhiteKnights { get; private set; } = new HashSet<int> { 1,6 };
    public HashSet<int> WhiteBishops { get; private set; } = new HashSet<int> { 2,5 };
    public HashSet<int> WhiteQueens  { get; private set; } = new HashSet<int> { 3 };

    public HashSet<int> BlackPawns   { get; private set; } = new HashSet<int> { 48,49,50,51,52,53,54,55 };
    public HashSet<int> BlackRooks   { get; private set; } = new HashSet<int> { 56,63 };
    public HashSet<int> BlackKnights { get; private set; } = new HashSet<int> { 57,62 };
    public HashSet<int> BlackBishops { get; private set; } = new HashSet<int> { 58,61 };
    public HashSet<int> BlackQueens  { get; private set; } = new HashSet<int> { 59 };

    public int WhiteKing             { get; private set; } = 4;
    public int BlackKing             { get; private set; } = 60;
    // for where castled kings go
    public int WhiteLeftCastledFile  { get; private set; } = 2;
    public int WhiteRightCastledFile { get; private set; } = 6;
    public int BlackLeftCastledFile  { get; private set; } = 2;
    public int BlackRightCastledFile { get; private set; } = 6;
    
    public enum MoveType : byte { None, Normal, Castle, EnPassant };
    public enum PieceType : byte { None, Pawn, Rook, Knight, Bishop, Queen, King,
                                         VirginPawn, VirginRook, VirginKing }; // for castling, en passant etc.

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

    // top of game tree
    private Move root;
    // current to evaluate
    private Move current;

    public Engine(int ranks=8, int files=8)
    {
        nRanks = ranks;
        nFiles = files;

        InitOccupancy();

        root = new Move();
        current = root;
    }
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

    // for bishops, rooks, queens
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
    // for knights, kings, pawns (one move)
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
    private PieceType AllyRaycast(int source, int fileSlide, int rankSlide, bool whiteToMove)
    {
        int startFile = GetFile(source);
        int startRank = GetRank(source);
        int targetFile = startFile + fileSlide;
        int targetRank = startRank + rankSlide;
        int targetPos = GetPos(targetFile, targetRank);

        while (targetFile >= 0 && targetFile < nFiles &&
               targetRank >= 0 && targetRank < nRanks)
        {
            if (Occupied(targetPos))
            {
                var allies = whiteToMove? whiteOccupancy
                                        : blackOccupancy;
                return allies.ContainsKey(targetPos)
                        ? allies[targetPos]
                        : PieceType.None;
            }
            targetFile += fileSlide;
            targetRank += rankSlide;
            targetPos = GetPos(targetFile, targetRank);
        }
        return PieceType.None;
    }
    // private bool FileRangeFree(int fileMin, int fileMax, int rank)
    // {
    //     return true;
    // }

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

            // TODO: Source ambiguity
            if (move.Captured != PieceType.None) sb.Append('x');

            sb.Append((char)('a'+(move.Target%nFiles)));
            sb.Append(move.Target/nFiles + 1);
        }
        // UnityEngine.Debug.Log(sb.ToString());
        return sb.ToString();
    }
    public IEnumerable<string> GetLegalMovesAlgebraic()
    {
        // check if legal
        var nextMoves = new List<Move>(PseudoLegalMoves(current));
        foreach (Move next in nextMoves)
        {
            // UnityEngine.Debug.Log("n: " + Algebraic(next));
            PlayMove(next);
            bool legal = true;
            foreach (Move nextnext in PseudoLegalMoves(next))
            {
                // UnityEngine.Debug.Log("nn: " + Algebraic(nextnext));
                // TODO: castling
                if (nextnext.Captured == PieceType.King
                    || nextnext.Captured == PieceType.VirginKing)
                {
                    // UnityEngine.Debug.Log("NO");
                    legal = false;
                    break;
                }
            }
            if (legal)
            {
                yield return Algebraic(next);
            }
            UndoMove(next);
        }
    }

    // returns if check
    // may perform illegal move if asked to!
    public bool PlayMoveAlgebraic(string todo)
    {
        foreach (Move next in PseudoLegalMoves(current))
        {
            // UnityEngine.Debug.Log(Algebraic(next));
            if (Algebraic(next) == todo)
            {
                PlayMove(next);
                current = next;
                return Check();
            }
        }
        throw new Exception("No moves possible!");
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
