using System;
using System.Collections.Generic;
using System.Linq;

public partial class Engine
{
    // for finding candidate bishop, rook, queen moves
    // yes, I know IEnumerable is slow but it's nice
    private IEnumerable<int> SlideAttacks(int slider, int fileSlide, int rankSlide, bool whiteToMove)
    {
        int startFile = board.GetFile(slider);
        int startRank = board.GetRank(slider);
        int targetFile = startFile + fileSlide;
        int targetRank = startRank + rankSlide;
        int targetPos = board.GetPos(targetRank, targetFile);

        while (board.InBounds(targetPos) &&
               !board.Occupied(targetPos))
        {
            yield return targetPos;
            targetFile += fileSlide;
            targetRank += rankSlide;
            targetPos = board.GetPos(targetRank, targetFile);
        }

        // capture
        if (board.InBounds(targetPos) &&
            (whiteToMove? board.Black.ContainsKey(targetPos)
                        : board.White.ContainsKey(targetPos)))
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
        int startFile = board.GetFile(hopper);
        int startRank = board.GetRank(hopper);
        int targetFile = startFile + fileHop;
        int targetRank = startRank + rankHop;
        int targetPos = board.GetPos(targetRank, targetFile);

        if (board.InBounds(targetPos) &&
            (whiteToMove? !board.White.ContainsKey(targetPos)
                        : !board.Black.ContainsKey(targetPos)))
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
        int startFile = board.GetFile(source);
        int startRank = board.GetRank(source);
        int fileSlide = left? -1 : 1;
        int targetFile = startFile + fileSlide;
        int targetPos = board.GetPos(startRank, targetFile);

        while (board.InBounds(targetPos))
        {
            if (board.Occupied(targetPos))
            {
                var allies = whiteToMove? board.White
                                        : board.Black;
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
            targetPos = board.GetPos(startRank, targetFile);
        }
        return -1;
    }
    private int GetCastledPos(int king, bool left, bool whiteToMove)
    {
        int rank = board.GetRank(king);
        int file;
        if (whiteToMove)
        {
            if (left) file = board.WhiteLeftCastledFile;
            else      file = board.WhiteRightCastledFile;
        }
        else
        {
            if (left) file = board.BlackLeftCastledFile;
            else      file = board.BlackRightCastledFile;
        }
        return board.GetPos(rank, file);
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
            if (pos!=king && pos!=virginRook && board.Occupied(pos))
                return false;
        }
        return true;
    }
    // Generate candidate moves given the current board state and previous move
    private IEnumerable<Move> FindPseudoLegalMoves(Move previous)
    {
        bool whiteToMove = !previous.WhiteMove;
        var allies = whiteToMove? board.White : board.Black;
        var enemies = whiteToMove? board.Black : board.White;
        int forward = whiteToMove? board.NFiles : -board.NFiles;

        // go through each piece and generate moves
        foreach (int pos in allies.Keys)
        {
            int rank = board.GetRank(pos);
            int file = board.GetFile(pos);
            if (allies[pos] == Piece.Pawn)
            {
                // push
                // pawn should never be at last rank, so no need for bounds check
                if (!board.Occupied(pos+forward))
                {
                    var push = new Move() {
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = pos + forward,
                        Type = Move.Special.Normal,
                        Moved = allies[pos],
                        Previous = previous
                    };
                    yield return push;
                    foreach (Move m in PromotionsIfPossible(push))
                        yield return m;
                    
                    // puush
                    if (rank == (whiteToMove? 1:6) &&
                        board.InBounds(pos+2*forward) &&
                        !board.Occupied(pos+2*forward))
                    {
                        var puush = new Move() {
                            WhiteMove = whiteToMove,
                            Source = pos,
                            Target = pos + 2*forward,
                            Type = Move.Special.Normal,
                            Moved = Piece.Pawn,
                            Previous = previous
                        };
                        yield return puush;
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
                            WhiteMove = whiteToMove,
                            Source = pos,
                            Target = attack,
                            Type = Move.Special.Normal,
                            Moved = Piece.Pawn,
                            Captured = capture,
                            Previous = previous
                        };
                        yield return pish;
                        foreach (Move m in PromotionsIfPossible(pish))
                            yield return m;
                    }
                }
                if (file < board.NFiles-1) // right
                {
                    int attack = pos+forward + 1;
                    Piece capture;
                    // pawns cannot move to an attacked square unless it's a capture
                    if (enemies.TryGetValue(attack, out capture))
                    {
                        var pish = new Move() {
                            WhiteMove = whiteToMove,
                            Source = pos,
                            Target = attack,
                            Type = Move.Special.Normal,
                            Moved = Piece.Pawn,
                            Captured = capture,
                            Previous = previous
                        };
                        yield return pish;
                        foreach (Move m in PromotionsIfPossible(pish))
                            yield return m;
                    }
                }
                if (previous.Moved == Piece.Pawn
                    && previous.Target == previous.Source-2*forward
                    && rank == board.GetRank(previous.Target)
                    && Math.Abs(file - board.GetFile(previous.Target)) == 1) // enpassant
                {
                    // enpassant cannot be a promotion
                    yield return new Move() {
                        WhiteMove = whiteToMove,
                        Source = previous.Target - 1,
                        Target = previous.Target + forward,
                        Type = Move.Special.EnPassant,
                        Moved = Piece.Pawn,
                        Captured = Piece.Pawn,
                        Previous = previous
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
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = attack,
                        Type = Move.Special.Normal,
                        Moved = Piece.Knight,
                        Captured = capture,
                        Previous = previous
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
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = attack,
                        Type = Move.Special.Normal,
                        Moved = Piece.Bishop,
                        Captured = capture,
                        Previous = previous
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
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = attack,
                        Type = Move.Special.Normal,
                        Moved = allies[pos],
                        Captured = capture,
                        Promotion = allies[pos]==Piece.VirginRook
                                    ? Piece.Rook
                                    : Piece.None,
                        Previous = previous
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
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = attack,
                        Type = Move.Special.Normal,
                        Moved = Piece.Queen,
                        Captured = capture,
                        Previous = previous
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
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = attack,
                        Type = Move.Special.Normal,
                        Moved = allies[pos],
                        Captured = capture,
                        Promotion = allies[pos]==Piece.VirginKing
                                    ? Piece.King
                                    : Piece.None,
                        Previous = previous
                    };
                }
                // castling
                int leftRook;
                if (FindCastlingRook(pos, true, whiteToMove, out leftRook))
                {
                    yield return new Move() {
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = leftRook,
                        Type = Move.Special.Castle,
                        Moved = Piece.VirginKing,
                        Previous = previous
                    };
                }
                int rightRook;
                if (FindCastlingRook(pos, false, whiteToMove, out rightRook))
                {
                    yield return new Move() {
                        WhiteMove = whiteToMove,
                        Source = pos,
                        Target = rightRook,
                        Type = Move.Special.Castle,
                        Moved = Piece.VirginKing,
                        Previous = previous
                    };
                }

            }
        }

        // ////////////
        // // castle //
        // ////////////
        // // int king = whiteToMove? WhiteKing : BlackKing;
        // var kingType = whiteToMove? whitePieces[king] : blackPieces[king];
        // if (kingType == Piece.VirginKing)
        // {
        //     int leftRook;
        //     if (FindCastlingRook(king, true, whiteToMove, out leftRook))
        //     {
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = king,
        //             Target = leftRook,
        //             Type = Move.Special.Castle,
        //             Moved = Piece.VirginKing,
        //             Previous = previous
        //         };
        //     }
        //     int rightRook;
        //     if (FindCastlingRook(king, false, whiteToMove, out rightRook))
        //     {
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = king,
        //             Target = rightRook,
        //             Type = Move.Special.Castle,
        //             Moved = Piece.VirginKing,
        //             Previous = previous
        //         };
        //     }
        // }

        // ////////////////
        // // en passant //
        // ////////////////
        // int forward = whiteToMove? NFiles : -NFiles;
        // var allyPawns = whiteToMove? WhitePawns : BlackPawns;
        // var enemyPawns =  whiteToMove? BlackPawns : WhitePawns;
        // if (previous.Type == Move.Special.EnPassant)
        // {
        //     // capture from left
        //     if (previous.Target%NFiles != 0 &&
        //         allyPawns.Contains(previous.Target-1))
        //     {
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = previous.Target - 1,
        //             Target = previous.Target + forward,
        //             Type = Move.Special.EnPassant,
        //             Moved = allies[previous.Target-1],
        //             Captured = enemies[previous.Target],
        //             Previous = previous
        //         };
        //     }
        //     // capture from right
        //     if (previous.Target%NFiles != NFiles-1 &&
        //         allyPawns.Contains(previous.Target+1))
        //     {
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = previous.Target + 1,
        //             Target = previous.Target + forward,
        //             Type = Move.Special.EnPassant,
        //             Moved = allies[previous.Target+1],
        //             Captured = enemies[previous.Target],
        //             Previous = previous
        //         };
        //     }
        // }

        // ///////////
        // // pawns //
        // ///////////
        // foreach (int pawn in allyPawns)
        // {
        //     // push
        //     if (!Occupied(pawn+forward)) // pawn should never be at last rank
        //     {
        //         var push = new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = pawn,
        //             Target = pawn + forward,
        //             Type = Move.Special.Normal,
        //             Moved = allies[pawn],
        //             Previous = previous
        //         };
        //         foreach (Move m in PromotionsIfPossible(push))
        //             yield return m;
                
        //         // puush
        //         int pushedRank = GetRank(pawn + 2*forward);
        //         if (allies[pawn] == Piece.VirginPawn &&
        //             pushedRank >= 0 && pushedRank < NRanks &&
        //             !Occupied(pawn+2*forward))
        //         {
        //             var puush = new Move() {
        //                 WhiteMove = whiteToMove,
        //                 Source = pawn,
        //                 Target = pawn + 2*forward,
        //                 Type = Move.Special.Normal,
        //                 Moved = Piece.VirginPawn,
        //                 Previous = previous
        //             };
        //             foreach (Move m in PromotionsIfPossible(puush))
        //                 yield return m;
        //         }
        //     }
            
        //     // attacks
        //     int file = GetFile(pawn);
        //     if (file > 0)
        //     {
        //         // left capture
        //         int attack = pawn+forward - 1;
        //         Piece capture;
        //         // pawns cannot move to an attacked square unless it's a capture
        //         if (enemies.TryGetValue(attack, out capture))
        //         {
        //             var pish = new Move() {
        //                 WhiteMove = whiteToMove,
        //                 Source = pawn,
        //                 Target = attack,
        //                 Type = Move.Special.Normal,
        //                 Moved = allies[pawn],
        //                 Captured = capture,
        //                 Previous = previous
        //             };
        //             foreach (Move m in PromotionsIfPossible(pish))
        //                 yield return m;
        //         }
        //     }
        //     if (file < NFiles-1)
        //     {
        //         // right capture
        //         int attack = pawn+forward + 1;
        //         Piece capture;
        //         // pawns cannot move to an attacked square unless it's a capture
        //         if (enemies.TryGetValue(attack, out capture))
        //         {
        //             var pish = new Move() {
        //                 WhiteMove = whiteToMove,
        //                 Source = pawn,
        //                 Target = attack,
        //                 Type = Move.Special.Normal,
        //                 Moved = allies[pawn],
        //                 Captured = capture,
        //                 Previous = previous
        //             };
        //             foreach (Move m in PromotionsIfPossible(pish))
        //                 yield return m;
        //         }
        //     }
        // }

        // ////////////
        // // pieces //
        // ////////////

        // var knights = whiteToMove? WhiteKnights : BlackKnights;
        // foreach (int knight in knights)
        // {
        //     foreach (int attack in KnightAttacks(knight, whiteToMove))
        //     {
        //         Piece capture;
        //         enemies.TryGetValue(attack, out capture);
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = knight,
        //             Target = attack,
        //             Type = Move.Special.Normal,
        //             Moved = Piece.Knight,
        //             Captured = capture,
        //             Previous = previous
        //         };
        //     }
        // }
        // var bishops = whiteToMove? WhiteBishops : BlackBishops;
        // foreach (int bishop in bishops)
        // {
        //     foreach (int attack in BishopAttacks(bishop, whiteToMove))
        //     {
        //         Piece capture;
        //         enemies.TryGetValue(attack, out capture);
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = bishop,
        //             Target = attack,
        //             Type = Move.Special.Normal,
        //             Moved = Piece.Bishop,
        //             Captured = capture,
        //             Previous = previous
        //         };
        //     }
        // }
        // var rooks = whiteToMove? WhiteRooks : BlackRooks;
        // foreach (int rook in rooks)
        // {
        //     foreach (int attack in RookAttacks(rook, whiteToMove))
        //     {
        //         Piece capture;
        //         enemies.TryGetValue(attack, out capture);
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = rook,
        //             Target = attack,
        //             Type = Move.Special.Normal,
        //             Moved = allies[rook],
        //             Captured = capture,
        //             Promotion = allies[rook]==Piece.VirginRook
        //                          ? Piece.Rook
        //                          : Piece.None,
        //             Previous = previous
        //         };
        //     }
        // }
        // var queens = whiteToMove? WhiteQueens : BlackQueens;
        // foreach (int queen in queens)
        // {
        //     foreach (int attack in QueenAttacks(queen, whiteToMove))
        //     {
        //         Piece capture;
        //         enemies.TryGetValue(attack, out capture);
        //         yield return new Move() {
        //             WhiteMove = whiteToMove,
        //             Source = queen,
        //             Target = attack,
        //             Type = Move.Special.Normal,
        //             Moved = Piece.Queen,
        //             Captured = capture,
        //             Previous = previous
        //         };
        //     }
        // }
        // foreach (int attack in KingAttacks(king, whiteToMove))
        // {
        //     Piece capture;
        //     enemies.TryGetValue(attack, out capture);
        //     yield return new Move() {
        //         WhiteMove = whiteToMove,
        //         Source = king,
        //         Target = attack,
        //         Type = Move.Special.Normal,
        //         Moved = allies[king],
        //         Captured = capture,
        //         Promotion = allies[king]==Piece.VirginKing
        //                      ? Piece.King
        //                      : Piece.None,
        //         Previous = previous
        //     };
        // }
    }

    private static Move DeepCopyMove(Move toCopy) // to make promotion simpler
    {
        return new Move() {
            Previous = toCopy.Previous,
            WhiteMove = toCopy.WhiteMove,
            Source = toCopy.Source,
            Target = toCopy.Target,
            Type = toCopy.Type,
            Moved = toCopy.Moved,
            Captured = toCopy.Captured,
            Promotion = toCopy.Promotion
        };
    }
    // convenience function to check for promotion
    private IEnumerable<Move> PromotionsIfPossible(Move pawnMove)
    {
        int rank = board.GetRank(pawnMove.Target);
        if (rank == 0 || rank == board.NRanks-1)
        {
            // add possible promotions
            pawnMove.Promotion = Piece.Knight;
            yield return DeepCopyMove(pawnMove);
            pawnMove.Promotion = Piece.Bishop;
            yield return DeepCopyMove(pawnMove);
            pawnMove.Promotion = Piece.Rook;
            yield return DeepCopyMove(pawnMove);
            pawnMove.Promotion = Piece.Queen;
            yield return pawnMove;
        }
    }

    // returns if current has moved into check
    private bool InCheck(Move current, IEnumerable<Move> nexts)
    {
        // bool inCheck = false;
        if (current.Type != Move.Special.Castle)
        {
            // simply check for king being captured
            foreach (Move next in nexts)
            {
                if (next.Captured == Piece.King
                    || next.Captured == Piece.VirginKing)
                {
                    return true;
                }
            }
        }
        else // need to check all squares moved through
        {
            // var kingSquares = new HashSet<int>();
            // int kingBefore = current.Source;
            // int kingAfter = current.WhiteMove? WhiteKing : BlackKing;

            // // TODO: case where king moves e.g. right for a left castle
            // //       and the rook is originally shielding from the source check square
            // //       although will this situation ever happen?
            // if (kingBefore > kingAfter) // king moves left
            // {
            //     // include original square i.e. cannot move out of check
            //     for (int pos=kingBefore; pos>=kingAfter; pos--)
            //         kingSquares.Add(pos);
            // }
            // else // right
            // {
            //     for (int pos=kingBefore; pos<=kingAfter; pos++)
            //         kingSquares.Add(pos);
            // }
            // foreach (Move next in nexts)
            // {
            //     if (next.Moved != Piece.Pawn
            //         && kingSquares.Contains(next.Target))
            //     {
            //         return true;
            //     }
            // }
            // var enemyPawns = current.WhiteMove? BlackPawns : WhitePawns;
            // int forward = current.WhiteMove? -NFiles : NFiles;
            // foreach (int pawn in enemyPawns)
            // {
            //     int file = GetFile(pawn);
            //     if ((file > 0 && kingSquares.Contains(pawn+forward - 1))
            //         || (file < NFiles-1 && kingSquares.Contains(pawn+forward + 1)))
            //     {
            //         return true;
            //     }
            // }
        }
        return false;
    }
    // returns if current move is checking enemy
    private bool IsCheck(Move current)
    {
        // assume empty move for black
        Move empty = new Move() { WhiteMove = !current.WhiteMove };
        foreach (Move next in FindPseudoLegalMoves(empty))
        {
            if (next.Captured == Piece.King
                || next.Captured == Piece.VirginKing)
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

    private void AddPiece(Piece type, int pos, bool white)
    {
        if (board.Occupied(pos))
            throw new Exception(pos + " occupado");

        if (white)
        {
            // if (type == Piece.Pawn
            //     || type == Piece.VirginPawn) WhitePawns.Add(pos);
            // else if (type == Piece.Knight) WhiteKnights.Add(pos);
            // else if (type == Piece.Bishop) WhiteBishops.Add(pos);
            // else if (type == Piece.Rook
            //          || type == Piece.VirginRook) WhiteRooks.Add(pos);
            // else if (type == Piece.Queen) WhiteQueens.Add(pos);
            // else if (type == Piece.King
            //          || type == Piece.VirginKing) WhiteKing = pos;
            board.White.Add(pos, type);
        }
        else
        {
            // if (type == Piece.Pawn
            //     || type == Piece.VirginPawn) BlackPawns.Add(pos);
            // else if (type == Piece.Knight) BlackKnights.Add(pos);
            // else if (type == Piece.Bishop) BlackBishops.Add(pos);
            // else if (type == Piece.Rook
            //          || type == Piece.VirginRook) BlackRooks.Add(pos);
            // else if (type == Piece.Queen) BlackQueens.Add(pos);
            // else if (type == Piece.King
            //          || type == Piece.VirginKing) BlackKing = pos;
            board.Black.Add(pos, type);
        }
    }
    private void RemovePiece(Piece type, int pos, bool white)
    {
        if (!board.Occupied(pos))
            throw new Exception(pos + " no piece here");

        if (white)
        {
            // if (type == Piece.Pawn
            //     || type == Piece.VirginPawn) WhitePawns.Remove(pos);
            // else if (type == Piece.Knight) WhiteKnights.Remove(pos);
            // else if (type == Piece.Bishop) WhiteBishops.Remove(pos);
            // else if (type == Piece.Rook
            //          || type == Piece.VirginRook) WhiteRooks.Remove(pos);
            // else if (type == Piece.Queen) WhiteQueens.Remove(pos);
            // else if (type == Piece.King
            //          || type == Piece.VirginKing) WhiteKing = -1;
            board.White.Remove(pos);
        }
        else
        {
            // if (type == Piece.Pawn
            //     || type == Piece.VirginPawn) BlackPawns.Remove(pos);
            // else if (type == Piece.Knight) BlackKnights.Remove(pos);
            // else if (type == Piece.Bishop) BlackBishops.Remove(pos);
            // else if (type == Piece.Rook
            //          || type == Piece.VirginRook) BlackRooks.Remove(pos);
            // else if (type == Piece.Queen) BlackQueens.Remove(pos);
            // else if (type == Piece.King
            //          || type == Piece.VirginKing) BlackKing = -1;
            board.Black.Remove(pos);
        }
    }

    private void PlayMove(Move move)
    {
        if (move.Type == Move.Special.None)
            return;

        // capture target
        if (move.Captured != Piece.None)
        {
            if (move.Type == Move.Special.EnPassant)
            {
                RemovePiece(Piece.Pawn, move.Previous.Target, !move.WhiteMove);
            }
            else
            {
                RemovePiece(move.Captured, move.Target, !move.WhiteMove);
            }
        }

        // move source
        RemovePiece(move.Moved, move.Source, move.WhiteMove);
        if (move.Type == Move.Special.Castle)
        {
            RemovePiece(Piece.VirginRook, move.Target, move.WhiteMove);

            int rank = board.GetRank(move.Source);
            bool left = move.Source > move.Target;
            int castledKing = GetCastledPos(move.Source, left, move.WhiteMove);
            int castledRook = left? castledKing+1 : castledKing-1;
            AddPiece(Piece.King, castledKing, move.WhiteMove);
            AddPiece(Piece.Rook, castledRook, move.WhiteMove);
        }
        else if (move.Promotion != Piece.None)
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
        if (move.Type == Move.Special.None)
            return;

        // move back source
        if (move.Type == Move.Special.Castle)
        {
            int rank = board.GetRank(move.Source);
            bool left = move.Source > move.Target;
            int castledKing = GetCastledPos(move.Source, left, move.WhiteMove);
            int castledRook = left? castledKing+1 : castledKing-1;
            RemovePiece(Piece.King, castledKing, move.WhiteMove);
            RemovePiece(Piece.Rook, castledRook, move.WhiteMove);

            AddPiece(Piece.VirginRook, move.Target, move.WhiteMove);
        }
        else if (move.Promotion != Piece.None)
        {
            RemovePiece(move.Promotion, move.Target, move.WhiteMove);
        }
        else
        {
            RemovePiece(move.Moved, move.Target, move.WhiteMove);
        }
        AddPiece(move.Moved, move.Source, move.WhiteMove);

        // release captured
        if (move.Captured != Piece.None)
        {
            if (move.Type == Move.Special.EnPassant)
            {
                AddPiece(Piece.Pawn, move.Previous.Target, !move.WhiteMove);
            }
            else
            {
                AddPiece(move.Captured, move.Target, !move.WhiteMove);
            }
        }
    }
}