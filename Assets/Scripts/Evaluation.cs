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
                    if (next.type != Move.Special.Null) {
                        count += Perft(next, ply-1);
                    }
                }
                UndoMove(current);
                return count;
            }
        }
    }

    // returns algebraic move and their evaluation
    int quiescePly=4;
    public Dictionary<string, int> EvaluatePosition(int negaMaxPly, int quiescePly=3)
    {
        if (negaMaxPly < 1 || quiescePly < 1) {
            throw new Exception("cannot evaluate 0 moves ahead");
        }
        this.quiescePly = quiescePly;
        
        var evals = new Dictionary<string, int>();
        foreach (string algebraic in legalMoves.Keys)
        {
            // should never be null because it is already a legal move
            int eval = (int)NegaMax(legalMoves[algebraic], negaMaxPly-1, -999999, 999999);
            evals[algebraic] = eval;
        }
        return evals;
    }

    private int? NegaMax(Move current, int ply, int alpha, int beta)
    {
        if (current.type == Move.Special.Null) { // TODO: so ugly... why pawns why
            return null;
        }
        if (ply == 0) {
            return Quiesce(current, quiescePly-1, alpha, beta);
        }

        PlayMove(current);
        var nexts = new List<Move>(FindPseudoLegalMoves(current)); // save as list so calculations don't happen twice
        if (InCheck(current, nexts)) // moving into check
        {
            UndoMove(current);
            return null;
        }
        foreach (Move next in nexts)
        {
            int? eval = -NegaMax(next, ply-1, -beta, -alpha);
            if (eval == null) {
                continue;
            }
            int score = (int)eval;
            if (score <= alpha) { 
                UndoMove(current);
                return alpha;
            }
            if (score < beta) {
                beta = score;
            }
        }
        UndoMove(current);
        return beta;
    }

    private static readonly Dictionary<Piece, int> pieceValues = new Dictionary<Piece, int>() {
        { Piece.None, 0 },
        { Piece.Pawn, 100 },
        { Piece.VirginPawn, 100 },
        { Piece.Rook, 500 },
        { Piece.VirginRook, 500 },
        { Piece.Knight, 300 },
        { Piece.Bishop, 310 },
        { Piece.Queen, 950 },
        { Piece.King, 10000 },
        { Piece.VirginKing, 10000 },
    };
    private int Evaluate(Move current)
    {
        int total = 0;
        foreach (Piece p in whitePieces) {
            total += pieceValues[p];
        }
        foreach (Piece p in blackPieces) {
            total -= pieceValues[p];
        }
        return current.whiteMove? total:-total;
    }
    private int? Quiesce(Move current, int ply, int alpha, int beta)
    {
        PlayMove(current);
        var nexts = new List<Move>(FindPseudoLegalMoves(current));
        if (InCheck(current, nexts)) // moving into check
        {
            UndoMove(current);
            return null;
        }

        int standPat = Evaluate(current);
        if (ply == 0) {
            UndoMove(current);
            return standPat;
        }
        if (standPat <= alpha) {
            UndoMove(current);
            return alpha;
        }
        if (standPat < beta) {
            beta = standPat;
        }
        foreach (Move next in nexts)
        {
            if (next.captured == Piece.None) { // only consider captures, which means should not need to check for null moves 
                continue;
            }
            int? eval = -Quiesce(next, ply-1, -beta, -alpha);
            if (eval == null) {
                continue;
            }
            int score = (int)eval;
            if (score <= alpha) {
                UndoMove(current);
                return alpha;
            }
            if (score < beta) {
                beta = score;
            }
        }
        UndoMove(current);
        return beta;
    }
}