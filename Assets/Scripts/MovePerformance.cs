using System;
using System.Collections.Generic;

public partial class Engine
{
    private void InitOccupancy()
    {
        whitePawnsInit = new HashSet<int>(WhitePawns);
        blackPawnsInit = new HashSet<int>(BlackPawns);

        whiteOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in WhitePawns)   whiteOccupancy[pos] = PieceType.Pawn;
        foreach (int pos in WhiteRooks)   whiteOccupancy[pos] = PieceType.Rook;
        foreach (int pos in WhiteKnights) whiteOccupancy[pos] = PieceType.Knight;
        foreach (int pos in WhiteBishops) whiteOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in WhiteQueens)  whiteOccupancy[pos] = PieceType.Queen;
        whiteOccupancy[WhiteKing] = PieceType.King;
        
        blackOccupancy = new Dictionary<int, PieceType>();
        foreach (int pos in BlackPawns)   blackOccupancy[pos] = PieceType.Pawn;
        foreach (int pos in BlackRooks)   blackOccupancy[pos] = PieceType.Rook;
        foreach (int pos in BlackKnights) blackOccupancy[pos] = PieceType.Knight;
        foreach (int pos in BlackBishops) blackOccupancy[pos] = PieceType.Bishop;
        foreach (int pos in BlackQueens)  blackOccupancy[pos] = PieceType.Queen;
        blackOccupancy[BlackKing] = PieceType.King;

        occupancy = new HashSet<int>();
        occupancy.UnionWith(whiteOccupancy.Keys);
        occupancy.UnionWith(blackOccupancy.Keys);
    }
    
    private void InitAttackTables() // relies on occupancy being filled in
    {
        whiteAttackTable = new int[nFiles*nRanks];
        blackAttackTable = new int[nFiles*nRanks];

        // pawns
        Action<int, bool> PawnAttack = (pawn, isWhite)=>
        {
            int file = GetFile(pawn), rank = GetRank(pawn);
            int nextRank = rank + (isWhite? 1 : -1);
            if (file > 0) // left capture
            {
                if (isWhite)
                {
                    whiteAttackTable[GetPos(file-1, nextRank)] += 1;
                }
                else
                {
                    blackAttackTable[GetPos(file-1, nextRank)] += 1;
                }
            }
            if (file < nFiles-1) // right capture
            {
                if (isWhite)
                {
                    whiteAttackTable[GetPos(file+1, nextRank)] += 1;
                }
                else
                {
                    blackAttackTable[GetPos(file+1, nextRank)] += 1;
                }
            }
        };
        foreach (int pawn in WhitePawns) PawnAttack(pawn, true);
        foreach (int pawn in BlackPawns) PawnAttack(pawn, false);

        // sliding pieces
        Action<int, int, int, bool> SliderAttack = (slider, fileSlide, rankSlide, isWhite)=>
        {
            int startFile = GetFile(slider);
            int startRank = GetRank(slider);
            int targetFile = startFile + fileSlide;
            int targetRank = startRank + rankSlide;
            int targetPos = GetPos(targetFile, targetRank);
            bool blocked = occupancy.Contains(targetPos);

            while (targetRank >= 0 && targetRank < nRanks &&
                   targetFile >= 0 && targetFile < nFiles &&
                   !blocked)
            {
                if (isWhite)
                {
                    whiteAttackTable[targetPos] += 1;
                }
                else
                {
                    blackAttackTable[targetPos] += 1;
                }

                targetFile += fileSlide;
                targetRank += rankSlide;
                targetPos = GetPos(targetFile, targetRank);
                blocked = occupancy.Contains(targetPos);
            }

            bool capture = isWhite? blackOccupancy.ContainsKey(targetPos)
                                  : whiteOccupancy.ContainsKey(targetPos);
            if (targetRank >= 0 && targetRank < nRanks &&
                targetFile >= 0 && targetFile < nFiles &&
                capture)
            {
                if (isWhite)
                {
                    whiteAttackTable[targetPos] += 1;
                }
                else
                {
                    blackAttackTable[targetPos] += 1;
                }
            }
        };
        Action<IEnumerable<int>, bool> BishopAttacks = (bishops, isWhite)=>
        {
            foreach (int bishop in bishops)
            {
                SliderAttack(bishop,  1,  1, isWhite);
                SliderAttack(bishop,  1, -1, isWhite);
                SliderAttack(bishop, -1, -1, isWhite);
                SliderAttack(bishop, -1,  1, isWhite);
            }
        };
        BishopAttacks(WhiteBishops, true);
        BishopAttacks(BlackBishops, false);

        Action<IEnumerable<int>, bool> RookAttacks = (rooks, isWhite)=>
        {
            foreach (int rook in rooks)
            {
                SliderAttack(rook,  0,  1, isWhite);
                SliderAttack(rook,  1,  0, isWhite);
                SliderAttack(rook,  0, -1, isWhite);
                SliderAttack(rook, -1,  0, isWhite);
            }
        };
        RookAttacks(WhiteRooks, true);
        RookAttacks(BlackRooks, false);

        Action<IEnumerable<int>, bool> QueenAttacks = (queens, isWhite)=>
        {
            foreach (int queen in queens)
            {
                SliderAttack(queen,  1,  1, isWhite);
                SliderAttack(queen,  1, -1, isWhite);
                SliderAttack(queen, -1, -1, isWhite);
                SliderAttack(queen, -1,  1, isWhite);
                SliderAttack(queen,  0,  1, isWhite);
                SliderAttack(queen,  1,  0, isWhite);
                SliderAttack(queen,  0, -1, isWhite);
                SliderAttack(queen, -1,  0, isWhite);
            }
        };

        // knights and kings
        Action<int, int, int, bool> HopAttack = (knight, fileHop, rankHop, isWhite)=>
        {
            int startFile = GetFile(knight);
            int startRank = GetRank(knight);
            int targetFile = startFile + fileHop;
            int targetRank = startRank + rankHop;
            int targetPos = GetPos(targetFile, targetRank);

            // only blocked by own pieces
            bool blocked = isWhite? whiteOccupancy.ContainsKey(targetPos)
                                  : blackOccupancy.ContainsKey(targetPos);

            if (targetRank >= 0 && targetRank < nRanks &&
                targetFile >= 0 && targetFile < nFiles &&
                (!blocked))
            {
                if (isWhite)
                {
                    whiteAttackTable[targetPos] += 1;
                }
                else
                {
                    blackAttackTable[targetPos] += 1;
                }
            }
        };
        Action<IEnumerable<int>, bool> KnightAttacks = (knights, isWhite)=>
        {
            foreach (int knight in knights)
            {
                HopAttack(knight,  1,  2, isWhite);
                HopAttack(knight,  2,  1, isWhite);
                HopAttack(knight,  2, -1, isWhite);
                HopAttack(knight,  1, -2, isWhite);
                HopAttack(knight, -1, -2, isWhite);
                HopAttack(knight, -2, -1, isWhite);
                HopAttack(knight, -2,  1, isWhite);
                HopAttack(knight, -1,  2, isWhite);
            }
        };
        KnightAttacks(WhiteKnights, true);
        KnightAttacks(BlackKnights, false);

        Action<int, bool> KingAttacks = (king, isWhite)=>
        {
            HopAttack(king,  0,  1, isWhite);
            HopAttack(king,  1,  1, isWhite);
            HopAttack(king,  1,  0, isWhite);
            HopAttack(king,  1, -1, isWhite);
            HopAttack(king,  0, -1, isWhite);
            HopAttack(king, -1,  1, isWhite);
            HopAttack(king, -1,  0, isWhite);
            HopAttack(king, -1, -1, isWhite);
        };
        KingAttacks(WhiteKing, true);
        KingAttacks(BlackKing, false);
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
            else if (type == PieceType.Rook) WhiteRooks.Add(pos);
            else if (type == PieceType.Knight) WhiteKnights.Add(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Add(pos);
            else if (type == PieceType.Queen) WhiteQueens.Add(pos);
            whiteOccupancy.Add(pos, type);
        }
        else
        {
            if (type == PieceType.Pawn) BlackPawns.Add(pos);
            else if (type == PieceType.Rook) BlackRooks.Add(pos);
            else if (type == PieceType.Knight) BlackKnights.Add(pos);
            else if (type == PieceType.Bishop) BlackBishops.Add(pos);
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
            else if (type == PieceType.Rook) WhiteRooks.Remove(pos);
            else if (type == PieceType.Knight) WhiteKnights.Remove(pos);
            else if (type == PieceType.Bishop) WhiteBishops.Remove(pos);
            else if (type == PieceType.Queen) WhiteQueens.Remove(pos);
            whiteOccupancy.Remove(pos);
        }
        else
        {
            if (type == PieceType.Pawn) BlackPawns.Remove(pos);
            else if (type == PieceType.Rook) BlackRooks.Remove(pos);
            else if (type == PieceType.Knight) BlackKnights.Remove(pos);
            else if (type == PieceType.Bishop) BlackBishops.Remove(pos);
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
        else
        {
            throw new NotImplementedException();
        }
        previous = move;
    }
    private void UndoMove(Move move)
    {
        // TODO:
    }
}