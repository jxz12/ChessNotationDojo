using System;
using System.Collections.Generic;
using System.Text;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game
// TODO: can also evaluate moves and play with other engines

public partial class Engine
{
    public static int nRanks = 8;
    public static int nFiles = 8;
    private Move previous { get; set; } = null;

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
    public int WhiteShortCastledKing { get; private set; } = 6;
    public int WhiteLongCastledKing  { get; private set; } = 2;
    public int BlackShortCastledKing { get; private set; } = 62;
    public int BlackLongCastledKing  { get; private set; } = 58;
    
    // allow for empty moves for analysis
    public enum MoveType : byte { Normal, Castle, EnPassant };
    public enum PieceType : byte { None, Pawn, Rook, Knight, Bishop, Queen, King };

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public bool WhiteMove { get; set; } = false;
        public int Source { get; set; } = 0;
        public int Target { get; set; } = 0;
        public MoveType Type { get; set; } = MoveType.Normal;
        public PieceType Moved { get; set; } = PieceType.None;
        public PieceType Captured { get; set; } = PieceType.None;
        public PieceType Promotion { get; set; } = PieceType.None;

        public Move DeepCopy()
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

    // for puush
    private HashSet<int> whitePawnsInit, blackPawnsInit;
    // for castling
    private HashSet<int> castlePiecesInit;

    // for checking blocks
    private HashSet<int> occupancy;
    // for checking captures
    private Dictionary<int, PieceType> whiteOccupancy, blackOccupancy;

    // attack tables
    private int[] whiteAttackTable;
    private int[] blackAttackTable;

    public Engine(int ranks=8, int files=8)
    {
        nRanks = ranks;
        nFiles = files;

        InitOccupancy();
        InitAttackTables();

        previous = new Move();
    }


    ///////////////////////////////////
    // for interface from the outside

    private string GetAlgebraic(Move move)
    {
        var sb = new StringBuilder();
        if (move.Type == MoveType.Castle)
        {
            if (move.Target > move.Source) sb.Append('>');
            else sb.Append('<');
        }
        else if (move.Moved == PieceType.Pawn)
        {
            sb.Append((char)('a'+(move.Source%nFiles)));
            if (move.Captured != PieceType.None)
            {
                sb.Append('x').Append((char)('a'+(move.Target%nFiles)));
            }
            sb.Append(move.Target/nFiles + 1);
            if (move.Promotion != PieceType.None)
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
            if (move.Moved == PieceType.Rook) sb.Append('R');
            else if (move.Moved == PieceType.Knight) sb.Append('N');
            else if (move.Moved == PieceType.Bishop) sb.Append('B');
            else if (move.Moved == PieceType.Queen) sb.Append('Q');
            else if (move.Moved == PieceType.King) sb.Append('K');

            // TODO: Source ambiguity
            if (move.Captured != PieceType.None) sb.Append('x');

            sb.Append((char)('a'+(move.Target%nFiles)));
            sb.Append(move.Target/nFiles + 1);
        }
        return sb.ToString();
    }

    public IEnumerable<string> GetMovesAlgebraic()
    {
        var moves = GenerateMoves();
        foreach (Move move in moves)
        {
            yield return GetAlgebraic(move);
        }
    }

    public void PerformMoveAlgebraic(string todo)
    {
        var moves = GenerateMoves();
        foreach (Move move in moves)
        {
            if (GetAlgebraic(move) == todo)
            {
                PerformMove(move);
                return;
            }
        }
    }
    public void UndoLastMove()
    {
        // TODO:
    }
}
