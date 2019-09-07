using System;
using System.Collections.Generic;

public partial class Engine
{
    int checks;
    int captures;
    int castles;
    int promos;
    int eps;
    public int Perft(int ply)
    {
        captures = eps = castles = promos = checks = 0;
        int nodes = Perft(lastPlayed, ply);

        UnityEngine.Debug.Log(captures + " " + eps + " " + castles + " " + promos + " " + checks);
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
            if (ply == 0)
            {
                if (current.Captured != PieceType.None) captures += 1;
                if (current.Type == MoveType.EnPassant) eps += 1;
                if (current.Type == MoveType.Castle) castles += 1;
                if (current.Promotion != PieceType.None
                    && current.Moved == PieceType.Pawn) promos += 1;
                if (IsCheck(current)) checks += 1;

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
    private float Evaluate(Move current)
    {
        // float WhiteMaterial = WhitePawns.Count
        //                       + 3 * WhiteBishops.Count
        //                       + 3 * WhiteKnights.Count
        //                       + 5 * WhiteRooks.Count
        //                       + 9 * WhiteQueens.Count;
        // float BlackMaterial = WhitePawns.Count
        //                       + 3 * WhiteBishops.Count
        //                       + 3 * WhiteKnights.Count
        //                       + 5 * WhiteRooks.Count
        //                       + 9 * WhiteQueens.Count;
        return (whiteOccupancy.Count - blackOccupancy.Count) * (current.WhiteMove? 1 : -1);
    }
    float alpha=float.MinValue, beta=float.MaxValue;
    private float NegaMax(Move current, int ply)
    {
        PlayMove(current);
        var nexts = new List<Move>(FindPseudoLegalMoves(current));
        if (InCheck(current, nexts)) // if illegal
        {
            UndoMove(current);
            return float.MinValue;
        }
        else
        {
            if (ply == 0)
            {
                UndoMove(current);
                return Evaluate(current);
            }
            else
            {
                float eval = float.MinValue;
                foreach (Move next in nexts)
                {
                    // negation here is for minimax
                    eval = Math.Max(eval, -NegaMax(next, ply-1));
                }
                UndoMove(current);
                return eval;
            }
        }

    }
}