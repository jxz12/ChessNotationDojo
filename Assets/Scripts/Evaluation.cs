using System.Collections.Generic;

public partial class Engine
{
    int checks;
    int captures;
    int castles;
    int eps;
    public int Perft(int ply)
    {
        captures = eps = castles = checks = 0;
        int nodes = Perft(lastPlayed, ply);

        UnityEngine.Debug.Log(captures + " " + eps + " " + castles + " " + checks);
        return nodes;
    }
    private int Perft(Move current, int ply)
    {
        PlayMove(current);
        var nexts = new List<Move>(FindPseudoLegalMoves(current));
        if (InCheck(current, nexts)) // if illegal
        {
            UndoMove(current);
            return 0;
        }
        else
        {
            // UnityEngine.Debug.Log(ply + " " + current.Moved + " " + current.Target);
            if (ply == 0)
            {
                if (current.Captured != PieceType.None) captures += 1;
                if (IsCheck(current)) checks += 1;
                if (current.Type == MoveType.Castle) castles += 1;
                if (current.Type == MoveType.EnPassant) eps += 1;

                UndoMove(current);
                return 1;
            }
            else
            {
                int count = 0;
                foreach (Move next in nexts)
                {
                    count += Perft(next, ply-1);
                }
                UndoMove(current);
                return count;
            }
        }

    }
    // private Move Evaluate(int ply)
    // {
    //     currentPly = 0;
    // }
    // private Move best;
    // private int currentPly;
    // private int _Evaluate(Move current, int ply)
    // {
    //     var candidates = new List<Move>(GeneratePseudoLegalMoves(current));
    //     foreach (Move move in candidates)
    //     {
    //         if (move.Captured == PieceType.King)
    //         {
    //             return -1;
    //         }
    //     }
    // }
}