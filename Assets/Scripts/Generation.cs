using System;
using System.Collections.Generic;
using System.Linq;

public partial class Engine
{
    // convenience
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
    private bool Occupied(int pos)
    {
        return whiteOccupancy.ContainsKey(pos) || blackOccupancy.ContainsKey(pos);
    }

    // for finding candidate bishop, rook, queen moves
    private IEnumerable<int> SliderAttacks(int slider, int fileSlide, int rankSlide, bool whiteToMove)
    {
        int startFile = GetFile(slider);
        int startRank = GetRank(slider);
        int targetFile = startFile + fileSlide;
        int targetRank = startRank + rankSlide;
        int targetPos = GetPos(targetFile, targetRank);

        while (targetFile >= 0 && targetFile < nFiles &&
               targetRank >= 0 && targetRank < nRanks &&
               !Occupied(targetPos))
        {
            yield return targetPos;
            targetFile += fileSlide;
            targetRank += rankSlide;
            targetPos = GetPos(targetFile, targetRank);
        }

        if (targetFile >= 0 && targetFile < nFiles &&
            targetRank >= 0 && targetRank < nRanks &&
            (whiteToMove? blackOccupancy.ContainsKey(targetPos)
                        : whiteOccupancy.ContainsKey(targetPos)))
        {
            yield return targetPos;
        }
    }
    private IEnumerable<int> BishopAttacks(int bishop, bool whiteToMove)
    {
        return         SliderAttacks(bishop,  1,  1, whiteToMove)
               .Concat(SliderAttacks(bishop,  1, -1, whiteToMove))
               .Concat(SliderAttacks(bishop, -1, -1, whiteToMove))
               .Concat(SliderAttacks(bishop, -1,  1, whiteToMove));
    }
    private IEnumerable<int> RookAttacks(int rook, bool whiteToMove)
    {
        return         SliderAttacks(rook,  0,  1, whiteToMove)
               .Concat(SliderAttacks(rook,  1,  0, whiteToMove))
               .Concat(SliderAttacks(rook,  0, -1, whiteToMove))
               .Concat(SliderAttacks(rook, -1,  0, whiteToMove));
    }
    private IEnumerable<int> QueenAttacks(int queen, bool whiteToMove)
    {
        return       BishopAttacks(queen, whiteToMove)
               .Concat(RookAttacks(queen, whiteToMove));
    }
    // for candidate knight, king, pawn moves
    private IEnumerable<int> HopperAttack(int hopper, int fileHop, int rankHop, bool whiteToMove)
    {
        int startFile = GetFile(hopper);
        int startRank = GetRank(hopper);
        int targetFile = startFile + fileHop;
        int targetRank = startRank + rankHop;
        int targetPos = GetPos(targetFile, targetRank);

        if (targetFile >= 0 && targetFile < nFiles &&
            targetRank >= 0 && targetRank < nRanks &&
            (whiteToMove? !whiteOccupancy.ContainsKey(targetPos)
                        : !blackOccupancy.ContainsKey(targetPos)))
            // only blocked by own pieces
        {
            yield return targetPos;
        }
    }
    private IEnumerable<int> PawnAttacks(int pawn, bool whiteToMove)
    {
        if (whiteToMove)
        {
            return         HopperAttack(pawn,  1, 1, whiteToMove)
                   .Concat(HopperAttack(pawn, -1, 1, whiteToMove));
        }
        else
        {
            return         HopperAttack(pawn,  1, -1, whiteToMove)
                   .Concat(HopperAttack(pawn, -1, -1, whiteToMove));
        }
    }
    private IEnumerable<int> KnightAttacks(int knight, bool whiteToMove)
    {
        return         HopperAttack(knight,  1,  2, whiteToMove)
               .Concat(HopperAttack(knight,  2,  1, whiteToMove))
               .Concat(HopperAttack(knight,  2, -1, whiteToMove))
               .Concat(HopperAttack(knight,  1, -2, whiteToMove))
               .Concat(HopperAttack(knight, -1, -2, whiteToMove))
               .Concat(HopperAttack(knight, -2, -1, whiteToMove))
               .Concat(HopperAttack(knight, -2,  1, whiteToMove))
               .Concat(HopperAttack(knight, -1,  2, whiteToMove));
    }
    private IEnumerable<int> KingAttacks(int king, bool whiteToMove)
    {
        return         HopperAttack(king,  0,  1, whiteToMove)
               .Concat(HopperAttack(king,  1,  1, whiteToMove))
               .Concat(HopperAttack(king,  1,  0, whiteToMove))
               .Concat(HopperAttack(king,  1, -1, whiteToMove))
               .Concat(HopperAttack(king,  0, -1, whiteToMove))
               .Concat(HopperAttack(king, -1,  1, whiteToMove))
               .Concat(HopperAttack(king, -1,  0, whiteToMove))
               .Concat(HopperAttack(king, -1, -1, whiteToMove));
    }

    // all for castling
    private int FindVirginRook(int source, bool left, bool whiteToMove)
    {
        int startFile = GetFile(source);
        int startRank = GetRank(source);
        int fileSlide = left? -1 : 1;
        int targetFile = startFile + fileSlide;
        int targetPos = GetPos(targetFile, startRank);

        while (targetFile >= 0 && targetFile < nFiles)
        {
            if (Occupied(targetPos))
            {
                var allies = whiteToMove? whiteOccupancy
                                        : blackOccupancy;
                PieceType ally;
                allies.TryGetValue(targetPos, out ally);
                return ally==PieceType.VirginRook? targetPos 
                                                 : -1;
            }
            targetFile += fileSlide;
            targetPos = GetPos(targetFile, startRank);
        }
        return -1;
    }
    private int GetCastledPos(int king, bool left, bool whiteToMove)
    {
        int rank = GetRank(king);
        int file;
        if (whiteToMove)
        {
            if (left) file = WhiteLeftCastledFile;
            else      file = WhiteRightCastledFile;
        }
        else
        {
            if (left) file = BlackLeftCastledFile;
            else      file = BlackRightCastledFile;
        }
        return GetPos(file, rank);
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
        int leftmostPos = left? Math.Min(virginRook, castledPos)
                              : Math.Min(castledPos-1, king);
        int rightmostPos = left? Math.Max(castledPos+1, king)
                               : Math.Max(virginRook, castledPos);

        var allies = whiteToMove? whiteOccupancy : blackOccupancy;
        for (int pos=leftmostPos; pos<=rightmostPos; pos++)
        {
            if (pos!=king && pos!=virginRook && Occupied(pos))
                return false;
        }
        return true;
    }
    // Generate candidate moves given the current board state and previous move
    private IEnumerable<Move> FindPseudoLegalMoves(Move previous)
    {
        bool whiteToMove = !previous.WhiteMove;
        var allies = whiteToMove? whiteOccupancy : blackOccupancy;
        var enemies = whiteToMove? blackOccupancy : whiteOccupancy;

        ////////////
        // castle //
        ////////////
        int king = whiteToMove? WhiteKing : BlackKing;
        var kingType = whiteToMove? whiteOccupancy[king] : blackOccupancy[king];
        if (kingType == PieceType.VirginKing)
        {
            int leftRook;
            if (FindCastlingRook(king, true, whiteToMove, out leftRook))
            {
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = king,
                    Target = leftRook,
                    Type = MoveType.Castle,
                    Moved = PieceType.VirginKing,
                    Previous = previous
                };
            }
            int rightRook;
            if (FindCastlingRook(king, false, whiteToMove, out rightRook))
            {
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = king,
                    Target = rightRook,
                    Type = MoveType.Castle,
                    Moved = PieceType.VirginKing,
                    Previous = previous
                };
            }
        }

        ////////////////
        // en passant //
        ////////////////
        int forward = whiteToMove? nFiles : -nFiles;
        var allyPawns = whiteToMove? WhitePawns : BlackPawns;
        var enemyPawns =  whiteToMove? BlackPawns : WhitePawns;
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
                    Captured = enemies[previous.Target],
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
                    Moved = allies[previous.Target+1],
                    Captured = enemies[previous.Target],
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
                PieceType capture;
                // pawns cannot move to an attacked square unless it's a capture
                if (enemies.TryGetValue(attack, out capture))
                {
                    var pish = new Move() {
                        WhiteMove = whiteToMove,
                        Source = pawn,
                        Target = attack,
                        Type = MoveType.Normal,
                        Moved = allies[pawn],
                        Captured = capture,
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
                PieceType capture;
                enemies.TryGetValue(attack, out capture);
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = knight,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Knight,
                    Captured = capture,
                    Previous = previous
                };
            }
        }
        var bishops = whiteToMove? WhiteBishops : BlackBishops;
        foreach (int bishop in bishops)
        {
            foreach (int attack in BishopAttacks(bishop, whiteToMove))
            {
                PieceType capture;
                enemies.TryGetValue(attack, out capture);
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = bishop,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Bishop,
                    Captured = capture,
                    Previous = previous
                };
            }
        }
        var rooks = whiteToMove? WhiteRooks : BlackRooks;
        foreach (int rook in rooks)
        {
            foreach (int attack in RookAttacks(rook, whiteToMove))
            {
                PieceType capture;
                enemies.TryGetValue(attack, out capture);
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = rook,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = allies[rook],
                    Captured = capture,
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
                PieceType capture;
                enemies.TryGetValue(attack, out capture);
                yield return new Move() {
                    WhiteMove = whiteToMove,
                    Source = queen,
                    Target = attack,
                    Type = MoveType.Normal,
                    Moved = PieceType.Queen,
                    Captured = capture,
                    Previous = previous
                };
            }
        }
        foreach (int attack in KingAttacks(king, whiteToMove))
        {
            PieceType capture;
            enemies.TryGetValue(attack, out capture);
            yield return new Move() {
                WhiteMove = whiteToMove,
                Source = king,
                Target = attack,
                Type = MoveType.Normal,
                Moved = allies[king],
                Captured = capture,
                Promotion = allies[king]==PieceType.VirginKing
                             ? PieceType.King
                             : PieceType.None,
                Previous = previous
            };
        }
    }
    // convenience function to check for promotion
    private IEnumerable<Move> PromotionsIfPossible(Move pawnMove)
    {
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

    // returns if current has moved into check
    private bool InCheck(Move current, IEnumerable<Move> nexts)
    {
        bool inCheck = false;
        if (current.Type != MoveType.Castle)
        {
            // simply check for king being captured
            foreach (Move next in nexts)
            {
                if (next.Captured == PieceType.King
                    || next.Captured == PieceType.VirginKing)
                {
                    inCheck = true;
                    break;
                }
            }
        }
        else // need to check all squares moved through
        {
            var kingSquares = new HashSet<int>();
            int kingBefore = current.Source;
            int kingAfter = current.WhiteMove? WhiteKing : BlackKing;

            if (kingBefore > kingAfter) // move left
            {
                for (int pos=kingBefore; pos>kingAfter; pos--)
                    kingSquares.Add(pos);
            }
            else
            {
                for (int pos=kingBefore; pos<kingAfter; pos++)
                    kingSquares.Add(pos);
            }
            foreach (Move next in nexts)
            {
                if (next.Captured == PieceType.King
                    || next.Captured == PieceType.VirginKing
                    || kingSquares.Contains(next.Target))
                {
                    inCheck = true;
                    break;
                }
            }
        }
        return inCheck;
    }
    // returns if current move is checking enemy
    private bool IsCheck(Move current)
    {
        // assume empty move for black
        Move empty = new Move() { WhiteMove = !current.WhiteMove };
        foreach (Move next in FindPseudoLegalMoves(empty))
        {
            if (next.Captured == PieceType.King
                || next.Captured == PieceType.VirginKing)
            {
                return true;
            }
        }
        return false;
    }
    public bool Check()
    {
        return IsCheck(lastPlayed);
    }

    private void AddPiece(PieceType type, int pos, bool white)
    {
        if (Occupied(pos))
            throw new Exception(pos + " occupado");

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
            throw new Exception(pos + " no piece here");

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
        if (move.Type == MoveType.None)
            return;

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
        if (move.Type == MoveType.Castle)
        {
            RemovePiece(PieceType.VirginRook, move.Target, move.WhiteMove);

            int rank = GetRank(move.Source);
            bool left = move.Source > move.Target;
            int castledKing = GetCastledPos(move.Source, left, move.WhiteMove);
            int castledRook = left? castledKing+1 : castledKing-1;
            AddPiece(PieceType.King, castledKing, move.WhiteMove);
            AddPiece(PieceType.Rook, castledRook, move.WhiteMove);
        }
        else if (move.Promotion != PieceType.None)
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
        if (move.Type == MoveType.None)
            return;

        // move back source
        if (move.Type == MoveType.Castle)
        {
            int rank = GetRank(move.Source);
            bool left = move.Source > move.Target;
            int castledKing = GetCastledPos(move.Source, left, move.WhiteMove);
            int castledRook = left? castledKing+1 : castledKing-1;
            RemovePiece(PieceType.King, castledKing, move.WhiteMove);
            RemovePiece(PieceType.Rook, castledRook, move.WhiteMove);

            AddPiece(PieceType.VirginRook, move.Target, move.WhiteMove);
        }
        else if (move.Promotion != PieceType.None)
        {
            RemovePiece(move.Promotion, move.Target, move.WhiteMove);
        }
        else
        {
            RemovePiece(move.Moved, move.Target, move.WhiteMove);
        }
        AddPiece(move.Moved, move.Source, move.WhiteMove);

        // release captured
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