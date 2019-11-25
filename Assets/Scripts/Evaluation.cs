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
    private static Dictionary<Piece, int> pieceValues = new Dictionary<Piece, int>() {
        { Piece.None, 0 },
        { Piece.VirginPawn, 1 },
        { Piece.Pawn, 1 },
        { Piece.VirginRook, 5 },
        { Piece.Rook, 5 },
        { Piece.Knight, 3 },
        { Piece.Bishop, 3 },
        { Piece.Queen, 9 },
        { Piece.King, 100 },
        { Piece.VirginKing, 100 },
    };
    private int Evaluate(Move current)
    {
        int total = 0;
        foreach (Piece p in whitePieces) total += pieceValues[p];
        foreach (Piece p in blackPieces) total -= pieceValues[p];
        return total;
    }

    // returns best move algebraic and evaluation
    public Tuple<string, int> EvaluateBestMove(int ply)
    {
        int bestEval = -999;
        string bestAlgebraic = null;
        foreach (string algebraic in legalMoves.Keys)
        {
            // FIXME: simple 2-ply with randomness added in!
            // int eval = NegaMax(legalMoves[algebraic], ply, -999, 999, prevMove.whiteMove?-1:1);
            int eval = UnityEngine.Random.Range(0,100);
            if (eval > bestEval)
            {
                bestEval = eval;
                bestAlgebraic = algebraic;
            }
        }
        return Tuple.Create(bestAlgebraic, bestEval);
    }
    private int NegaMax(Move current, int ply, int alpha, int beta, int colour)
    {
        if (current.type == Move.Special.None) // TODO: so ugly...
            return -999;

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
                int eval = -999;
                foreach (Move next in nexts)
                {
                    eval = Math.Max(eval, -NegaMax(next, ply-1, -beta, -alpha, -colour));
                    alpha = Math.Max(alpha, eval);
                    if (alpha >= beta)
                        break; // cut-off
                }
                // TODO: checkmate checks here probably
                UndoMove(current);
                return eval;
            }
        }
    }
}