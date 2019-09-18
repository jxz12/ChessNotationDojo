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

    private class Board
    {
        public int NRanks { get; set; } = 8;
        public int NFiles { get; set; } = 8;
        public int GetRank(int pos) { return pos / NFiles; }
        public int GetFile(int pos) { return pos % NFiles; }
        public int GetPos(int rank, int file) { return rank * NFiles + file; }
        public bool InBounds(int pos) { return pos>=0 && pos<(NRanks*NFiles); }
        // public bool InBounds(int rank, int file) { return file>=0 && file<NFiles && rank>=0 && rank<NRanks; }

        public Dictionary<int, Piece> White { get; private set; } = new Dictionary<int, Piece>();
        public Dictionary<int, Piece> Black { get; private set; } = new Dictionary<int, Piece>();
        public bool Occupied(int pos) { return White.ContainsKey(pos) || Black.ContainsKey(pos); }

        // for where castled kings go
        public int WhiteLeftCastledFile  { get; set; } 
        public int WhiteRightCastledFile { get; set; }
        public int BlackLeftCastledFile  { get; set; }
        public int BlackRightCastledFile { get; set; }
    }

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public enum Special { None=0, Normal, Castle, EnPassant };

        public Move Previous = null;
        public bool WhiteMove { get; set; } = false;
        public int Source { get; set; } = 0;
        public int Target { get; set; } = 0;
        public Special Type { get; set; } = Special.None;
        public Piece Moved { get; set; } = Piece.None;
        public Piece Captured { get; set; } = Piece.None;
        public Piece Promotion { get; set; } = Piece.None;
    }

    // current to evaluate
    private Move prevMove;
    private Board board;
    private Dictionary<string, Move> legalMoves;
    public Engine(string FEN="rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                  int whiteLeftCastledFile=2, int blackLeftCastledFile=2,
                  int whiteRightCastledFile=6, int blackRightCastledFile=6)
    {
        board = new Board();
        board.NRanks = FEN.Count(c=>c=='/') + 1;
        board.NFiles = 0;
        foreach (char c in FEN) // count files in first rank
        {
            if (c == '/') break;
            else if (c > '0' && c <= '9') board.NFiles += c-'0';
            else board.NFiles += 1;
        }
        if (board.NRanks > 26 || board.NFiles > 9)
            throw new Exception("cannot have more than 26x9 board (blame ASCII lol)");

        int rank = board.NRanks-1;
        int file = -1;
        int i = 0;
        while (FEN[i] != ' ')
        {
            file += 1;
            int pos = board.GetPos(rank, file);
            if (FEN[i] == '/')
            {
                if (file != board.NFiles)
                    throw new Exception("wrong number of squares in FEN rank " + rank);

                rank -= 1;
                file = -1;
            }
            else if (FEN[i] > '0' && FEN[i] <= '9')
            {
                file += FEN[i] - '1'; // -1 because file will be incremented regardless
            }
            else if (FEN[i] == 'P') board.White[pos] = Piece.Pawn;
            else if (FEN[i] == 'R') board.White[pos] = Piece.Rook;
            else if (FEN[i] == 'N') board.White[pos] = Piece.Knight;
            else if (FEN[i] == 'B') board.White[pos] = Piece.Bishop;
            else if (FEN[i] == 'Q') board.White[pos] = Piece.Queen;
            else if (FEN[i] == 'K') board.White[pos] = Piece.King; // TODO: virgins
            else if (FEN[i] == 'p') board.Black[pos] = Piece.Pawn;
            else if (FEN[i] == 'r') board.Black[pos] = Piece.Rook;
            else if (FEN[i] == 'n') board.Black[pos] = Piece.Knight;
            else if (FEN[i] == 'b') board.Black[pos] = Piece.Bishop;
            else if (FEN[i] == 'q') board.Black[pos] = Piece.Queen;
            else if (FEN[i] == 'k') board.Black[pos] = Piece.King;
            else throw new Exception("unexpected character " + FEN[i]);

            i += 1;
        }
        prevMove = new Move();

        // 
        i += 1;
        if (FEN[i] == 'w') prevMove.WhiteMove = false;
        else if (FEN[i] == 'b') prevMove.WhiteMove = true;
        else throw new Exception("unexpected character " + FEN[i]);

        // // castling
        // i += 1;
        // while (FEN[i] != ' ')
        // {

        // }

        // // en passant
        // i += 1;
        // if (FEN[i] != '-')
        // {
        //     file = FEN[i] - 'a';
        //     if (file < 0 || file >= NFiles)
        //         throw new Exception("unexpected character " + FEN[i]);
        // }

        // TODO: for 960 too pls
        UnityEngine.Debug.Log("TODO: check multiple occupations, if in check, if pawns are behind the starting square, if rooks are on either side for castling");

        board.WhiteLeftCastledFile  = whiteLeftCastledFile;
        board.WhiteRightCastledFile = whiteRightCastledFile;
        board.BlackLeftCastledFile  = blackLeftCastledFile;
        board.BlackRightCastledFile = blackRightCastledFile;

        legalMoves = FindLegalMoves(prevMove);
    }


    ///////////////////////////////////
    // for interface from the outside

    private static Dictionary<Piece, string> pieceStrings = new Dictionary<Piece, string>() {
        { Piece.Pawn, "♟" },
        { Piece.Rook, "♜" },
        { Piece.Knight, "♞" },
        { Piece.Bishop, "♝" },
        { Piece.Queen, "♛" },
        { Piece.King, "♚" }
    };
    public string PieceOnSquare(int pos, bool white)
    {
        Piece p;
        if (white && board.White.TryGetValue(pos, out p))
        {
            return pieceStrings[p];
        }
        else if (!white && board.Black.TryGetValue(pos, out p))
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
            sb.Append((char)('a'+(move.Source%board.NFiles)));
            if (move.Captured != Piece.None)
            {
                sb.Append('x').Append((char)('a'+(move.Target%board.NFiles)));
            }
            sb.Append(move.Target/board.NFiles + 1);
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

            sb.Append((char)('a'+(move.Target%board.NFiles)));
            sb.Append(move.Target/board.NFiles + 1);
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
                    int file = board.GetFile(move.Source);
                    int rank = board.GetRank(move.Source);
                    bool repeatFile = false;
                    bool repeatRank = false;
                    foreach (Move clash in ambiguous[algebraic].Where(x=> x!=move))
                    {
                        repeatFile |= file == board.GetFile(clash.Source);
                        repeatRank |= rank == board.GetRank(clash.Source);
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
