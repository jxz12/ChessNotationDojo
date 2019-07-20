using System;
using System.Collections.Generic;

public partial class Engine
{
    // Generate candidate moves given the current board state and previous move
    private IEnumerable<Move> PseudoLegalMoves(Move previous)
    {
        bool whiteToMove = !previous.WhiteMove;
        var allyPawns = whiteToMove? WhitePawns : BlackPawns;
        var enemyPawns =  whiteToMove? BlackPawns : WhitePawns;
        var allies = whiteToMove? whiteOccupancy : blackOccupancy;
        var enemies = whiteToMove? blackOccupancy : whiteOccupancy;

        ////////////
        // castle //
        ////////////
        int king = whiteToMove? WhiteKing : BlackKing;
        // var kingType = whiteToMove? whiteOccupancy[king] : blackOccupancy[king];
        // if (kingType == PieceType.VirginKing)
        // {
        //     PieceType left = AllyRaycast(king, -1, 0, whiteToMove);
        //     if (left == PieceType.VirginRook) // TODO: won't work for bizarre castles
        //     {
        //         int castleFile = whiteToMove? WhiteLeftCastledFile
        //                                     : BlackLeftCastledFile;
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = king,
        //             Target = GetPos(castleFile, GetRank(king)),
        //             Type = MoveType.Castle,
        //             Moved = PieceType.Pawn,
        //             Captured = PieceType.Pawn,
        //             Previous = previous
        //         };
        //     }
        //     PieceType right = AllyRaycast(king, 1, 0, whiteToMove);
        //     UnityEngine.Debug.Log(right);
        //     if (right == PieceType.VirginRook) // TODO: won't work for bizarre castles
        //     {
        //         int castleFile = whiteToMove? WhiteRightCastledFile
        //                                     : BlackRightCastledFile;
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = king,
        //             Target = GetPos(castleFile, GetRank(king)),
        //             Type = MoveType.Castle,
        //             Moved = PieceType.Pawn,
        //             Captured = PieceType.Pawn,
        //             Previous = previous
        //         };
        //     }
        // }

        ////////////////
        // en passant //
        ////////////////
        int forward = whiteToMove? nFiles : -nFiles;
        if (previous.Moved == PieceType.VirginPawn &&
            previous.Target == previous.Source - 2*forward)
        {
            // capture from left
            if (previous.Target%nFiles != 0 &&
                allyPawns.Contains(previous.Target-1))
            {
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = previous.Target - 1,
                    Target = previous.Target + forward,
                    Type = MoveType.EnPassant,
                    Moved = allies[previous.Target-1],
                    Captured = PieceType.Pawn,
                    Previous = previous
                };
            }
            // capture from right
            if (previous.Target%nFiles != nFiles-1 &&
                allyPawns.Contains(previous.Target+1))
            {
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = previous.Target + 1,
                    Target = previous.Target + forward,
                    Type = MoveType.EnPassant,
                    // Moved = PieceType.Pawn,
                    Moved = allies[previous.Target+1],
                    Captured = PieceType.Pawn,
                    Previous = previous
                };
            }
        }


        ///////////
        // pawns //
        ///////////
        foreach (int pawn in allyPawns)
        {
            // push
            if (!Occupied(pawn+forward)) // pawn should never be at last rank
            {
                var push = new Move() {
                    WhiteMove = whiteToMove,
                    Source = pawn,
                    Target = pawn + forward,
                    Type = MoveType.Normal,
                    Moved = allies[pawn],
                    Previous = previous
                };
                foreach (Move m in PromotionsIfPossible(push))
                    yield return m;
                
                // puush
                int pushedRank = GetRank(pawn + 2*forward);
                if (allies[pawn] == PieceType.VirginPawn &&
                    pushedRank >= 0 && pushedRank < nRanks &&
                    !Occupied(pawn+2*forward))
                {
                    var puush = new Move() {
                        WhiteMove = whiteToMove,
                        Source = pawn,
                        Target = pawn + 2*forward,
                        Type = MoveType.Normal,
                        Moved = PieceType.VirginPawn,
                        Previous = previous
                    };
                    foreach (Move m in PromotionsIfPossible(puush))
                        yield return m;
                }
            }
            
            // capture
            foreach (int attack in PawnAttacks(pawn, whiteToMove))
            {
                // pawns cannot move to an attacked square unless it's a capture
                if (enemies.ContainsKey(attack))
                {
                    var pish = new Move() {
                        WhiteMove = whiteToMove,
                        Source = pawn,
                        Target = attack,
                        Type = MoveType.Normal,
                        Moved = allies[pawn],
                        Captured = enemies[attack],
                        Previous = previous
                    };
                    foreach (Move m in PromotionsIfPossible(pish))
                        yield return m;
                }
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
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = knight,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Knight,
                    Captured = enemies.ContainsKey(attack)
                                ? enemies[attack]
                                : PieceType.None,
                    Previous = previous
                };
            }
        }
        var bishops = whiteToMove? WhiteBishops : BlackBishops;
        foreach (int bishop in bishops)
        {
            foreach (int attack in BishopAttacks(bishop, whiteToMove))
            {
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = bishop,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Bishop,
                    Captured = enemies.ContainsKey(attack)
                                ? enemies[attack]
                                : PieceType.None,
                    Previous = previous
                };
            }
        }
        var rooks = whiteToMove? WhiteRooks : BlackRooks;
        foreach (int rook in rooks)
        {
            foreach (int attack in RookAttacks(rook, whiteToMove))
            {
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = rook,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = allies[rook],
                    Captured = enemies.ContainsKey(attack)
                                ? enemies[attack]
                                : PieceType.None,
                    Promotion = allies[rook]==PieceType.VirginRook
                                 ? PieceType.Rook
                                 : PieceType.None,
                    Previous = previous
                };
            }
        }
        var queens = whiteToMove? WhiteQueens : BlackQueens;
        foreach (int queen in queens)
        {
            foreach (int attack in QueenAttacks(queen, whiteToMove))
            {
                // UnityEngine.Debug.Log(attack);
                PieceType capture = enemies.ContainsKey(attack)
                                     ? enemies[attack]
                                     : PieceType.None;
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = queen,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Queen,
                    Captured = enemies.ContainsKey(attack)
                                ? enemies[attack]
                                : PieceType.None,
                    Previous = previous
                };
            }
        }
        foreach (int attack in KingAttacks(king, whiteToMove))
        {
            yield return new Move() {
                WhiteMove = whiteToMove,
                Source = king,
                Target = attack,
                Type = MoveType.Normal,
                Moved = allies[king],
                Captured = enemies.ContainsKey(attack)
                            ? enemies[attack]
                            : PieceType.None,
                Promotion = allies[king]==PieceType.VirginKing
                             ? PieceType.King
                             : PieceType.None,
                Previous = previous
            };
        }
    }
    private IEnumerable<Move> PromotionsIfPossible(Move pawnMove)
    {
        // check for promotion
        if (pawnMove.Target/nRanks == (pawnMove.WhiteMove? nRanks-1 : 0))
        {
            // add possible promotions
            pawnMove.Promotion = PieceType.Knight;
            yield return pawnMove.DeepCopy();
            pawnMove.Promotion = PieceType.Bishop;
            yield return pawnMove.DeepCopy();
            pawnMove.Promotion = PieceType.Rook;
            yield return pawnMove.DeepCopy();
            pawnMove.Promotion = PieceType.Queen;
            yield return pawnMove;
        }
        else
        {
            if (pawnMove.Moved == PieceType.VirginPawn)
            {
                pawnMove.Promotion = PieceType.Pawn;
            }
            yield return pawnMove;
        }
    }

    private void AddPiece(PieceType type, int pos, bool white)
    {
        if (Occupied(pos))
            throw new Exception("occupado");

        if (white)
        {
            if (type == PieceType.Pawn
                || type == PieceType.VirginPawn) WhitePawns.Add(pos);
            else if (type == PieceType.Knight) WhiteKnights.Add(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Add(pos);
            else if (type == PieceType.Rook
                     || type == PieceType.VirginRook) WhiteRooks.Add(pos);
            else if (type == PieceType.Queen) WhiteQueens.Add(pos);
            else if (type == PieceType.King
                     || type == PieceType.VirginKing) WhiteKing = pos;
            whiteOccupancy.Add(pos, type);
        }
        else
        {
            if (type == PieceType.Pawn
                || type == PieceType.VirginPawn) BlackPawns.Add(pos);
            else if (type == PieceType.Knight) BlackKnights.Add(pos);
            else if (type == PieceType.Bishop) BlackBishops.Add(pos);
            else if (type == PieceType.Rook
                     || type == PieceType.VirginRook) BlackRooks.Add(pos);
            else if (type == PieceType.Queen) BlackQueens.Add(pos);
            else if (type == PieceType.King
                     || type == PieceType.VirginKing) BlackKing = pos;
            blackOccupancy.Add(pos, type);
        }
    }
    private void RemovePiece(PieceType type, int pos, bool white)
    {
        if (!Occupied(pos))
            throw new Exception("no piece here");

        // occupancy.Remove(pos);
        if (white)
        {
            if (type == PieceType.Pawn
                || type == PieceType.VirginPawn) WhitePawns.Remove(pos);
            else if (type == PieceType.Knight) WhiteKnights.Remove(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Remove(pos);
            else if (type == PieceType.Rook
                     || type == PieceType.VirginRook) WhiteRooks.Remove(pos);
            else if (type == PieceType.Queen) WhiteQueens.Remove(pos);
            else if (type == PieceType.King
                     || type == PieceType.VirginKing) WhiteKing = -1;
            whiteOccupancy.Remove(pos);
        }
        else
        {
            if (type == PieceType.Pawn
                || type == PieceType.VirginPawn) BlackPawns.Remove(pos);
            else if (type == PieceType.Knight) BlackKnights.Remove(pos);
            else if (type == PieceType.Bishop) BlackBishops.Remove(pos);
            else if (type == PieceType.Rook
                     || type == PieceType.VirginRook) BlackRooks.Remove(pos);
            else if (type == PieceType.Queen) BlackQueens.Remove(pos);
            else if (type == PieceType.King
                     || type == PieceType.VirginKing) BlackKing = -1;
            blackOccupancy.Remove(pos);
        }
    }

    private void PlayMove(Move move)
    {
        if (move.Type == MoveType.Castle)
        {
            throw new Exception("TODO:");
        }
        // capture target
        if (move.Captured != PieceType.None)
        {
            if (move.Type == MoveType.EnPassant)
            {
                RemovePiece(PieceType.Pawn, move.Previous.Target, !move.WhiteMove);
            }
            else
            {
                RemovePiece(move.Captured, move.Target, !move.WhiteMove);
            }
        }

        // move source
        RemovePiece(move.Moved, move.Source, move.WhiteMove);
        if (move.Promotion != PieceType.None)
        {
            AddPiece(move.Promotion, move.Target, move.WhiteMove);
        }
        else
        {
            AddPiece(move.Moved, move.Target, move.WhiteMove);
        }
    }
    private void UndoMove(Move move)
    {
        if (move.Type == MoveType.Castle)
        {
            throw new Exception("TODO:");
        }
        // move back source
        if (move.Promotion != PieceType.None)
        {
            RemovePiece(move.Promotion, move.Target, move.WhiteMove);
        }
        else
        {
            RemovePiece(move.Moved, move.Target, move.WhiteMove);
        }
        AddPiece(move.Moved, move.Source, move.WhiteMove);

        // capture target
        if (move.Captured != PieceType.None)
        {
            if (move.Type == MoveType.EnPassant)
            {
                AddPiece(PieceType.Pawn, move.Previous.Target, !move.WhiteMove);
            }
            else
            {
                AddPiece(move.Captured, move.Target, !move.WhiteMove);
            }
        }
    }
}