using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game

public partial class Engine
{
    private enum Piece { None=0, Pawn, Rook, Knight, Bishop, Queen, King,
                        VirginRook, VirginKing }; // for castling, en passant etc.

    private Dictionary<int, Piece> whitePieces = new Dictionary<int, Piece>();
    private Dictionary<int, Piece> blackPieces = new Dictionary<int, Piece>();

    public int NRanks { get; private set; } = 8;
    public int NFiles { get; private set; } = 8;
    // for where castled kings go, may be different in variants
    public int LeftCastledFile  { get; private set; } 
    public int RightCastledFile { get; private set; }

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public enum Special { None=0, Normal, Castle, EnPassant };

        public Move Previous = null;
        public bool WhiteMove = false;
        public int Source = 0;
        public int Target = 0;
        public Special Type = Special.None;
        public Piece Moved = Piece.None;
        public Piece Captured = Piece.None;
        public Piece Promotion = Piece.None;
    }

    // current to evaluate
    private Move prevMove;
    // TODO: private int halfMoveClock;
    private Dictionary<string, Move> legalMoves;

    public Engine(string FEN="rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                  int leftCastledFile=2, int rightCastledFile=6)// TODO:, bool puushable=true)
    {
        // board = new Board();
        NRanks = FEN.Count(c=>c=='/') + 1;
        NFiles = 0;
        foreach (char c in FEN) // count files in first rank
        {
            if (c == '/') break;
            else if (c > '0' && c <= '9') NFiles += c-'0';
            else NFiles += 1;
        }
        if (NRanks > 26 || NFiles > 9)
            throw new Exception("cannot have more than 26x9 board (blame ASCII lol)");

        int rank = NRanks-1;
        int file = -1;
        int i = 0;
        while (FEN[i] != ' ')
        {
            file += 1;
            int pos = GetPos(rank, file);
            if (FEN[i] == '/')
            {
                if (file != NFiles)
                    throw new Exception("wrong number of squares in FEN rank " + rank);

                rank -= 1;
                file = -1;
            }
            else if (FEN[i] > '0' && FEN[i] <= '9')
            {
                file += FEN[i] - '1'; // -1 because file will be incremented regardless
            }
            else if (FEN[i] == 'P') whitePieces[pos] = Piece.Pawn;
            else if (FEN[i] == 'R') whitePieces[pos] = Piece.VirginRook;
            else if (FEN[i] == 'N') whitePieces[pos] = Piece.Knight;
            else if (FEN[i] == 'B') whitePieces[pos] = Piece.Bishop;
            else if (FEN[i] == 'Q') whitePieces[pos] = Piece.Queen;
            else if (FEN[i] == 'K') whitePieces[pos] = Piece.VirginKing; // TODO: check for more than one king
            else if (FEN[i] == 'p') blackPieces[pos] = Piece.Pawn;
            else if (FEN[i] == 'r') blackPieces[pos] = Piece.VirginRook;
            else if (FEN[i] == 'n') blackPieces[pos] = Piece.Knight;
            else if (FEN[i] == 'b') blackPieces[pos] = Piece.Bishop;
            else if (FEN[i] == 'q') blackPieces[pos] = Piece.Queen;
            else if (FEN[i] == 'k') blackPieces[pos] = Piece.VirginKing;
            else throw new Exception("unexpected character " + FEN[i] + " at " + i);

            i += 1;
        }
        prevMove = new Move();

        // 
        i += 1;
        if (FEN[i] == 'w') prevMove.WhiteMove = false;
        else if (FEN[i] == 'b') prevMove.WhiteMove = true;
        else throw new Exception("unexpected character " + FEN[i] + " at " + i);

        // castling
        i += 2;
        while (FEN[i] != ' ')
        {
            if (FEN[i] == 'K')
            {
                // unvirgin all rooks to the left of white king
            }
            else if (FEN[i] == 'Q')
            {
                // unvirgin all rooks to the left of white king
            }
            else if (FEN[i] == 'k')
            {
                // unvirgin all rooks to the right of black king
            }
            else if (FEN[i] == 'q')
            {
                // unvirgin all rooks to the left of black king
            }
            else throw new Exception("unexpected character " + FEN[i] + " at " + i);

            i += 1;
        }

        // en passant
        i += 1;
        if (FEN[i] != '-')
        {
            file = FEN[i] - 'a';
            if (file < 0 || file >= NFiles)
                throw new Exception("unexpected character " + FEN[i] + " at " + i);
            else
            {
                prevMove.Moved = Piece.Pawn;
                prevMove.Source = prevMove.WhiteMove? GetPos(1, file) : GetPos(NRanks-2, file);
                prevMove.Target = prevMove.WhiteMove? GetPos(3, file) : GetPos(NRanks-4, file);
            }
        }

        // counter TODO: maybe

        LeftCastledFile = leftCastledFile;
        RightCastledFile = rightCastledFile;

        legalMoves = FindLegalMoves(prevMove);
    }

    public int GetRank(int pos) {
        return pos / NFiles;
    }
    public int GetFile(int pos) {
        return pos % NFiles;
    }
    public int GetPos(int rank, int file) {
        return rank * NFiles + file;
    }
    public bool InBounds(int rank, int file) {
        return file>=0 && file<NFiles && rank>=0 && rank<NRanks;
    }
    public bool Occupied(int pos) {
        return whitePieces.ContainsKey(pos) || blackPieces.ContainsKey(pos);
    }


    ///////////////////////////////////
    // for interface from the outside

    private static Dictionary<Piece, string> pieceStrings = new Dictionary<Piece, string>() {
        { Piece.Pawn, "♟" },
        { Piece.Rook, "♜" },
        { Piece.VirginRook, "♜" },
        { Piece.Knight, "♞" },
        { Piece.Bishop, "♝" },
        { Piece.Queen, "♛" },
        { Piece.King, "♚" },
        { Piece.VirginKing, "♚" },
    };
    public string PieceOnSquare(int pos, bool white)
    {
        Piece p;
        if (white && whitePieces.TryGetValue(pos, out p))
        {
            return pieceStrings[p];
        }
        else if (!white && blackPieces.TryGetValue(pos, out p))
        {
            return pieceStrings[p];
        }
        return null;
    }

    private string Algebraic(Move move)
    {
        var sb = new StringBuilder();
        if (move.Type == Move.Special.Castle)
        {
            if (move.Target > move.Source) sb.Append('>');
            else sb.Append('<');
        }
        else if (move.Moved == Piece.Pawn)
        {
            sb.Append((char)('a'+(move.Source%NFiles)));
            if (move.Captured != Piece.None)
            {
                sb.Append('x').Append((char)('a'+(move.Target%NFiles)));
            }
            sb.Append(move.Target/NFiles + 1);
            if (move.Promotion != Piece.None
                && move.Promotion != Piece.Pawn)
            {
                sb.Append('=');
                if (move.Promotion == Piece.Rook) sb.Append('R');
                else if (move.Promotion == Piece.Knight) sb.Append('N');
                else if (move.Promotion == Piece.Bishop) sb.Append('B');
                else if (move.Promotion == Piece.Queen) sb.Append('Q');
            }
        }
        else
        {
            if (move.Moved == Piece.Rook
                || move.Moved == Piece.VirginRook) sb.Append('R');
            else if (move.Moved == Piece.Knight) sb.Append('N');
            else if (move.Moved == Piece.Bishop) sb.Append('B');
            else if (move.Moved == Piece.Queen) sb.Append('Q');
            else if (move.Moved == Piece.King
                     || move.Moved == Piece.VirginKing) sb.Append('K');

            if (move.Captured != Piece.None) sb.Append('x');

            sb.Append((char)('a'+(move.Target%NFiles)));
            sb.Append(move.Target/NFiles + 1);
        }
        return sb.ToString();
    }
    private Dictionary<string, Move> FindLegalMoves(Move prev)
    {
        var nexts = new List<Move>(FindPseudoLegalMoves(prev));
        var ambiguous = new Dictionary<string, List<Move>>();

        foreach (Move next in nexts)
        {
            PlayMove(next);
            var nextnexts = FindPseudoLegalMoves(next);

            // if legal, add to list
            if (!InCheck(next, nextnexts))
            {
                string algebraic = Algebraic(next);
                if (ambiguous.ContainsKey(algebraic))
                {
                    ambiguous[algebraic].Add(next);
                }
                else
                {
                    ambiguous[algebraic] = new List<Move> { next };
                }
            }
            UndoMove(next);
        }
        var unambiguous = new Dictionary<string, Move>();
        foreach (string algebraic in ambiguous.Keys)
        {
            if (ambiguous[algebraic].Count == 1) // already unambiguous
            {
                unambiguous[algebraic] = ambiguous[algebraic][0];
            }
            else
            {
                // ambiguous
                foreach (Move move in ambiguous[algebraic])
                {
                    // check which coordinates (file/rank) clash
                    int file = GetFile(move.Source);
                    int rank = GetRank(move.Source);
                    bool repeatFile = false;
                    bool repeatRank = false;
                    foreach (Move clash in ambiguous[algebraic].Where(x=> x!=move))
                    {
                        repeatFile |= file == GetFile(clash.Source);
                        repeatRank |= rank == GetRank(clash.Source);
                    }
                    // if no shared file, use file
                    string disambiguated;
                    if (!repeatFile)
                    {
                        disambiguated = algebraic.Insert(1, ((char)('a'+file)).ToString());
                    }
                    else if (!repeatRank) // use rank
                    {
                        disambiguated = algebraic.Insert(1, ((char)('1'+rank)).ToString());
                    }
                    else // use both
                    {
                        disambiguated = algebraic.Insert(1, ((char)('a'+file)).ToString()
                                                            + ((char)('1'+rank)).ToString());
                    }
                    unambiguous[disambiguated] = move;
                }
            }
        }
        return unambiguous;
    }
    public void PlayMoveAlgebraic(string algebraic)
    {
        Move toPlay;
        if (legalMoves.TryGetValue(algebraic, out toPlay))
        {
            PlayMove(toPlay);
            prevMove = toPlay;
            legalMoves = FindLegalMoves(prevMove);
        }
        else
        {
            throw new Exception("move not legal");
        }
    }
    public IEnumerable<string> GetLegalMovesAlgebraic()
    {
        return legalMoves.Keys;
    }

    // returns best move algebraic and evaluation
    public Tuple<string, float> EvaluateBestMove(int ply)
    {
        float bestEval = float.MinValue;
        string bestAlgebraic = null;
        foreach (string algebraic in legalMoves.Keys)
        {
            float eval = NegaMax(legalMoves[algebraic], ply);
            if (eval > bestEval)
            {
                bestEval = eval;
                bestAlgebraic = algebraic;
            }
        }
        return Tuple.Create(bestAlgebraic, bestEval);
    }
    public void UndoLastMove()
    {
        if (prevMove != null)
        {
            UndoMove(prevMove);
            prevMove = prevMove.Previous;
            legalMoves = FindLegalMoves(prevMove);
        }
        else
        {
            throw new Exception("no moves played yet");
        }
    }
}
