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
        int nodes = Perft(prevMove, ply);

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
                if (current.Captured != Piece.None) captures += 1;
                if (current.Type == Move.Special.EnPassant) eps += 1;
                if (current.Type == Move.Special.Castle) castles += 1;
                if (current.Promotion != Piece.None
                    && current.Moved == Piece.Pawn) promos += 1;
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
    private static Dictionary<Piece, float> pieceValues = new Dictionary<Piece, float>() {
        { Piece.Pawn, 1 },
        { Piece.Rook, 5 },
        { Piece.Knight, 3 },
        { Piece.Bishop, 3 },
        { Piece.Queen, 9 },
        { Piece.King, 100 }
    };
    private float Evaluate(Move current)
    {
        float total = 0;
        foreach (Piece p in whitePieces.Values) total += pieceValues[p];
        foreach (Piece p in blackPieces.Values) total -= pieceValues[p];
        return total;
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