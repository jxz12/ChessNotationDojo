using System;
using System.Collections.Generic;
using System.Linq;

public partial class Engine
{
    // for finding candidate bishop, rook, queen moves
    // yes, I know IEnumerable is slow but it's nice
    private IEnumerable<int> SlideAttacks(int slider, int fileSlide, int rankSlide, bool whiteToMove)
    {
        int startFile = GetFile(slider);
        int startRank = GetRank(slider);
        int targetFile = startFile + fileSlide;
        int targetRank = startRank + rankSlide;
        int targetPos = GetPos(targetRank, targetFile);

        while (InBounds(targetRank, targetFile) &&
               !Occupied(targetPos))
        {
            yield return targetPos;
            targetFile += fileSlide;
            targetRank += rankSlide;
            targetPos = GetPos(targetRank, targetFile);
        }

        // capture
        if (InBounds(targetRank, targetFile) &&
            (whiteToMove? blackPieces[targetPos] != Piece.None
                        : whitePieces[targetPos] != Piece.None))
        {
            yield return targetPos;
        }
    }
    private IEnumerable<int> BishopAttacks(int bishop, bool whiteToMove)
    {
        return         SlideAttacks(bishop,  1,  1, whiteToMove)
               .Concat(SlideAttacks(bishop,  1, -1, whiteToMove))
               .Concat(SlideAttacks(bishop, -1, -1, whiteToMove))
               .Concat(SlideAttacks(bishop, -1,  1, whiteToMove));
    }
    private IEnumerable<int> RookAttacks(int rook, bool whiteToMove)
    {
        return         SlideAttacks(rook,  0,  1, whiteToMove)
               .Concat(SlideAttacks(rook,  1,  0, whiteToMove))
               .Concat(SlideAttacks(rook,  0, -1, whiteToMove))
               .Concat(SlideAttacks(rook, -1,  0, whiteToMove));
    }
    private IEnumerable<int> QueenAttacks(int queen, bool whiteToMove)
    {
        return       BishopAttacks(queen, whiteToMove)
               .Concat(RookAttacks(queen, whiteToMove));
    }
    // for candidate knight, king, pawn moves FIXME: horrendously slow, use for pawns?
    private IEnumerable<int> HopAttack(int hopper, int fileHop, int rankHop, bool whiteToMove)
    {
        int startFile = GetFile(hopper);
        int startRank = GetRank(hopper);
        int targetFile = startFile + fileHop;
        int targetRank = startRank + rankHop;
        int targetPos = GetPos(targetRank, targetFile);

        if (InBounds(targetRank, targetFile) &&
            (whiteToMove? whitePieces[targetPos] == Piece.None
                        : blackPieces[targetPos] == Piece.None))
            // only blocked by own pieces
        {
            yield return targetPos;
        }
    }
    private IEnumerable<int> KnightAttacks(int knight, bool whiteToMove)
    {
        return         HopAttack(knight,  1,  2, whiteToMove)
               .Concat(HopAttack(knight,  2,  1, whiteToMove))
               .Concat(HopAttack(knight,  2, -1, whiteToMove))
               .Concat(HopAttack(knight,  1, -2, whiteToMove))
               .Concat(HopAttack(knight, -1, -2, whiteToMove))
               .Concat(HopAttack(knight, -2, -1, whiteToMove))
               .Concat(HopAttack(knight, -2,  1, whiteToMove))
               .Concat(HopAttack(knight, -1,  2, whiteToMove));
    }
    private IEnumerable<int> KingAttacks(int king, bool whiteToMove)
    {
        return         HopAttack(king,  0,  1, whiteToMove)
               .Concat(HopAttack(king,  1,  1, whiteToMove))
               .Concat(HopAttack(king,  1,  0, whiteToMove))
               .Concat(HopAttack(king,  1, -1, whiteToMove))
               .Concat(HopAttack(king,  0, -1, whiteToMove))
               .Concat(HopAttack(king, -1,  1, whiteToMove))
               .Concat(HopAttack(king, -1,  0, whiteToMove))
               .Concat(HopAttack(king, -1, -1, whiteToMove));
    }

    private bool IsCastleBlocked(int king, int rook)
    {
        int kingEnd, rookEnd;
        if (king < rook) // king moves right
        {
            kingEnd = castle960? GetPos(GetRank(king), NFiles-3) : king+2; // TODO: what if king is near edge?
            rookEnd = kingEnd - 1;
        }
        else // king moves left
        {
            kingEnd = castle960? GetPos(GetRank(king), 3) : king-2;
            rookEnd = kingEnd + 1;
        }
        int min = Math.Min(king, Math.Min(rook, Math.Min(kingEnd, rookEnd)));
        int max = Math.Max(king, Math.Max(rook, Math.Max(kingEnd, rookEnd)));

        for (int pos=min; pos<=max; pos++)
        {
            if (pos!=king && pos!=rook && Occupied(pos))
                return true;
        }
        return false;
    }
    // Generate candidate moves given the current board state and previous move
    private IEnumerable<Move> FindPseudoLegalMoves(Move current)
    {
        bool whiteToMove = !current.whiteMove;
        var allies = whiteToMove? whitePieces : blackPieces;
        var enemies = whiteToMove? blackPieces : whitePieces;
        int forward = whiteToMove? NFiles : -NFiles;

        // convenience function to check pawns for promotion
        IEnumerable<Move> PromotionsIfPossible(Move pawnMove)
        {
            Move DeepCopyMove(Move toCopy)
            {
                return new Move() {
                    previous = toCopy.previous,
                    whiteMove = toCopy.whiteMove,
                    source = toCopy.source,
                    target = toCopy.target,
                    type = toCopy.type,
                    moved = toCopy.moved,
                    captured = toCopy.captured,
                    promotion = toCopy.promotion
                };
            }
            int rank = GetRank(pawnMove.target);
            if (rank == 0 || rank == NRanks-1)
            {
                // add possible promotions
                pawnMove.promotion = Piece.Knight;
                yield return DeepCopyMove(pawnMove);
                pawnMove.promotion = Piece.Bishop;
                yield return DeepCopyMove(pawnMove);
                pawnMove.promotion = Piece.Rook;
                yield return DeepCopyMove(pawnMove);
                pawnMove.promotion = Piece.Queen;
                yield return pawnMove;
            }
            else
            {
                yield return pawnMove;
            }
        }

        // go through each piece and generate moves
        for (int pos=0; pos<allies.Length; pos++)
        {
            int rank = GetRank(pos);
            int file = GetFile(pos);
            if (allies[pos] == Piece.Pawn || allies[pos] == Piece.VirginPawn)
            {
                // push
                // pawn should never be at last rank, so no need for bounds check
                if (!Occupied(pos+forward))
                {
                    var push = new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = pos + forward,
                        type = Move.Special.Normal,
                        moved = allies[pos],
                        promotion = Piece.Pawn,
                        previous = current
                    };
                    foreach (Move m in PromotionsIfPossible(push)) {
                        yield return m;
                    }
                    
                    // puush
                    if (allies[pos] == Piece.VirginPawn && puush)
                    {
                        int maxSteps = whiteToMove? (NRanks/2-1 - GetRank(pos)) : (NRanks/2-1 - (NRanks-GetRank(pos)-1));
                        // TODO: test this
                        for (int steps=2; steps<=maxSteps; steps++)
                        {
                            if (!Occupied(pos+steps*forward))
                            {
                                push = new Move() {
                                    whiteMove = whiteToMove,
                                    source = pos,
                                    target = pos + steps*forward,
                                    type = Move.Special.Normal,
                                    moved = Piece.VirginPawn,
                                    promotion = Piece.Pawn,
                                    previous = current
                                };
                                foreach (Move m in PromotionsIfPossible(push)) { // TODO: maybe stupid
                                    yield return m;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                
                // captures
                if (file > 0) // left
                {
                    int attack = pos+forward - 1;
                    // pawns cannot move to an attacked square unless it's a capture
                    if (enemies[attack] != Piece.None)
                    {
                        var pysh = new Move() {
                            whiteMove = whiteToMove,
                            source = pos,
                            target = attack,
                            type = Move.Special.Normal,
                            moved = allies[pos],
                            captured = enemies[attack],
                            promotion = Piece.Pawn,
                            previous = current
                        };
                        foreach (Move m in PromotionsIfPossible(pysh)) {
                            yield return m;
                        }
                    }
                    else // virtual attacks for castle check
                    {
                        yield return new Move() {
                            source = pos,
                            target = attack,
                            type = Move.Special.Null,
                        };
                    }
                }
                if (file < NFiles-1) // right
                {
                    int attack = pos+forward + 1;
                    // pawns cannot move to an attacked square unless it's a capture
                    if (enemies[attack] != Piece.None)
                    {
                        var pish = new Move() {
                            whiteMove = whiteToMove,
                            source = pos,
                            target = attack,
                            type = Move.Special.Normal,
                            moved = allies[pos],
                            captured = enemies[attack],
                            promotion = Piece.Pawn,
                            previous = current
                        };
                        foreach (Move m in PromotionsIfPossible(pish)) {
                            yield return m;
                        }
                    }
                    else // virtual attacks for castle check
                    {
                        yield return new Move() {
                            source = pos,
                            target = attack,
                            type = Move.Special.Null,
                        };
                    }
                }
                if (current.moved == Piece.VirginPawn
                    && current.target == current.source-2*forward // pushed twice
                    && rank == GetRank(current.target) // same rank
                    && Math.Abs(file - GetFile(current.target)) == 1) // enpassant
                {
                    // enpassant cannot be a promotion
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = current.target + forward,
                        type = Move.Special.EnPassant,
                        moved = allies[pos],
                        captured = Piece.Pawn,
                        promotion = Piece.Pawn,
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.Knight)
            {
                foreach (int attack in KnightAttacks(pos, whiteToMove))
                {
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = Piece.Knight,
                        captured = enemies[attack],
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.Bishop)
            {
                foreach (int attack in BishopAttacks(pos, whiteToMove))
                {
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = Piece.Bishop,
                        captured = enemies[attack],
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.Rook || allies[pos] == Piece.VirginRook)
            {
                foreach (int attack in RookAttacks(pos, whiteToMove))
                {
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = allies[pos],
                        captured = enemies[attack],
                        promotion = Piece.Rook,
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.Queen)
            {
                foreach (int attack in QueenAttacks(pos, whiteToMove))
                {
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = Piece.Queen,
                        captured = enemies[attack],
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.King || allies[pos] == Piece.VirginKing)
            {
                foreach (int attack in KingAttacks(pos, whiteToMove))
                {
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = allies[pos],
                        captured = enemies[attack],
                        promotion = Piece.King,
                        previous = current
                    };
                }
                // castling
                if (allies[pos] == Piece.VirginKing)
                {
                    foreach (int rook in castles[pos])
                    {
                        if (allies[rook] == Piece.VirginRook && !IsCastleBlocked(pos, rook))
                        {
                            yield return new Move() {
                                whiteMove = whiteToMove,
                                source = pos,
                                target = rook,
                                type = Move.Special.Castle,
                                moved = Piece.VirginKing,
                                promotion = Piece.King,
                                previous = current
                            };
                        }
                    }
                }
            }
        }
    }


    // returns if current has moved into check
    private bool InCheck(Move current, IEnumerable<Move> nexts)
    {
        if (current.type != Move.Special.Castle)
        {
            // simply check for king being captured
            foreach (Move next in nexts)
            {
                if (next.captured == Piece.King
                    || next.captured == Piece.VirginKing)
                {
                    return true;
                }
            }
        }
        else
        {
            int king=current.source, rook=current.target;
            int kingEnd;
            if (king < rook) // king moves right
            {
                kingEnd = castle960? GetPos(GetRank(king), NFiles-3) : king+2; // TODO: what if king is near edge?
            }
            else // king moves left
            {
                kingEnd = castle960? GetPos(GetRank(king), 3) : king-2;
            }
            int kingLeft = Math.Min(king, kingEnd);
            int kingRight = Math.Max(king, kingEnd);
            foreach (Move next in nexts)
            {
                if (next.target >= kingLeft && next.target <= kingRight)
                {
                    return true;
                }
            }
        }
        return false;
    }
    // returns if current move is checking enemy
    private bool IsCheck(Move current)
    {
        // assume empty move for opponent
        Move empty = new Move() { whiteMove = !current.whiteMove };
        foreach (Move next in FindPseudoLegalMoves(empty))
        {
            if (next.captured == Piece.King
                || next.captured == Piece.VirginKing)
            {
                return true;
            }
        }
        return false;
    }

    private void AddPiece(Piece type, int pos, bool white)
    {
        if (Occupied(pos)) {
            throw new Exception("sorry " + type + ", " + pos + " occupado");
        }
        if (white)
        {
            whitePieces[pos] = type;
        }
        else
        {
            blackPieces[pos] = type;
        }
    }
    private void RemovePiece(int pos, bool white)
    {
        if (!Occupied(pos))
            throw new Exception(pos + " no piece here");

        if (white)
        {
            whitePieces[pos] = Piece.None;
        }
        else
        {
            blackPieces[pos] = Piece.None;
        }
    }

    private void PlayMove(Move move)
    {
        if (move.type == Move.Special.Null) {
            return;
        }
        // capture target
        if (move.captured != Piece.None)
        {
            if (move.type == Move.Special.EnPassant)
            {
                RemovePiece(move.previous.target, !move.whiteMove);
            }
            else
            {
                RemovePiece(move.target, !move.whiteMove);
            }
        }

        // move source
        RemovePiece(move.source, move.whiteMove);
        if (move.type == Move.Special.Castle)
        {
            RemovePiece(move.target, move.whiteMove);

            int king=move.source, rook=move.target;
            int kingEnd, rookEnd;
            if (king < rook) // king moves right
            {
                kingEnd = castle960? GetPos(GetRank(king), NFiles-3) : king+2; // TODO: what if king is near edge?
                rookEnd = kingEnd - 1;
            }
            else // king moves left
            {
                kingEnd = castle960? GetPos(GetRank(king), 3) : king-2;
                rookEnd = kingEnd + 1;
            }

            AddPiece(Piece.King, kingEnd, move.whiteMove);
            AddPiece(Piece.Rook, rookEnd, move.whiteMove);
        }
        else if (move.promotion != Piece.None)
        {
            AddPiece(move.promotion, move.target, move.whiteMove);
        }
        else
        {
            AddPiece(move.moved, move.target, move.whiteMove);
        }
    }
    private void UndoMove(Move move)
    {
        if (move.type == Move.Special.Null) {
            return;
        }
        // move back source
        if (move.type == Move.Special.Castle)
        {
            AddPiece(Piece.VirginRook, move.target, move.whiteMove);

            int king=move.source, rook=move.target;
            int kingEnd, rookEnd;
            if (king < rook) // king moves right
            {
                kingEnd = castle960? GetPos(GetRank(king), NFiles-3) : king+2; // TODO: what if king is near edge?
                rookEnd = kingEnd - 1;
            }
            else // king moves left
            {
                kingEnd = castle960? GetPos(GetRank(king), 3) : king-2;
                rookEnd = kingEnd + 1;
            }

            RemovePiece(kingEnd, move.whiteMove);
            RemovePiece(rookEnd, move.whiteMove);
        }
        else if (move.promotion != Piece.None)
        {
            RemovePiece(move.target, move.whiteMove);
        }
        else
        {
            RemovePiece(move.target, move.whiteMove);
        }
        AddPiece(move.moved, move.source, move.whiteMove);

        // release captured
        if (move.captured != Piece.None)
        {
            if (move.type == Move.Special.EnPassant)
            {
                AddPiece(Piece.Pawn, move.previous.target, !move.whiteMove);
            }
            else
            {
                AddPiece(move.captured, move.target, !move.whiteMove);
            }
        }
    }
}