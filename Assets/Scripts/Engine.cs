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

    public HashSet<int> WhitePawns   { get; private set; } = new HashSet<int> { 8,9,10,11,12,13,14,15 };
    public HashSet<int> WhiteRooks   { get; private set; } = new HashSet<int> { 0,7 };
    public HashSet<int> WhiteKnights { get; private set; } = new HashSet<int> { 1,6 };
    public HashSet<int> WhiteBishops { get; private set; } = new HashSet<int> { 2,5 };
    public HashSet<int> WhiteQueens  { get; private set; } = new HashSet<int> { 3 };
    public int WhiteKing             { get; private set; } =                    4;

    public HashSet<int> BlackPawns   { get; private set; } = new HashSet<int> { 48,49,50,51,52,53,54,55 };
    public HashSet<int> BlackRooks   { get; private set; } = new HashSet<int> { 56,63 };
    public HashSet<int> BlackKnights { get; private set; } = new HashSet<int> { 57,62 };
    public HashSet<int> BlackBishops { get; private set; } = new HashSet<int> { 58,61 };
    public HashSet<int> BlackQueens  { get; private set; } = new HashSet<int> { 59 };
    public int BlackKing             { get; private set; } =                    60;
    
    // allow for empty moves for analysis
    public enum MoveType : byte { None, Normal, Castle, EnPassant };
    public enum PieceType : byte { None, Pawn, Rook, Knight, Bishop, Queen, King };

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public bool WhiteMove { get; set; } = false;
        public bool CanCastle { get; set; } = true;
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
                CanCastle = CanCastle,
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

    // for checking puush
    private HashSet<int> whitePawnsInit, blackPawnsInit;
    // for checking blocks
    private HashSet<int> occupancy;
    // for checking captures
    private Dictionary<int, PieceType> whiteOccupancy, blackOccupancy;
    public Engine(int ranks=8, int files=8)
    {
        nRanks = ranks;
        nFiles = files;

        whitePawnsInit = new HashSet<int>(WhitePawns);
        blackPawnsInit = new HashSet<int>(BlackPawns);
        InitOccupancy();
        // previous = new Move(false, true, 0, 0,
                            // MoveType.None, PieceType.None, PieceType.None, PieceType.None);
        previous = new Move() {
            WhiteMove = false,
            CanCastle = true
        };
    }

    private void InitOccupancy()
    {
        whiteOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in WhitePawns)   whiteOccupancy[pos] = PieceType.Pawn;
        foreach (int pos in WhiteRooks)   whiteOccupancy[pos] = PieceType.Rook;
        foreach (int pos in WhiteKnights) whiteOccupancy[pos] = PieceType.Knight;
        foreach (int pos in WhiteBishops) whiteOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in WhiteQueens)  whiteOccupancy[pos] = PieceType.Queen;
        whiteOccupancy[WhiteKing] = PieceType.King;
        
        blackOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in BlackPawns)   blackOccupancy[pos] = PieceType.Pawn;
        foreach (int pos in BlackRooks)   blackOccupancy[pos] = PieceType.Rook;
        foreach (int pos in BlackKnights) blackOccupancy[pos] = PieceType.Knight;
        foreach (int pos in BlackBishops) blackOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in BlackQueens)  blackOccupancy[pos] = PieceType.Queen;
        blackOccupancy[BlackKing] = PieceType.King;

        occupancy = new HashSet<int>();
        occupancy.UnionWith(whiteOccupancy.Keys);
        occupancy.UnionWith(blackOccupancy.Keys);
    }

    private void AddPiece(PieceType type, int pos, bool white)
    {
        if (occupancy.Contains(pos))
            throw new Exception("occupado");
        if (type == PieceType.King)
            throw new Exception("2019");

        occupancy.Add(pos);
        if (white)
        {
            if (type == PieceType.Pawn) WhitePawns.Add(pos);
            else if (type == PieceType.Rook) WhiteRooks.Add(pos);
            else if (type == PieceType.Knight) WhiteKnights.Add(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Add(pos);
            else if (type == PieceType.Queen) WhiteQueens.Add(pos);
            whiteOccupancy.Add(pos, type);
        }
        else
        {
            if (type == PieceType.Pawn) BlackPawns.Add(pos);
            else if (type == PieceType.Rook) BlackRooks.Add(pos);
            else if (type == PieceType.Knight) BlackKnights.Add(pos);
            else if (type == PieceType.Bishop) BlackBishops.Add(pos);
            else if (type == PieceType.Queen) BlackQueens.Add(pos);
            blackOccupancy.Add(pos, type);
        }
    }
    private void RemovePiece(PieceType type, int pos, bool white)
    {
        if (!occupancy.Contains(pos))
            throw new Exception("no piece here");
        if (type == PieceType.King)
            throw new Exception("coup!");

        occupancy.Remove(pos);
        if (white)
        {
            if (type == PieceType.Pawn) WhitePawns.Remove(pos);
            else if (type == PieceType.Rook) WhiteRooks.Remove(pos);
            else if (type == PieceType.Knight) WhiteKnights.Remove(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Remove(pos);
            else if (type == PieceType.Queen) WhiteQueens.Remove(pos);
            whiteOccupancy.Remove(pos);
        }
        else
        {
            if (type == PieceType.Pawn) BlackPawns.Remove(pos);
            else if (type == PieceType.Rook) BlackRooks.Remove(pos);
            else if (type == PieceType.Knight) BlackKnights.Remove(pos);
            else if (type == PieceType.Bishop) BlackBishops.Remove(pos);
            else if (type == PieceType.Queen) BlackQueens.Remove(pos);
            blackOccupancy.Remove(pos);
        }
    }
    private void MoveKing(int pos, bool white)
    {
        if (occupancy.Contains(pos))
            throw new Exception("occupado");

        if (white)
        {
            occupancy.Remove(WhiteKing);
            WhiteKing = pos;
        }
        else
        {
            occupancy.Remove(BlackKing);
            BlackKing = pos;
        }
        occupancy.Add(pos);
    }
    private void PerformMove(Move move)
    {
        if (move.Moved == PieceType.Pawn)
        {
            RemovePiece(PieceType.Pawn, move.Source, move.WhiteMove);
            if (move.Captured != PieceType.None)
            {
                if (move.Type == MoveType.EnPassant)
                {
                    RemovePiece(PieceType.Pawn, move.Target-nFiles, !move.WhiteMove);
                }
                else
                {
                    RemovePiece(move.Captured, move.Target, !move.WhiteMove);
                }
            }
            if (move.Promotion != PieceType.None)
            {
                AddPiece(move.Promotion, move.Target, move.WhiteMove);
            }
            else
            {
                AddPiece(PieceType.Pawn, move.Target, move.WhiteMove);
            }
        }
        else if (move.Moved == PieceType.Knight)
        {
            RemovePiece(PieceType.Knight, move.Source, move.WhiteMove);
            if (move.Captured != PieceType.None)
            {
                RemovePiece(move.Captured, move.Target, !move.WhiteMove);
            }
            AddPiece(PieceType.Knight, move.Target, move.WhiteMove);
        }
        else
        {
            throw new NotImplementedException();
        }
        previous = move;
    }
    private void UndoMove(Move move)
    {
        // TODO:
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
