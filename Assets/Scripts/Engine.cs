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
    public HashSet<int> WhitePawns   { get; private set; } = new HashSet<int>();
    public HashSet<int> WhiteRooks   { get; private set; } = new HashSet<int>();
    public HashSet<int> WhiteKnights { get; private set; } = new HashSet<int>();
    public HashSet<int> WhiteBishops { get; private set; } = new HashSet<int>();
    public HashSet<int> WhiteQueens  { get; private set; } = new HashSet<int>();
    public int WhiteKing             { get; private set; } = -1;

    public HashSet<int> BlackPawns   { get; private set; } = new HashSet<int>();
    public HashSet<int> BlackRooks   { get; private set; } = new HashSet<int>();
    public HashSet<int> BlackKnights { get; private set; } = new HashSet<int>();
    public HashSet<int> BlackBishops { get; private set; } = new HashSet<int>();
    public HashSet<int> BlackQueens  { get; private set; } = new HashSet<int>();
    public int BlackKing             { get; private set; } = -1;

    // for starting rank of pawns
    public int WhitePawnStartingRank { get; private set; }
    public int BlackPawnStartingRank { get; private set; }
    // for where castled kings go
    public int WhiteLeftCastledFile  { get; private set; } 
    public int WhiteRightCastledFile { get; private set; }
    public int BlackLeftCastledFile  { get; private set; }
    public int BlackRightCastledFile { get; private set; }
    
    public enum MoveType { None=0, Normal, Castle, EnPassant };
    public enum PieceType { None=0, Pawn, Rook, Knight, Bishop, Queen, King,
                                           VirginPawn, VirginRook, VirginKing };
                                           // for castling, en passant etc.
    // for checking captures
    private Dictionary<int, PieceType> whiteOccupancy, blackOccupancy;

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    // therefore acts like a LinkedList
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
                Previous = Previous,
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

    // current to evaluate
    private Move prevMove;
    Dictionary<string, Move> legalMoves;

    public Engine(int ranks=8, int files=8,
                  string sFEN="rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w AHah - 0 1",
                  int whitePawnStartingRank=1, int blackPawnStartingRank=6,
                  int whiteLeftCastledFile=2, int blackLeftCastledFile=2,
                  int whiteRightCastledFile=6, int blackRightCastledFile=6)
    {
        if (ranks > 16 || files > 16) // ascii bleeds into letters otherwise!
            throw new Exception("only up to 16x16 boards are supported. If you want more then you're crazy.");

        nRanks = ranks;
        nFiles = files;

        int rank = nRanks-1;
        int file = -1;
        int i = 0;
        while (sFEN[i] != ' ')
        {
            file += 1;
            if (sFEN[i] == '/')
            {
                if (file != nFiles)
                    throw new Exception("wrong number of squares in FEN rank " + rank);

                rank -= 1;
                file = -1;
            }
            else if (sFEN[i] == 'P') WhitePawns.Add(GetPos(rank, file));
            else if (sFEN[i] == 'R') WhiteRooks.Add(GetPos(rank, file));
            else if (sFEN[i] == 'N') WhiteKnights.Add(GetPos(rank, file));
            else if (sFEN[i] == 'B') WhiteBishops.Add(GetPos(rank, file));
            else if (sFEN[i] == 'Q') WhiteQueens.Add(GetPos(rank, file));
            else if (sFEN[i] == 'K') WhiteKing = GetPos(rank, file); // TODO: check duplicate
            else if (sFEN[i] == 'p') BlackPawns.Add(GetPos(rank, file));
            else if (sFEN[i] == 'r') BlackRooks.Add(GetPos(rank, file));
            else if (sFEN[i] == 'n') BlackKnights.Add(GetPos(rank, file));
            else if (sFEN[i] == 'b') BlackBishops.Add(GetPos(rank, file));
            else if (sFEN[i] == 'q') BlackQueens.Add(GetPos(rank, file));
            else if (sFEN[i] == 'k') BlackKing = GetPos(rank, file); // TODO: check duplicate
            else // blank, so assume number
            {
                file += sFEN[i] - '1'; // -1 because file will be incremented regardless
            }
            i += 1;
        }
        // TODO: rest of notation, virgins

        WhitePawnStartingRank = whitePawnStartingRank;
        WhiteLeftCastledFile  = whiteLeftCastledFile;
        WhiteRightCastledFile = whiteRightCastledFile;
        BlackPawnStartingRank = blackPawnStartingRank;
        BlackLeftCastledFile  = blackLeftCastledFile;
        BlackRightCastledFile = blackRightCastledFile;

        InitOccupancy();
        prevMove = new Move(); // TODO: change for enpassant

        CheckLegality();
        legalMoves = FindLegalMoves(prevMove);
    }

    private void InitOccupancy()
    {
        whiteOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in WhitePawns)   whiteOccupancy[pos] = PieceType.VirginPawn;
        // foreach (int pos in WhitePawns)   whiteOccupancy[pos] = pos>15?PieceType.Pawn:PieceType.VirginPawn;
        foreach (int pos in WhiteRooks)   whiteOccupancy[pos] = PieceType.VirginRook;
        foreach (int pos in WhiteKnights) whiteOccupancy[pos] = PieceType.Knight;
        foreach (int pos in WhiteBishops) whiteOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in WhiteQueens)  whiteOccupancy[pos] = PieceType.Queen;
        whiteOccupancy[WhiteKing] = PieceType.VirginKing;
        
        blackOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in BlackPawns)   blackOccupancy[pos] = PieceType.VirginPawn;
        // foreach (int pos in BlackPawns)   blackOccupancy[pos] = pos<48?PieceType.Pawn:PieceType.VirginPawn;
        foreach (int pos in BlackRooks)   blackOccupancy[pos] = PieceType.VirginRook;
        foreach (int pos in BlackKnights) blackOccupancy[pos] = PieceType.Knight;
        foreach (int pos in BlackBishops) blackOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in BlackQueens)  blackOccupancy[pos] = PieceType.Queen;
        blackOccupancy[BlackKing] = PieceType.VirginKing;
    }
    private void CheckLegality()
    {
        UnityEngine.Debug.Log("TODO: check multiple occupations, if in check, if pawns are behind the starting square, ");
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
        return sb.ToString();
    }
    private Dictionary<string, Move> FindLegalMoves(Move prev)
    {
        var nexts = new List<Move>(FindPseudoLegalMoves(prev));
        var ambiguous = new Dictionary<string, List<Move>>();

        foreach (Move next in nexts)
        {
            PlayMove(next);
            var nextnexts = FindPseudoLegalMoves(next);

            // if legal, add to list
            if (!InCheck(next, nextnexts))
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
            UndoMove(next);
        }
        var unambiguous = new Dictionary<string, Move>();
        foreach (string algebraic in ambiguous.Keys)
        {
            if (ambiguous[algebraic].Count == 1) // already unambiguous
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
    public void PlayMoveAlgebraic(string algebraic)
    {
        Move toPlay;
        if (legalMoves.TryGetValue(algebraic, out toPlay))
        {
            PlayMove(toPlay);
            prevMove = toPlay;
            legalMoves = FindLegalMoves(prevMove);
        }
        else
        {
            throw new Exception("move not legal");
        }
    }
    // returns best move algebraic and evaluation
    public Tuple<string, float> EvaluateBestMove(int ply)
    {
        float bestEval = float.MinValue;
        string bestAlgebraic = null;
        foreach (string algebraic in legalMoves.Keys)
        {
            float eval = NegaMax(legalMoves[algebraic], ply);
            if (eval > bestEval)
            {
                bestEval = eval;
                bestAlgebraic = algebraic;
            }
        }
        return Tuple.Create(bestAlgebraic, bestEval);
    }
    public void UndoLastMove()
    {
        if (prevMove != null)
        {
            UndoMove(prevMove);
            prevMove = prevMove.Previous;
            legalMoves = FindLegalMoves(prevMove);
        }
        else
        {
            throw new Exception("no moves played yet");
        }
    }
}
