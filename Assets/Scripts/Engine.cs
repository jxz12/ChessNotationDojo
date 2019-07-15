using System;
using System.Collections.Generic;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game
// TODO: can also evaluate moves and play with other engines

public class Engine
{
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
        public bool WhiteMove { get; private set; }
        public int Source { get; private set; }
        public int Target { get; private set; }
        public MoveType Type { get; private set; }
        public PieceType Moved { get; private set; }
        public PieceType Captured { get; private set; } // also used for promotion

        public Move(bool whiteMove, int source, int target, MoveType type, PieceType moved, PieceType captured)
        {
            WhiteMove = whiteMove;
            Source = source;
            Target = target;
            Type = type;
            Moved = moved;
            Captured = captured;
        }
    }
    public enum MoveType : byte { Quiet, Capture, Check, Castle, EnPassant };
    public enum PieceType : byte { Pawn, Rook, Knight, Bishop, Queen, King };


    // used for checking if double push available
    private HashSet<int> whitePawnsInit, blackPawnsInit;
    public Engine(int ranks=8, int files=8)
    {
        nRanks = ranks;
        nFiles = files;
        whitePawnsInit = new HashSet<int>(WhitePawns);
        blackPawnsInit = new HashSet<int>(BlackPawns);
    }


    // Generate moves given the current board state and previous move
    private List<Move> GenerateMoves()
    {
        var moves = new List<Move>();
        if (previous == null || !previous.WhiteMove)
        {
            foreach (int pawn in WhitePawns)
            {
                if (pawn / nFiles < nRanks-1)
                {
                    var push = new Move(true, pawn, pawn+nFiles, MoveType.Quiet, PieceType.Pawn, PieceType.Pawn);
                    moves.Add(push);
                    if (whitePawnsInit.Contains(pawn))
                    {
                        var puush = new Move(true, pawn, pawn+2*nFiles, MoveType.Quiet, PieceType.Pawn, PieceType.Pawn);
                        moves.Add(puush);
                    }
                }
            }
        }
        else
        {
            foreach (int pawn in BlackPawns)
            {
                if (pawn / nFiles > 0)
                {
                    UnityEngine.Debug.Log("hello");
                    var push = new Move(false, pawn, pawn-nFiles, MoveType.Quiet, PieceType.Pawn, PieceType.Pawn);
                    moves.Add(push);
                    if (blackPawnsInit.Contains(pawn))
                    {
                        var puush = new Move(false, pawn, pawn-2*nFiles, MoveType.Quiet, PieceType.Pawn, PieceType.Pawn);
                        moves.Add(puush);
                    }
                }
            }
        }
        return moves;
    }
    private void PerformMove(Move move)
    {
        if (move.Moved == PieceType.Pawn)
        {
            if (move.WhiteMove)
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
    private void UndoMove(Move move)
    {
        // TODO:
    }



    ///////////////////////////////////
    // for interface from the outside

    private string GetAlgebraic(Move move)
    {
        string str = "";
        if (move.Moved == PieceType.Pawn)
        {
            str += (char)('a'+(move.Source%nFiles));
            if (move.Type == MoveType.Capture)
            {
                str += 'x' + ('a'+(move.Target%nFiles));
            }
            str += move.Target/nFiles + 1;
        }
        else
        {
            if (move.Moved == PieceType.Rook)
            {
                str += 'R';
            }
            else if (move.Moved == PieceType.Knight)
            {
                str += 'N';
            }
            else if (move.Moved == PieceType.Bishop)
            {
                str += 'B';
            }
            else if (move.Moved == PieceType.Queen)
            {
                str += 'Q';
            }
            else if (move.Moved == PieceType.King)
            {
                str += 'J';
            }
            // TODO: piece ambiguity

            if (move.Type == MoveType.Capture)
            {
                str += 'x';
            }
            str += (char)('a'+(move.Target%nFiles));
            str += move.Target/nFiles + 1;
        }
        return str;
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
