using System;
using System.Collections.Generic;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game
// TODO: can also evaluate moves and play with other engines

public class Engine
{
    public Engine(int ranks=8, int files=8)
    {
        nRanks = ranks;
        nFiles = files;
    }

    public static int nRanks = 8;
    public static int nFiles = 8;
    private Move previous { get; set; } = null;

    public HashSet<int> WhitePawns   { get; private set; } = new HashSet<int> { 8,9,10,11,12,13,14,15 };
    public HashSet<int> WhiteRooks   { get; private set; } = new HashSet<int> { 0,7 };
    public HashSet<int> WhiteKnights { get; private set; } = new HashSet<int> { 1,6 };
    public HashSet<int> WhiteBishops { get; private set; } = new HashSet<int> { 2,5 };
    public HashSet<int> WhiteQueens  { get; private set; } = new HashSet<int> { 3 };
    public HashSet<int> WhiteKings   { get; private set; } = new HashSet<int> { 4 };

    public HashSet<int> BlackPawns   { get; private set; } = new HashSet<int> { 48,49,50,51,52,53,54,55 };
    public HashSet<int> BlackRooks   { get; private set; } = new HashSet<int> { 56,63 };
    public HashSet<int> BlackKnights { get; private set; } = new HashSet<int> { 57,62 };
    public HashSet<int> BlackBishops { get; private set; } = new HashSet<int> { 58,61 };
    public HashSet<int> BlackQueens  { get; private set; } = new HashSet<int> { 59 };
    public HashSet<int> BlackKings   { get; private set; } = new HashSet<int> { 60 };

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public bool whiteMove;

        public enum PieceType : byte { Pawn, Rook, Knight, Bishop, Queen, King };
        public PieceType Piece { get; set; }
        public int Source { get; set; }
        public int Target { get; set; }

        public enum MoveType : byte { Quiet, Capture, Check, Castle, EnPassant };
        public MoveType Type { get; set; }
        public PieceType Promotion { get; set; }

        public string Algebraic {
            get
            {
                // TODO: other piece types and stuff
                return (char)('a'+(Source%nFiles)) + "" + ((Target/nFiles)+1);
            }
        }
    }

    // Generate moves given the current board state and previous move
    private List<Move> GenerateMoves()
    {
        var moves = new List<Move>();
        if (previous == null || !previous.whiteMove)
        {
            foreach (int pawn in WhitePawns)
            {
                if (pawn / nFiles < nRanks-1)
                {
                    var newMove = new Move();
                    newMove.whiteMove = true;
                    newMove.Piece = Move.PieceType.Pawn;
                    newMove.Source = pawn;
                    newMove.Target = pawn + nFiles;
                    newMove.Type = Move.MoveType.Quiet;
                    // newMove.promotion = Move.PieceType.Pawn;
                    moves.Add(newMove);
                }
            }
        }
        else
        {
            foreach (int pawn in BlackPawns)
            {
                if (pawn / nFiles > 0)
                {
                    var newMove = new Move();
                    newMove.whiteMove = false;
                    newMove.Piece = Move.PieceType.Pawn;
                    newMove.Source = pawn;
                    newMove.Target = pawn - nFiles;
                    newMove.Type = Move.MoveType.Quiet;
                    // newMove.promotion = Move.PieceType.Pawn;
                    moves.Add(newMove);
                }
            }
        }
        return moves;
    }
    private void PerformMove(Move move)
    {
        if (move.Piece == Move.PieceType.Pawn)
        {
            if (move.whiteMove)
            {
                WhitePawns.Remove(move.Source);
                WhitePawns.Add(move.Target);
            }
            else
            {
                BlackPawns.Remove(move.Source);
                BlackPawns.Add(move.Target);
            }
            previous = move;
        }
        else
        {
            throw new NotImplementedException();
        }
    }



    ///////////////////////////////////
    // for interface from the outside

    public IEnumerable<string> GetMovesAlgebraic()
    {
        var moves = GenerateMoves();
        // UnityEngine.Debug.Log(moves.Count);
        foreach (Move move in moves)
        {
            yield return move.Algebraic;
        }
    }

    public void PerformMoveAlgebraic(string todo)
    {
        var moves = GenerateMoves();
        foreach (Move move in moves)
        {
            if (move.Algebraic == todo)
            {
                PerformMove(move);
            }
        }
    }
}
