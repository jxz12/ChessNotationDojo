using System;
using System.Collections.Generic;

public partial class Engine
{
    // Generate candidate moves given the current board state and previous move
    private List<Move> GenerateMoves()
    {
        var moves = new List<Move>();
        bool whiteToMove = !previous.WhiteMove;
        var enemyOccupancy = whiteToMove? blackOccupancy : whiteOccupancy;

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
                moves.Add(new Move() {
                    WhiteMove = whiteToMove,
                    Source = previous.Target - 1,
                    Target = previous.Target + push,
                    Type = MoveType.EnPassant,
                    Moved = PieceType.Pawn,
                    Captured = PieceType.Pawn
                });
            }
            // capture from right
            if (previous.Target%nFiles != nFiles-1 &&
                friendPawns.Contains(previous.Target+1))
            {
                moves.Add(new Move() {
                    WhiteMove = true,
                    Source = previous.Target + 1,
                    Target = previous.Target + push,
                    Type = MoveType.EnPassant,
                    Moved = PieceType.Pawn,
                    Captured = PieceType.Pawn
                });
            }
        }
        // push
        foreach (int pawn in friendPawns)
        {
            // single
            if (!occupancy.Contains(pawn+push)) // pawn should never be at last rank
            {
                moves.Add(new Move() {
                    WhiteMove = whiteToMove,
                    Source = pawn,
                    Target = pawn + push,
                    Type = MoveType.Normal,
                    Moved = PieceType.Pawn,
                });

                // double
                var initPawns = whiteToMove? whitePawnsInit : blackPawnsInit;
                int pushedRank = GetRank(pawn + 2*push);
                if (initPawns.Contains(pawn) &&
                    pushedRank >= 0 && pushedRank < nRanks &&
                    !occupancy.Contains(pawn+2*push))
                {
                    moves.Add(new Move() {
                        WhiteMove = whiteToMove,
                        Source = pawn,
                        Target = pawn + 2*push,
                        Type = MoveType.Normal,
                        Moved = PieceType.Pawn,
                    });
                }
            }
            
            foreach (int attack in PawnAttacks(pawn, whiteToMove))
            {
                // pawns cannot move to an attacked square unless it's a capture
                if (enemyOccupancy.ContainsKey(attack))
                {
                    moves.Add(new Move() {
                        WhiteMove = whiteToMove,
                        Source = pawn,
                        Target = attack,
                        Type = MoveType.Normal,
                        Moved = PieceType.Pawn,
                        Captured = enemyOccupancy[attack]
                    });
                }
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
                move.Promotion = PieceType.Knight;
                moves.Add(move.DeepCopy());
                move.Promotion = PieceType.Bishop;
                moves.Add(move.DeepCopy());
                move.Promotion = PieceType.Rook;
                moves.Add(move.DeepCopy());
                // replace existing move with queen promotion
                move.Promotion = PieceType.Queen;
            }
        }

        ////////////
        // pieces //
        ////////////

        var knights = whiteToMove? WhiteKnights : BlackKnights;
        foreach (int knight in knights)
        {
            foreach (int attack in KnightAttacks(knight, whiteToMove))
            {
                moves.Add(new Move() {
                    WhiteMove = whiteToMove,
                    Source = knight,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Knight,
                    Captured = enemyOccupancy.ContainsKey(attack)
                                ? enemyOccupancy[attack]
                                : PieceType.None
                });
            }
        }
        var bishops = whiteToMove? WhiteBishops : BlackBishops;
        foreach (int bishop in bishops)
        {
            foreach (int attack in BishopAttacks(bishop, whiteToMove))
            {
                moves.Add(new Move() {
                    WhiteMove = whiteToMove,
                    Source = bishop,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Bishop,
                    Captured = enemyOccupancy.ContainsKey(attack)
                                ? enemyOccupancy[attack]
                                : PieceType.None
                });
            }
        }
        var rooks = whiteToMove? WhiteRooks : BlackRooks;
        foreach (int rook in rooks)
        {
            foreach (int attack in RookAttacks(rook, whiteToMove))
            {
                moves.Add(new Move() {
                    WhiteMove = whiteToMove,
                    Source = rook,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Rook,
                    Captured = enemyOccupancy.ContainsKey(attack)
                                ? enemyOccupancy[attack]
                                : PieceType.None
                });
            }
        }
        var queens = whiteToMove? WhiteQueens : BlackQueens;
        foreach (int queen in queens)
        {
            foreach (int attack in QueenAttacks(queen, whiteToMove))
            {
                PieceType capture = enemyOccupancy.ContainsKey(attack)
                                        ? enemyOccupancy[attack]
                                        : PieceType.None;
                moves.Add(new Move() {
                    WhiteMove = whiteToMove,
                    Source = queen,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Queen,
                    Captured = enemyOccupancy.ContainsKey(attack)
                                ? enemyOccupancy[attack]
                                : PieceType.None
                });
            }
        }

        //////////
        // king //
        //////////
        int king = whiteToMove? WhiteKing : BlackKing;
        int[] enemyAttack = whiteToMove? blackAttackTable : whiteAttackTable;
        foreach (int attack in KingAttacks(king, whiteToMove))
        {
            if (enemyAttack[attack] == 0)
            {
                moves.Add(new Move() {
                    WhiteMove = whiteToMove,
                    Source = king,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.King,
                    Captured = enemyOccupancy.ContainsKey(attack)
                                ? enemyOccupancy[attack]
                                : PieceType.None
                });
            }
        }

        return moves;
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
            else if (type == PieceType.Knight) WhiteKnights.Add(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Add(pos);
            else if (type == PieceType.Rook) WhiteRooks.Add(pos);
            else if (type == PieceType.Queen) WhiteQueens.Add(pos);
            whiteOccupancy.Add(pos, type);
        }
        else
        {
            if (type == PieceType.Pawn) BlackPawns.Add(pos);
            else if (type == PieceType.Knight) BlackKnights.Add(pos);
            else if (type == PieceType.Bishop) BlackBishops.Add(pos);
            else if (type == PieceType.Rook) BlackRooks.Add(pos);
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
            else if (type == PieceType.Knight) WhiteKnights.Remove(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Remove(pos);
            else if (type == PieceType.Rook) WhiteRooks.Remove(pos);
            else if (type == PieceType.Queen) WhiteQueens.Remove(pos);
            whiteOccupancy.Remove(pos);
        }
        else
        {
            if (type == PieceType.Pawn) BlackPawns.Remove(pos);
            else if (type == PieceType.Knight) BlackKnights.Remove(pos);
            else if (type == PieceType.Bishop) BlackBishops.Remove(pos);
            else if (type == PieceType.Rook) BlackRooks.Remove(pos);
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
        // TODO: when finished with a move, update the attack table
        //       watch out for discover attacks by looking like a queen
        if (move.Moved == PieceType.Pawn)
        {
            if (move.Captured != PieceType.None)
            {
                if (move.Type == MoveType.EnPassant)
                {
                    RemovePiece(PieceType.Pawn, previous.Target, !move.WhiteMove);
                }
                else
                {
                    RemovePiece(move.Captured, move.Target, !move.WhiteMove);
                }
            }
            RemovePiece(PieceType.Pawn, move.Source, move.WhiteMove);
            if (move.Promotion == PieceType.None)
            {
                AddPiece(PieceType.Pawn, move.Target, move.WhiteMove);
            }
            else
            {
                AddPiece(move.Promotion, move.Target, move.WhiteMove);
            }
        }
        else if (move.Moved == PieceType.Knight ||
                 move.Moved == PieceType.Bishop ||
                 move.Moved == PieceType.Rook ||
                 move.Moved == PieceType.Queen)
        {
            if (move.Captured != PieceType.None)
            {
                RemovePiece(move.Captured, move.Target, !move.WhiteMove);
            }
            RemovePiece(move.Moved, move.Source, move.WhiteMove);
            AddPiece(move.Moved, move.Target, move.WhiteMove);
        }
        else if (move.Moved == PieceType.King)
        {
            if (move.Captured != PieceType.None)
            {
                RemovePiece(move.Captured, move.Target, !move.WhiteMove);
            }
            MoveKing(move.Target, move.WhiteMove);
        }
        else
        {
            throw new NotImplementedException();
        }
        previous = move;
        InitAttackTables();
    }
    private void UndoMove(Move move)
    {
        // TODO:
    }
}