using System;
using System.Collections.Generic;

public partial class Engine
{
    private int GetFile(int pos)
    {
        return pos % nFiles;
    }
    private int GetRank(int pos)
    {
        return pos / nFiles;
    }
    private int GetPos(int file, int rank)
    {
        return rank * nFiles + file;
    }

    // Generate candidate moves given the current board state and previous move
    private List<Move> GenerateMoves()
    {
        var moves = new List<Move>();
        bool whiteToMove = !previous.WhiteMove;

        ///////////
        // pawns //
        ///////////
        var friendPawns = whiteToMove? WhitePawns : BlackPawns;
        var enemyPawns =  whiteToMove? BlackPawns : WhitePawns;

        // en passant
        int push = whiteToMove? nFiles : -nFiles;
        if (previous.Moved == PieceType.Pawn &&
            previous.Target == previous.Source - 2*push)
        {
            // capture from left
            if (previous.Target%nFiles != 0 &&
                friendPawns.Contains(previous.Target-1))
            {
                var enpassant = new Move() {
                    WhiteMove = whiteToMove,
                    CanCastle = previous.CanCastle,
                    Source = previous.Target - 1,
                    Target = previous.Target + push,
                    Type = MoveType.EnPassant,
                    Moved = PieceType.Pawn,
                    Captured = PieceType.Pawn
                };
                moves.Add(enpassant);
            }
            // capture from right
            if (previous.Target%nFiles != nFiles-1 &&
                friendPawns.Contains(previous.Target+1))
            {
                var enpassant = new Move() {
                    WhiteMove = true,
                    CanCastle = previous.CanCastle,
                    Source = previous.Target + 1,
                    Target = previous.Target + push,
                    Type = MoveType.EnPassant,
                    Moved = PieceType.Pawn,
                    Captured = PieceType.Pawn
                };
                moves.Add(enpassant);
            }
        }
        // push
        foreach (int pawn in friendPawns)
        {
            // single
            int pushedRank = GetRank(pawn + push);
            if (pushedRank >= 0 && pushedRank <= nRanks &&
                !occupancy.Contains(pawn+push))
            {
                var move = new Move() {
                    WhiteMove = whiteToMove,
                    CanCastle = previous.CanCastle,
                    Source = pawn,
                    Target = pawn + push,
                    Type = MoveType.Normal,
                    Moved = PieceType.Pawn,
                };
                moves.Add(move);

                // double
                var initPawns = whiteToMove? whitePawnsInit : blackPawnsInit;
                pushedRank = GetRank(pawn + 2*push);
                if (initPawns.Contains(pawn) &&
                    pushedRank >= 0 && pushedRank <= nRanks &&
                    !occupancy.Contains(pawn+2*push))
                {
                    var puush = new Move() {
                        WhiteMove = whiteToMove,
                        CanCastle = previous.CanCastle,
                        Source = pawn,
                        Target = pawn + 2*push,
                        Type = MoveType.Normal,
                        Moved = PieceType.Pawn,
                    };
                    moves.Add(puush);
                }
            }

            var enemyOccupancy = whiteToMove? blackOccupancy : whiteOccupancy;

            // capture left
            int leftCapture = push-1;
            if (pawn%nFiles != 0 &&
                enemyOccupancy.ContainsKey(pawn + leftCapture))
            {
                var move = new Move() {
                    WhiteMove = whiteToMove,
                    CanCastle = previous.CanCastle,
                    Source = pawn,
                    Target = pawn + leftCapture,
                    Type = MoveType.Normal,
                    Moved = PieceType.Pawn,
                    Captured = enemyOccupancy[pawn+leftCapture]
                };
                moves.Add(move);
            }
            // capture right
            int rightCapture = push+1;
            if (pawn%nFiles != nFiles-1 &&
                enemyOccupancy.ContainsKey(pawn + rightCapture))
            {
                var move = new Move() {
                    WhiteMove = whiteToMove,
                    CanCastle = previous.CanCastle,
                    Source = pawn,
                    Target = pawn + rightCapture,
                    Type = MoveType.Normal,
                    Moved = PieceType.Pawn,
                    Captured = enemyOccupancy[pawn + rightCapture]
                };
                moves.Add(move);
            }
        }

        // check for promotion, now that all pawn moves should be in place
        int nMoves = moves.Count;
        for (int i=0; i<nMoves; i++)
        {
            Move move = moves[i];
            if (move.Target/nRanks == (whiteToMove? nRanks-1 : 0))
            {
                // add possible promotions
                move.Promotion = PieceType.Rook;
                moves.Add(move.DeepCopy());
                move.Promotion = PieceType.Knight;
                moves.Add(move.DeepCopy());
                move.Promotion = PieceType.Bishop;
                moves.Add(move.DeepCopy());
                // replace existing move with queen promotion
                move.Promotion = PieceType.Queen;
            }
        }

        /////////////
        // knights //
        /////////////
        // for convenience, but might be slow
        Action<int, int, int> TryAddKnightMove = (knight, fileHop, rankHop)=>
        {
            int startFile = GetFile(knight);
            int startRank = GetRank(knight);
            int targetFile = startFile + fileHop;
            int targetRank = startRank + rankHop;
            int targetPos = GetPos(targetFile, targetRank);
            bool blocked = whiteToMove? whiteOccupancy.ContainsKey(targetPos)
                                      : blackOccupancy.ContainsKey(targetPos);

            if (targetRank >= 0 && targetRank <= nRanks-1 &&
                targetFile >= 0 && targetFile <= nFiles-1 &&
                !blocked)
            {
                bool capture = whiteToMove? blackOccupancy.ContainsKey(targetPos)
                                          : whiteOccupancy.ContainsKey(targetPos);
                var move = new Move() {
                    WhiteMove = whiteToMove,
                    CanCastle = previous.CanCastle,
                    Source = knight,
                    Target = targetPos,
                    Type = MoveType.Normal,
                    Moved = PieceType.Knight,
                    Captured = capture? (whiteToMove? blackOccupancy[targetPos]
                                                    : whiteOccupancy[targetPos])
                                      : PieceType.None,
                };
                moves.Add(move);
            }
        };

        var knights = previous.WhiteMove? BlackKnights : WhiteKnights;
        foreach (int knight in knights)
        {
            TryAddKnightMove(knight, -2,  1);
            TryAddKnightMove(knight, -1,  2);
            TryAddKnightMove(knight,  1,  2);
            TryAddKnightMove(knight,  2,  1);
            TryAddKnightMove(knight,  2, -1);
            TryAddKnightMove(knight,  1, -2);
            TryAddKnightMove(knight, -1, -2);
            TryAddKnightMove(knight, -2, -1);
        }

        /////////////
        // bishops //
        /////////////



        return moves;
    }
}