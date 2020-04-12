using System;
using System.Collections.Generic;

public partial class Engine
{
    int checks, captures, castls, promos, EPs;
    public int Perft(int ply)
    {
        captures = EPs = castls = promos = checks = 0;
        int nodes = Perft(prevMove, ply);

        UnityEngine.Debug.Log(captures + " " + EPs + " " + castls + " " + promos + " " + checks);
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
                if (current.captured != Piece.None) captures += 1;
                if (current.type == Move.Special.EnPassant) EPs += 1;
                if (current.type == Move.Special.Castle) castls += 1;
                if (current.promotion != Piece.None
                    && current.promotion != Piece.Pawn
                    && current.moved == Piece.Pawn) promos += 1;
                if (IsCheck(current)) checks += 1;

                UndoMove(current);
                return 1;
            }
            else
            {
                int count = 0;
                foreach (Move next in nexts)
                {
                    if (next.type != Move.Special.None)
                        count += Perft(next, ply-1);
                }
                UndoMove(current);
                return count;
            }
        }
    }
    private static readonly Dictionary<Piece, float> pieceValues = new Dictionary<Piece, float>() {
        { Piece.None, 0 },
        { Piece.Pawn, 1 },
        { Piece.VirginPawn, 1 },
        { Piece.Rook, 5 },
        { Piece.VirginRook, 5 },
        { Piece.Knight, 3 },
        { Piece.Bishop, 3.1f },
        { Piece.Queen, 9.5f },
        { Piece.King, 100 },
        { Piece.VirginKing, 100 },
    };
    private float Evaluate(Move current)
    {
        float total = 0;
        foreach (Piece p in whitePieces) total += pieceValues[p];
        foreach (Piece p in blackPieces) total -= pieceValues[p];
        return total;
    }

    // returns best move algebraic and evaluation
    public Dictionary<string, float> EvaluatePosition(int ply)
    {
        var evals = new Dictionary<string, float>();
        foreach (string algebraic in legalMoves.Keys)
        {
            float eval = NegaMax(legalMoves[algebraic], ply, -999, 999, prevMove.whiteMove?-1:1);
            evals[algebraic] = eval;
        }
        return evals;
    }

    private float NegaMax(Move current, int ply, float alpha, float beta, int colour)
    {
        if (current.type == Move.Special.None) { // TODO: so ugly...
            return -999;
        }

        PlayMove(current);
        var nexts = new List<Move>(FindPseudoLegalMoves(current));
        if (InCheck(current, nexts)) // moving into check
        {
            UndoMove(current);
            return -999;
        }
        else
        {
            if (ply == 0)
            {
                UndoMove(current);
                return colour * Evaluate(current);
            }
            else
            {
                foreach (Move next in nexts)
                {
                    float score = -NegaMax(next, ply-1, -beta, -alpha, -colour);
                    if (score >= beta) {
                        UndoMove(current);
                        return beta; // fail beta hard cutoff
                    }
                    if (score > alpha) {
                        alpha = score;
                    }
                    // alpha = Math.Max(alpha, eval);
                    // if (alpha >= beta) {
                    //     break; // cut-off
                    // }
                }
                UndoMove(current);
                return alpha;

                // float eval = -999;
                // foreach (Move next in nexts)
                // {
                //     eval = Math.Max(eval, -NegaMax(next, ply-1, -beta, -alpha, -colour));
                //     alpha = Math.Max(alpha, eval);
                //     if (alpha >= beta) {
                //         break; // cut-off
                //     }
                // }
                // UndoMove(current);
                // return eval;
            }
        }
    }
}