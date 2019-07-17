using System.Collections.Generic;
using System.Linq;
using System.Text;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game
// TODO: can also evaluate moves and play with other engines

public partial class Engine
{
    public static int nRanks = 8;
    public static int nFiles = 8;
    private Move previous { get; set; } = null;

    // board state
    public HashSet<int> WhitePawns   { get; private set; } = new HashSet<int> { 8,9,10,11,12,13,14,15 };
    public HashSet<int> WhiteRooks   { get; private set; } = new HashSet<int> { 0,7 };
    public HashSet<int> WhiteKnights { get; private set; } = new HashSet<int> { 1,6 };
    public HashSet<int> WhiteBishops { get; private set; } = new HashSet<int> { 2,5 };
    public HashSet<int> WhiteQueens  { get; private set; } = new HashSet<int> { 3 };

    public HashSet<int> BlackPawns   { get; private set; } = new HashSet<int> { 48,49,50,51,52,53,54,55 };
    public HashSet<int> BlackRooks   { get; private set; } = new HashSet<int> { 56,63 };
    public HashSet<int> BlackKnights { get; private set; } = new HashSet<int> { 57,62 };
    public HashSet<int> BlackBishops { get; private set; } = new HashSet<int> { 58,61 };
    public HashSet<int> BlackQueens  { get; private set; } = new HashSet<int> { 59 };

    public int WhiteKing             { get; private set; } = 4;
    public int BlackKing             { get; private set; } = 60;
    // for where castled kings go
    public int WhiteShortCastledKing { get; private set; } = 6;
    public int WhiteLongCastledKing  { get; private set; } = 2;
    public int BlackShortCastledKing { get; private set; } = 62;
    public int BlackLongCastledKing  { get; private set; } = 58;
    
    // allow for empty moves for analysis
    public enum MoveType : byte { Normal, Castle, EnPassant };
    public enum PieceType : byte { None, Pawn, Rook, Knight, Bishop, Queen, King };

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public bool WhiteMove { get; set; } = false;
        public int Source { get; set; } = 0;
        public int Target { get; set; } = 0;
        public MoveType Type { get; set; } = MoveType.Normal;
        public PieceType Moved { get; set; } = PieceType.None;
        public PieceType Captured { get; set; } = PieceType.None;
        public PieceType Promotion { get; set; } = PieceType.None;

        public Move DeepCopy()
        {
            var copy = new Move() {
                WhiteMove = WhiteMove,
                Source = Source,
                Target = Target,
                Type = Type,
                Moved = Moved,
                Captured = Captured,
                Promotion = Promotion
            };
            return copy;
        }
    }

    // for puush
    private HashSet<int> whitePawnsInit, blackPawnsInit;
    // for castling
    private HashSet<int> castlePiecesInit;

    // for checking blocks
    private HashSet<int> occupancy;
    // for checking captures
    private Dictionary<int, PieceType> whiteOccupancy, blackOccupancy;

    // attack tables
    private int[] whiteAttackTable;
    private int[] blackAttackTable;

    public Engine(int ranks=8, int files=8)
    {
        nRanks = ranks;
        nFiles = files;

        InitOccupancy();
        InitAttackTables();

        previous = new Move();
    }
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

    // for bishops, rooks, queens
    private IEnumerable<int> SliderAttacks(int slider, int fileSlide, int rankSlide, bool whiteToMove)
    {
        int startFile = GetFile(slider);
        int startRank = GetRank(slider);
        int targetFile = startFile + fileSlide;
        int targetRank = startRank + rankSlide;
        int targetPos = GetPos(targetFile, targetRank);

        while (targetFile >= 0 && targetFile < nFiles &&
               targetRank >= 0 && targetRank < nRanks &&
               !occupancy.Contains(targetPos))
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
    // for knights, kings, pawns (one move)
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

    private void InitAttackTables() // relies on occupancy being filled in
    {
        whiteAttackTable = new int[nFiles*nRanks];
        blackAttackTable = new int[nFiles*nRanks];

        foreach (int pawn in WhitePawns)
            foreach (int attack in PawnAttacks(pawn, true))
                whiteAttackTable[attack] += 1;
        foreach (int pawn in BlackPawns)
            foreach (int attack in PawnAttacks(pawn, false))
                blackAttackTable[attack] += 1;

        foreach (int knight in WhiteKnights)
            foreach (int attack in KnightAttacks(knight, true))
                whiteAttackTable[attack] += 1;
        foreach (int knight in BlackKnights)
            foreach (int attack in KnightAttacks(knight, false))
                blackAttackTable[attack] += 1;

        foreach (int bishop in WhiteBishops)
            foreach (int attack in BishopAttacks(bishop, true))
                whiteAttackTable[attack] += 1;
        foreach (int bishop in BlackBishops)
            foreach (int attack in BishopAttacks(bishop, false))
                blackAttackTable[attack] += 1;

        foreach (int rook in WhiteRooks)
            foreach (int attack in RookAttacks(rook, true))
                whiteAttackTable[attack] += 1;
        foreach (int rook in BlackBishops)
            foreach (int attack in RookAttacks(rook, false))
                blackAttackTable[attack] += 1;

        foreach (int queen in WhiteQueens)
            foreach (int attack in QueenAttacks(queen, true))
                whiteAttackTable[attack] += 1;
        foreach (int queen in BlackQueens)
            foreach (int attack in QueenAttacks(queen, false))
                blackAttackTable[attack] += 1;

        foreach (int attack in KingAttacks(WhiteKing, true))
            whiteAttackTable[attack] += 1;
        foreach (int attack in KingAttacks(BlackKing, false))
            blackAttackTable[attack] += 1;


        UnityEngine.Debug.Log("white");
        for (int i=0; i<whiteAttackTable.Length; i++)
        {
            if (whiteAttackTable[i] != 0)
                UnityEngine.Debug.Log(i+" "+whiteAttackTable[i]);
        }
        UnityEngine.Debug.Log("black");
        for (int i=0; i<blackAttackTable.Length; i++)
        {
            if (blackAttackTable[i] != 0)
                UnityEngine.Debug.Log(i+" "+blackAttackTable[i]);
        }
    }


    ///////////////////////////////////
    // for interface from the outside

    private string GetAlgebraic(Move move)
    {
        var sb = new StringBuilder();
        if (move.Type == MoveType.Castle)
        {
            if (move.Target > move.Source) sb.Append('>');
            else sb.Append('<');
        }
        else if (move.Moved == PieceType.Pawn)
        {
            sb.Append((char)('a'+(move.Source%nFiles)));
            if (move.Captured != PieceType.None)
            {
                sb.Append('x').Append((char)('a'+(move.Target%nFiles)));
            }
            sb.Append(move.Target/nFiles + 1);
            if (move.Promotion != PieceType.None)
            {
                sb.Append('=');
                if (move.Promotion == PieceType.Rook) sb.Append('R');
                else if (move.Promotion == PieceType.Knight) sb.Append('N');
                else if (move.Promotion == PieceType.Bishop) sb.Append('B');
                else if (move.Promotion == PieceType.Queen) sb.Append('Q');
            }
        }
        else
        {
            if (move.Moved == PieceType.Rook) sb.Append('R');
            else if (move.Moved == PieceType.Knight) sb.Append('N');
            else if (move.Moved == PieceType.Bishop) sb.Append('B');
            else if (move.Moved == PieceType.Queen) sb.Append('Q');
            else if (move.Moved == PieceType.King) sb.Append('K');

            // TODO: Source ambiguity
            if (move.Captured != PieceType.None) sb.Append('x');

            sb.Append((char)('a'+(move.Target%nFiles)));
            sb.Append(move.Target/nFiles + 1);
        }
        return sb.ToString();
    }

    public IEnumerable<string> GetMovesAlgebraic()
    {
        var moves = GenerateMoves();
        foreach (Move move in moves)
        {
            yield return GetAlgebraic(move);
        }
    }

    public void PerformMoveAlgebraic(string todo)
    {
        var moves = GenerateMoves();
        foreach (Move move in moves)
        {
            if (GetAlgebraic(move) == todo)
            {
                PerformMove(move);
                return;
            }
        }
    }
    public void UndoLastMove()
    {
        // TODO:
    }
}
