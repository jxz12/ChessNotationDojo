using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

public class Menu : MonoBehaviour
{
    [SerializeField] GameManager gm;
    [SerializeField] TextAsset input2, input3, input4, quotes;
    [SerializeField] TMPro.TextMeshProUGUI progress2, progress3, progress4;
    [SerializeField] BoardAscii board;

    public void StartFullGame()
    {
        // "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w AHah -"
        // "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - "
        // "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w ah - 0 1"
        // "r2qkb1r/pp2nppp/3p4/2pNN1B1/2BnP3/3P4/PPP2PPP/R2bK2R w AHah - 1 0"
        gm.StartFullGame();
    }
    public void StartPeasantsRevolt()
    {
        gm.StartFullGame("1nn1knn1/4p3/8/8/8/8/PPPPPPPP/4K3 w - - 0 1");
    }
    public void StartHordeChess()
    {
        gm.StartFullGame("ppp2ppp/pppppppp/pppppppp/pppppppp/3pp3/8/PPPPPPPP/RNBQKBNR w AH - 0 1", 1);
    }
    public void StartDoubleChess()
    {
        gm.StartFullGame("rnbqkbnrrnbqkbnr/pppppppppppppppp/88/88/88/88/88/88/88/88/PPPPPPPPPPPPPPPP/RNBQKBNRRNBQKBNR w AHGPahgp - 0 1", 5);
    }
    public void StartChess960()
    {
        // Platonic solids method
        int tetra = UnityEngine.Random.Range(0, 4);
        int cube = UnityEngine.Random.Range(0, 6);
        int octa = UnityEngine.Random.Range(0, 8);
        // int dodeca = UnityEngine.Random.Range(0, 12);
        int ico = UnityEngine.Random.Range(0, 20);

        var sb = new StringBuilder("xxxxxxxx");
        sb[octa] = 'b';
        sb[2*tetra + ((octa+1)%2)] = 'b';
        sb[XthEmptySquare960(cube, sb)] = 'q';
        sb[XthEmptySquare960(ico/4, sb)] = 'n';
        sb[XthEmptySquare960(ico%4, sb)] = 'n';
        int leftRook = XthEmptySquare960(0, sb);
        sb[leftRook] = 'r';
        int king = XthEmptySquare960(0, sb);
        sb[king] = 'k';
        int rightRook = XthEmptySquare960(0, sb);
        sb[rightRook] = 'r';

        sb.Append("/pppppppp/8/8/8/8/PPPPPPPP/");
        sb.Append(sb.ToString(0,8).ToUpper());
        sb.Append(" w ")
          .Append((char)('A'+leftRook))
          .Append((char)('A'+rightRook))
          .Append((char)('a'+leftRook))
          .Append((char)('a'+rightRook))
          .Append(" -");

        gm.StartFullGame(sb.ToString());
    }
    private int XthEmptySquare960(int x, StringBuilder sb)
    {
        for (int i=0; i<8; i++)
        {
            if (sb[i] == 'x')
            {
                if (x == 0)
                    return i;
                else
                    x -= 1;
            }
        }
        throw new Exception("not enough empty squares remaining");
    }
    public void StartMicroChess()
    {
        gm.StartFullGame("knbr/p3/4/3P/RBNK w Da - 0 1", 1);
    }
    public void StartDemiChess()
    {
        gm.StartFullGame("kbnr/pppp/4/4/4/4/pppp/KBNR w Aa - 0 1", 2);
    }
    public void StartBabyChess()
    {
        gm.StartFullGame("kqbnr/ppppp/5/PPPPP/RNBQK w - - 0 1", 1);
    }

    public class Puzzle
    {
        public string name;
        public string FEN;
        public string PGN;
        public bool solved;
    }
    List<Puzzle> puzzles2, puzzles3, puzzles4;
    void Awake()
    {
        puzzles2 = InitPuzzles(input2.text, Application.persistentDataPath+"/m8n2.gd");
        puzzles3 = InitPuzzles(input3.text, Application.persistentDataPath+"/m8n3.gd");
        puzzles4 = InitPuzzles(input4.text, Application.persistentDataPath+"/m8n4.gd");
        ShowAllProgress();

        quotesList = LoadQuotes(quotes.text);
    }

    List<Puzzle> InitPuzzles(string input, string path)
    {
        List<Puzzle> puzzles = null;
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.OpenRead(path);
            puzzles = (List<Puzzle>)bf.Deserialize(fs);
            fs.Close();
        }
        catch (Exception e)
        {
            // print(e);
            print("Save file not present, so reading from TextAsset");
            puzzles = new List<Puzzle>();
            foreach (string line in Regex.Split(input, "\r\n|\r|\n"))
            {
                if (Regex.IsMatch(line, @".*vs.*"))
                {
                    puzzles.Add(new Puzzle());
                    puzzles[puzzles.Count-1].name = line;
                    puzzles[puzzles.Count-1].solved = false;
                }
                else if (Regex.IsMatch(line, @"(([prnbqkPRNBQK12345678]*/){7})([prnbqkPRNBQK12345678]*).*"))
                {
                    puzzles[puzzles.Count-1].FEN = line;
                }
                else if (Regex.IsMatch(line, @"1\..*"))
                {
                    puzzles[puzzles.Count-1].PGN = line;
                }
            }
            SavePuzzle(puzzles, path);
        }
        return puzzles;
    }
    List<Tuple<string, string>> quotesList;
    List<Tuple<string, string>> LoadQuotes(string input)
    {
        var list = new List<Tuple<string, string>>();
        StringBuilder quote = new StringBuilder(), author = new StringBuilder();
        bool quoting = false, authoring = false;
        foreach (char c in input)
        {
            if (c == '"')
            {
                if (quoting)
                {
                    quoting = false;
                    if (quote[quote.Length-1] != '.' && quote[quote.Length-1] != '?' && quote[quote.Length-1] != '!' && quote[quote.Length-1] != '\'')
                        quote.Append('.');
                }
                else
                {
                    quoting = true;
                }
            }
            else if (c == '(' && !quoting)
            {
                if (quoting) throw new Exception("MAD");
                authoring = true;
            }
            else if (c == ')' && !quoting)
            {
                authoring = false;
                list.Add(Tuple.Create(author.ToString(), quote.ToString()));
                author.Clear();
                quote.Clear();
            }
            else
            {
                if (authoring)
                    author.Append(c=='\n'? ' ':c);
                else if (quoting)
                    quote.Append(c=='\n'? ' ':c);
            }
        }
        return list;
    }
    void SaveAllPuzzles()
    {
        SavePuzzle(puzzles2, Application.persistentDataPath+"/m8n2.gd");
        SavePuzzle(puzzles3, Application.persistentDataPath+"/m8n3.gd");
        SavePuzzle(puzzles4, Application.persistentDataPath+"/m8n4.gd");
    }
    void SavePuzzle(List<Puzzle> puzzles, string path)
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.OpenWrite(path);
            bf.Serialize(fs, puzzles);
            fs.Close();
        }
        catch (Exception e)
        {
            print(e);
        }
    }
    void ShowAllProgress()
    {
        ShowProgress(puzzles2, progress2);
        ShowProgress(puzzles3, progress3);
        ShowProgress(puzzles4, progress4);
    }
    void ShowProgress(List<Puzzle> puzzles, TMPro.TextMeshProUGUI progress)
    {
        int total=0, solved=0;
        foreach (Puzzle p in puzzles)
        {
            total += 1;
            if (p.solved)
                solved += 1;
        }
        progress.text = solved+"/"+total;
        progress.color = Color.Lerp(Color.red, Color.green, (float)solved/total);
    }
    public void ResetProgress()
    {
        try
        {
            File.Delete(Application.persistentDataPath+"/m8n2.gd");
            File.Delete(Application.persistentDataPath+"/m8n3.gd");
            File.Delete(Application.persistentDataPath+"/m8n4.gd");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        catch (Exception e)
        {
            print(e);
        }
    }
    public void ShowRandomPuzzle(int numMoves)
    {
        if (numMoves == 2)
        {
            ShowRandomPuzzle(puzzles2);
        }
        else if (numMoves == 3)
        {
            ShowRandomPuzzle(puzzles3);
        }
        else if (numMoves == 4)
        {
            ShowRandomPuzzle(puzzles4);
        }
        else throw new Exception("wrong number of moves");
    }

    public void SetExcludeSolved(bool exclude)
    {
        excludeSolved = exclude;
    }
    public bool excludeSolved=true;
    Puzzle chosenPuzzle;
    void ShowRandomPuzzle(List<Puzzle> choices)
    {
        int unsolved;
        if (excludeSolved)
        {
            unsolved = 0;
            foreach (Puzzle p in choices)
            {
                if (!p.solved)
                    unsolved += 1;
            }
        }
        else
        {
            unsolved = choices.Count;
        }
        int choice = UnityEngine.Random.Range(0,unsolved);
        int counter = 0;
        chosenPuzzle = null;
        foreach (Puzzle p in choices)
        {
            if (!p.solved)
            {
                if (counter == choice)
                {
                    chosenPuzzle = p;
                    break;
                }
                counter += 1;
            }
        }
        if (chosenPuzzle != null)
        {
            board.SetFEN(chosenPuzzle.FEN);
            if (chosenPuzzle.PGN.Substring(0,4) == "1...")
                board.FlipBoard(true);
            else
                board.FlipBoard(false);
        }
        else
        {
            throw new Exception("could not choose puzzle");
        }
    }
    public void StartChosenPuzzle()
    {
        if (chosenPuzzle == null)
            throw new Exception("no puzzle chosen");

        gm.SetTitle(chosenPuzzle.name);
        gm.StartPuzzle(chosenPuzzle.FEN, chosenPuzzle.PGN,
                       ()=>{ chosenPuzzle.solved = true;
                             SaveAllPuzzles();
                             ShowAllProgress(); });
        ChooseQuote();
        Hide();
    }
    void ChooseQuote()
    {
        var chosenQuote = quotesList[UnityEngine.Random.Range(0, quotesList.Count)];
        gm.SetQuote("\"" + chosenQuote.Item2 + "\"—" + chosenQuote.Item1);
    }
    Vector2 velocity;
    Vector2 targetPos;
    void FixedUpdate()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, targetPos, ref velocity, .2f);
    }
    public void Hide()
    {
        targetPos = new Vector2(0, GetComponentInParent<CanvasScaler>().referenceResolution.y);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }
    public void Show()
    {
        targetPos = Vector2.zero;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }
}