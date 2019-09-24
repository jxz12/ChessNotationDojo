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
            (whiteToMove? blackPieces.ContainsKey(targetPos)
                        : whitePieces.ContainsKey(targetPos)))
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
    // for candidate knight, king, pawn moves
    private IEnumerable<int> HopAttack(int hopper, int fileHop, int rankHop, bool whiteToMove)
    {
        int startFile = GetFile(hopper);
        int startRank = GetRank(hopper);
        int targetFile = startFile + fileHop;
        int targetRank = startRank + rankHop;
        int targetPos = GetPos(targetRank, targetFile);

        if (InBounds(targetRank, targetFile) &&
            (whiteToMove? !whitePieces.ContainsKey(targetPos)
                        : !blackPieces.ContainsKey(targetPos)))
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

    // all for castling
    private int FindVirginRook(int source, bool left, bool whiteToMove)
    {
        int startFile = GetFile(source);
        int startRank = GetRank(source);
        int fileSlide = left? -1 : 1;
        int targetFile = startFile + fileSlide;
        int targetPos = GetPos(startRank, targetFile);
        var allies = whiteToMove? whitePieces
                                : blackPieces;

        while (InBounds(startRank, targetFile))
        {
            if (Occupied(targetPos))
            {
                Piece ally;
                if (allies.TryGetValue(targetPos, out ally)
                    && ally==Piece.VirginRook)
                {
                    return targetPos;
                }
                else
                {
                    return -1;
                }
            }
            targetFile += fileSlide;
            targetPos = GetPos(startRank, targetFile);
        }
        return -1;
    }
    private int GetCastledPos(int king, bool left, bool whiteToMove)
    {
        int rank = GetRank(king);
        int file = left? LeftCastledFile : RightCastledFile;
        return GetPos(rank, file);
    }
    private bool FindCastlingRook(int king, bool left, bool whiteToMove, out int virginRook)
    {
        virginRook = FindVirginRook(king, left, whiteToMove);
        if (virginRook < 0)
            return false;
        int castledPos = GetCastledPos(king, left, whiteToMove);
        if (castledPos < 0)
            return false;
        
        // check whether all squares involved are free
        int leftmostPos =  left? Math.Min(virginRook, castledPos)
                               : Math.Min(castledPos-1, king);
        int rightmostPos = left? Math.Max(castledPos+1, king)
                               : Math.Max(virginRook, castledPos);

        for (int pos=leftmostPos; pos<=rightmostPos; pos++)
        {
            if (pos!=king && pos!=virginRook && Occupied(pos))
                return false;
        }
        return true;
    }
    private bool IsRookLeftCastle(int rook, bool whiteRook)
    {
        int rank = GetRank(rook);
        int file = GetFile(rook);
        
        var allies = whiteRook? whitePieces : blackPieces;
        int hop = 1;
        Piece kingCheck;
        while (rank-hop >= 0 && rank+hop < NFiles)
        {
            if (allies.TryGetValue(rook-hop, out kingCheck) && kingCheck == Piece.King)
                return true;
            if (allies.TryGetValue(rook+hop, out kingCheck) && kingCheck == Piece.King)
                return false;

            hop += 1;
        }
        if (rank-hop == 0)
            return true;
        else // right
            return false;
    }
    // Generate candidate moves given the current board state and previous move
    private IEnumerable<Move> FindPseudoLegalMoves(Move current)
    {
        bool whiteToMove = !current.whiteMove;
        var allies = whiteToMove? whitePieces : blackPieces;
        var enemies = whiteToMove? blackPieces : whitePieces;
        int forward = whiteToMove? NFiles : -NFiles;

        // go through each piece and generate moves
        foreach (int pos in allies.Keys)
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
                    foreach (Move m in PromotionsIfPossible(push))
                        yield return m;
                    
                    // puush
                    if (allies[pos] == Piece.VirginPawn &&
                        !Occupied(pos+2*forward))
                    {
                        var puush = new Move() {
                            whiteMove = whiteToMove,
                            source = pos,
                            target = pos + 2*forward,
                            type = Move.Special.Normal,
                            moved = Piece.VirginPawn,
                            promotion = Piece.Pawn,
                            previous = current
                        };
                        foreach (Move m in PromotionsIfPossible(puush))
                            yield return m;
                    }
                }
                
                // captures
                if (file > 0) // left
                {
                    int attack = pos+forward - 1;
                    Piece capture;
                    // pawns cannot move to an attacked square unless it's a capture
                    if (enemies.TryGetValue(attack, out capture))
                    {
                        var pish = new Move() {
                            whiteMove = whiteToMove,
                            source = pos,
                            target = attack,
                            type = Move.Special.Normal,
                            moved = allies[pos],
                            captured = capture,
                            promotion = Piece.Pawn,
                            previous = current
                        };
                        foreach (Move m in PromotionsIfPossible(pish))
                            yield return m;
                    }
                    else // virtual attacks for castle check
                    {
                        yield return new Move() {
                            source = pos,
                            target = attack,
                            type = Move.Special.None,
                        };
                    }
                }
                if (file < NFiles-1) // right
                {
                    int attack = pos+forward + 1;
                    Piece capture;
                    // pawns cannot move to an attacked square unless it's a capture
                    if (enemies.TryGetValue(attack, out capture))
                    {
                        var pish = new Move() {
                            whiteMove = whiteToMove,
                            source = pos,
                            target = attack,
                            type = Move.Special.Normal,
                            moved = allies[pos],
                            captured = capture,
                            promotion = Piece.Pawn,
                            previous = current
                        };
                        foreach (Move m in PromotionsIfPossible(pish))
                            yield return m;
                    }
                    else // virtual attacks for castle check
                    {
                        yield return new Move() {
                            source = pos,
                            target = attack,
                            type = Move.Special.None,
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
                    Piece capture;
                    enemies.TryGetValue(attack, out capture);
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = Piece.Knight,
                        captured = capture,
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.Bishop)
            {
                foreach (int attack in BishopAttacks(pos, whiteToMove))
                {
                    Piece capture;
                    enemies.TryGetValue(attack, out capture);
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = Piece.Bishop,
                        captured = capture,
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.Rook || allies[pos] == Piece.VirginRook)
            {
                foreach (int attack in RookAttacks(pos, whiteToMove))
                {
                    Piece capture;
                    enemies.TryGetValue(attack, out capture);
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = allies[pos],
                        captured = capture,
                        promotion = Piece.Rook,
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.Queen)
            {
                foreach (int attack in QueenAttacks(pos, whiteToMove))
                {
                    Piece capture;
                    enemies.TryGetValue(attack, out capture);
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = Piece.Queen,
                        captured = capture,
                        previous = current
                    };
                }
            }
            else if (allies[pos] == Piece.King || allies[pos] == Piece.VirginKing)
            {
                foreach (int attack in KingAttacks(pos, whiteToMove))
                {
                    Piece capture;
                    enemies.TryGetValue(attack, out capture);
                    yield return new Move() {
                        whiteMove = whiteToMove,
                        source = pos,
                        target = attack,
                        type = Move.Special.Normal,
                        moved = allies[pos],
                        captured = capture,
                        promotion = Piece.King,
                        previous = current
                    };
                }
                // castling
                if (allies[pos] == Piece.VirginKing)
                {
                    int leftRook;
                    if (FindCastlingRook(pos, true, whiteToMove, out leftRook))
                    {
                        yield return new Move() {
                            whiteMove = whiteToMove,
                            source = pos,
                            target = leftRook,
                            type = Move.Special.Castle,
                            moved = Piece.VirginKing,
                            previous = current
                        };
                    }
                    int rightRook;
                    if (FindCastlingRook(pos, false, whiteToMove, out rightRook))
                    {
                        yield return new Move() {
                            whiteMove = whiteToMove,
                            source = pos,
                            target = rightRook,
                            type = Move.Special.Castle,
                            moved = Piece.VirginKing,
                            previous = current
                        };
                    }
                }
            }
        }
    }

    private static Move DeepCopyMove(Move toCopy) // to make promotion simpler
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
    // convenience function to check for promotion
    private IEnumerable<Move> PromotionsIfPossible(Move pawnMove)
    {
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
            // need to also check all squares moved through when castling
            int kingLeft;
            int kingRight;
            if (current.source < current.target)
            {
                kingLeft = current.source;
                kingRight = GetPos(GetRank(current.source), RightCastledFile);
            }
            else
            {
                kingLeft = GetPos(GetRank(current.source), LeftCastledFile);
                kingRight = current.source;
            }
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
    public bool Check()
    {
        return IsCheck(prevMove);
    }
    public int NumPieces(bool white)
    {
        if (white)
            return whitePieces.Count;
        else
            return blackPieces.Count;
    }

    private void AddPiece(Piece type, int pos, bool white)
    {
        // UnityEngine.Debug.Log(type + "+" + pos);
        if (Occupied(pos))
            throw new Exception("sorry " + type + ", " + pos + " occupado");

        if (white)
        {
            whitePieces.Add(pos, type);
        }
        else
        {
            blackPieces.Add(pos, type);
        }
    }
    private void RemovePiece(Piece type, int pos, bool white)
    {
        // UnityEngine.Debug.Log(type + "-" + pos);
        if (!Occupied(pos))
            throw new Exception(pos + " no piece " + type + " here");

        if (white)
        {
            whitePieces.Remove(pos);
        }
        else
        {
            blackPieces.Remove(pos);
        }
    }

    private void PlayMove(Move move)
    {
        if (move.type == Move.Special.None)
            return;

        // capture target
        if (move.captured != Piece.None)
        {
            if (move.type == Move.Special.EnPassant)
            {
                RemovePiece(Piece.Pawn, move.previous.target, !move.whiteMove);
            }
            else
            {
                RemovePiece(move.captured, move.target, !move.whiteMove);
            }
        }

        // move source
        RemovePiece(move.moved, move.source, move.whiteMove);
        if (move.type == Move.Special.Castle)
        {
            RemovePiece(Piece.VirginRook, move.target, move.whiteMove);

            int rank = GetRank(move.source);
            bool left = move.source > move.target;
            int castledKing = GetCastledPos(move.source, left, move.whiteMove);
            int castledRook = left? castledKing+1 : castledKing-1;
            AddPiece(Piece.King, castledKing, move.whiteMove);
            AddPiece(Piece.Rook, castledRook, move.whiteMove);
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
        if (move.type == Move.Special.None)
            return;

        // move back source
        if (move.type == Move.Special.Castle)
        {
            int rank = GetRank(move.source);
            bool left = move.source > move.target;
            int castledKing = GetCastledPos(move.source, left, move.whiteMove);
            int castledRook = left? castledKing+1 : castledKing-1;
            RemovePiece(Piece.King, castledKing, move.whiteMove);
            RemovePiece(Piece.Rook, castledRook, move.whiteMove);

            AddPiece(Piece.VirginRook, move.target, move.whiteMove);
        }
        else if (move.promotion != Piece.None)
        {
            RemovePiece(move.promotion, move.target, move.whiteMove);
        }
        else
        {
            RemovePiece(move.moved, move.target, move.whiteMove);
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