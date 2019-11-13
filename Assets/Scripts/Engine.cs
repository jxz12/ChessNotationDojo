﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

////////////////////////////////////////////////////////
// a class to save all the information of a chess game

public partial class Engine
{
    private enum Piece { None=0, Pawn, Rook, Knight, Bishop, Queen, King,
                         VirginPawn, VirginRook, VirginKing }; // for castling, en passant etc.

    private List<Piece> whitePieces;
    private List<Piece> blackPieces;
    // private Dictionary<int, int> castles; // FIXME:

    public int NRanks { get; private set; } = 8;
    public int NFiles { get; private set; } = 8;

    // a class to store all the information needed for a move
    // a Move plus the board state is all the info needed for move generation
    private class Move
    {
        public enum Special { None=0, Normal, Castle, Puush, EnPassant };

        public Move previous = null;
        public bool whiteMove = false;
        public int source = 0;
        public int target = 0;
        public Special type = Special.None;
        public Piece moved = Piece.None;
        public Piece captured = Piece.None;
        public Piece promotion = Piece.None;
        public int halfMoveClock = 0;
    }

    // current to evaluate
    private Move prevMove;
    private int moveCount;

    public Engine(string FEN="rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w AHah - 0 1",
                  int puush=2, bool castle960=false)
    {
        // board = new Board();
        NRanks = FEN.Count(c=>c=='/') + 1;
        NFiles = 0;
        foreach (char c in FEN)
        {
            if (c == '/')
                break;
            else if (c >= '1' && c <= '9')
                NFiles += c - '0';
            else
                NFiles += 1;
        }
        if (NFiles > 26 || NRanks > 16)
            throw new Exception("cannot have more than 26x16 board (blame ASCII lol)");

        whitePieces = new List<Piece>(NRanks*NFiles);
        blackPieces = new List<Piece>(NRanks*NFiles);
        for (int pos=0; pos<NRanks*NFiles; pos++)
        {
            whitePieces.Add(Piece.None);
            blackPieces.Add(Piece.None);
        }

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
            else if (FEN[i] >= '1' && FEN[i] <= '9')
            {
                file += FEN[i] - '1'; // -1 because file will be incremented regardless
            }
            else if (FEN[i] == 'P') whitePieces[pos] = GetRank(pos)==1?
                                                       Piece.VirginPawn:Piece.Pawn;
            else if (FEN[i] == 'R') whitePieces[pos] = Piece.Rook;
            else if (FEN[i] == 'N') whitePieces[pos] = Piece.Knight;
            else if (FEN[i] == 'B') whitePieces[pos] = Piece.Bishop;
            else if (FEN[i] == 'Q') whitePieces[pos] = Piece.Queen;
            else if (FEN[i] == 'K') whitePieces[pos] = Piece.VirginKing;

            else if (FEN[i] == 'p') blackPieces[pos] = GetRank(pos)==NRanks-2?
                                                       Piece.VirginPawn:Piece.Pawn;
            else if (FEN[i] == 'r') blackPieces[pos] = Piece.Rook;
            else if (FEN[i] == 'n') blackPieces[pos] = Piece.Knight;
            else if (FEN[i] == 'b') blackPieces[pos] = Piece.Bishop;
            else if (FEN[i] == 'q') blackPieces[pos] = Piece.Queen;
            else if (FEN[i] == 'k') blackPieces[pos] = Piece.VirginKing;
            else throw new Exception("unexpected character " + FEN[i] + " at " + i);

            i += 1;
        }

        // who to move
        prevMove = new Move();
        i += 1;
        if (FEN[i] == 'w') prevMove.whiteMove = false;
        else if (FEN[i] == 'b') prevMove.whiteMove = true;
        else throw new Exception("unexpected character " + FEN[i] + " at " + i);

        // FIXME:
        // castling I HATE YOU
        // i += 2;
        // bool K,Q,k,q;
        // K=Q=k=q=false;
        // while (FEN[i] != ' ')
        // {
        //     if (FEN[i] == 'K') K = true;
        //     else if (FEN[i] == 'Q') Q = true;
        //     else if (FEN[i] == 'k') k = true;
        //     else if (FEN[i] == 'q') q = true;
        //     else if (FEN[i] == '-') {}
        //     else throw new Exception("unexpected character " + FEN[i] + " at " + i);

        //     i += 1;
        // }
        // foreach (int pos in whitePieces)
        // {
        //     if (whitePieces[pos] == Piece.Rook)
        //     {
        //         bool leftRook = IsRookLeftCastle(pos, true);
        //         if (leftRook && K)
        //             whitePieces[pos] = Piece.VirginRook;
        //         if (!leftRook && Q)
        //             whitePieces[pos] = Piece.VirginRook;
        //     }
        // }
        // foreach (int pos in blackPieces)
        // {
        //     if (blackPieces[pos] == Piece.Rook)
        //     {
        //         bool leftRook = IsRookLeftCastle(pos, false);
        //         if (leftRook && k)
        //             blackPieces[pos] = Piece.VirginRook;
        //         if (!leftRook && q)
        //             blackPieces[pos] = Piece.VirginRook;
        //     }
        // }

        // FIXME:
        // en passant
        // i += 1;
        // if (FEN[i] != '-')
        // {
        //     file = FEN[i] - 'a';
        //     if (file < 0 || file >= NFiles)
        //         throw new Exception("unexpected character " + FEN[i] + " at " + i);
        //     else
        //     {
        //         prevMove.moved = Piece.VirginPawn;
        //         prevMove.source = prevMove.whiteMove? GetPos(1, file) : GetPos(NRanks-2, file);
        //         prevMove.target = prevMove.whiteMove? GetPos(3, file) : GetPos(NRanks-4, file);
        //     }
        // }

        // TODO: half and full move clocks
        // TODO: UCI extended notation
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
        // return whitePieces.ContainsKey(pos) || blackPieces.ContainsKey(pos);
        return whitePieces[pos] != Piece.None || blackPieces[pos] != Piece.None;
    }


    ///////////////////////////////////
    // for interface from the outside

    private Dictionary<string, Move> legalMoves;
    public void PlayPGN(string algebraic)
    {
        Move toPlay;
        if (legalMoves.TryGetValue(algebraic, out toPlay))
        {
            PlayMove(toPlay);
            prevMove = toPlay;
            legalMoves = FindLegalMoves(prevMove);
            moveCount += 1;
        }
        else
        {
            throw new Exception("move not legal");
        }
    }
    public IEnumerable<string> GetPGNs()
    {
        return legalMoves.Keys;
    }
    public string GetLastUCI() // not real UCI because it isn't smart enough for other boards
    {
        var sb = new StringBuilder();
        sb.Append((char)('a'+GetFile(prevMove.source)));
        sb.Append(GetRank(prevMove.source));
        sb.Append((char)('a'+GetFile(prevMove.target)));
        sb.Append(GetRank(prevMove.target));

        // FIXME:
        // if (prevMove.type == Move.Special.Castle)
        //     sb.Append(fuck castling);
        // FIXME:
        // if (prevMove.type == Move.Special.EnPassant)
        //     sb.Append("x").Append();
        if (prevMove.promotion != Piece.None)
            sb.Append(pieceStrings[prevMove.promotion]);

        return sb.ToString();
    }
    public void UndoLastMove()
    {
        if (prevMove != null)
        {
            UndoMove(prevMove);
            prevMove = prevMove.previous;
            legalMoves = FindLegalMoves(prevMove);
            moveCount -= 1;
        }
        else
        {
            throw new Exception("no moves played yet");
        }
    }

    private static Dictionary<Piece, string> pieceStrings = new Dictionary<Piece, string>() {
        { Piece.Pawn, "p" },
        { Piece.VirginPawn, "p" },
        { Piece.Rook, "r" },
        { Piece.VirginRook, "r" },
        { Piece.Knight, "n" },
        { Piece.Bishop, "b" },
        { Piece.Queen, "q" },
        { Piece.King, "k" },
        { Piece.VirginKing, "k" },
    };

    public string ToFEN()
    {
        var sb = new StringBuilder();

        int empty = 0;
        int rank = NRanks-1;
        int file = 0;
        while (rank >= 0)
        {
            int pos = GetPos(rank, file);
            if (whitePieces[pos] == Piece.None && blackPieces[pos] == Piece.None)
            {
                empty += 1;
            }
            else
            {
                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }
                if (whitePieces[pos] != Piece.None && blackPieces[pos] != Piece.None)
                {
                    throw new Exception("white and black on same square boo");
                }
                else if (whitePieces[pos] != Piece.None)
                {
                    sb.Append(pieceStrings[whitePieces[pos]].ToUpper());
                }
                else // if (blackPieces[pos] != Piece.None)
                {
                    sb.Append(pieceStrings[blackPieces[pos]]);
                }
            }
            file += 1;
            if (file >= NFiles)
            {
                file = 0;
                rank -= 1;
                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }
                if (rank >= 0)
                    sb.Append('/');
            }
        }

        // who to move
        sb.Append(' ').Append(moveCount%2==0? 'w' : 'b');

        // castling TODO: use shredder fen for this
        sb.Append(" FUCKTHISGAME");

        // en passant
        if (prevMove.type == Move.Special.Puush)
            sb.Append(' ').Append('a' + GetFile(prevMove.target)).Append(GetRank(prevMove.target));
        else
            sb.Append(' ').Append("-");

        // half move clock
        sb.Append(' ').Append(prevMove.halfMoveClock);

        // full move clock
        sb.Append(' ').Append(moveCount/2 + 1);

        return sb.ToString();
    }

    private string ToPGN(Move move)
    {
        var sb = new StringBuilder();
        if (move.type == Move.Special.Castle)
        {
            // TODO: change this into Ka1 or Kh1 (drop king on rook)
            if (move.target > move.source) sb.Append('>');
            else sb.Append('<');
        }
        else if (move.moved == Piece.Pawn || move.moved == Piece.VirginPawn)
        {
            sb.Append((char)('a'+(move.source%NFiles)));
            if (move.captured != Piece.None)
            {
                sb.Append('x').Append((char)('a'+(move.target%NFiles)));
            }
            sb.Append(move.target/NFiles + 1);
            if (move.promotion != Piece.None
                && move.promotion != Piece.Pawn)
            {
                sb.Append('=');
                sb.Append(pieceStrings[move.promotion]);
            }
        }
        else
        {
            sb.Append(pieceStrings[move.moved]);
            if (move.captured != Piece.None)
                sb.Append('x');

            sb.Append((char)('a'+(move.target%NFiles)));
            sb.Append(move.target/NFiles + 1);
        }
        return sb.ToString();
    }
    private Dictionary<string, Move> FindLegalMoves(Move prev)
    {
        var nexts = new List<Move>(FindPseudoLegalMoves(prev));
        var ambiguous = new Dictionary<string, List<Move>>();

        foreach (Move next in nexts)
        {
            if (next.type == Move.Special.None)
                continue;

            PlayMove(next);
            var nextnexts = FindPseudoLegalMoves(next);

            // if legal, add to list
            if (!InCheck(next, nextnexts))
            {
                string algebraic = ToPGN(next);
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
                    int file = GetFile(move.source);
                    int rank = GetRank(move.source);
                    bool repeatFile = false;
                    bool repeatRank = false;
                    foreach (Move clash in ambiguous[algebraic].Where(x=> x!=move))
                    {
                        repeatFile |= file == GetFile(clash.source);
                        repeatRank |= rank == GetRank(clash.source);
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

}
