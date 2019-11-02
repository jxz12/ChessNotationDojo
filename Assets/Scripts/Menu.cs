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

    public void StartFullGame()
    {
        // "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -"
        // "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - "
        // "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1"
        // "r2qkb1r/pp2nppp/3p4/2pNN1B1/2BnP3/3P4/PPP2PPP/R2bK2R w KQkq - 1 0"
        gm.StartFullGame();
    }
    public void StartPeasantsRevolt()
    {
        // "1nn1knn1/4p3/8/8/8/8/PPPPPPPP/4K3 w - -"
    }
    public void StartHordeChess()
    {
        // "ppp2ppp/pppppppp/pppppppp/pppppppp/3pp3/8/PPPPPPPP/RNBQKBNR w KQ -"
    }
    public void StartDoubleChess()
    {
        // fuck this and fuck castling
        // "rnbqkbnrrnbqkbnr/pppppppppppppppp/88/88/88/88/88/88/88/88/PPPPPPPPPPPPPPPP/RNBQKBNRRNBQKBNR w KQkq - 0 1"
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
        sb[XthEmptySquare960(0, sb)] = 'r';
        sb[XthEmptySquare960(0, sb)] = 'k';
        sb[XthEmptySquare960(0, sb)] = 'r';

        sb.Append("/pppppppp/8/8/8/8/PPPPPPPP/");
        sb.Append(sb.ToString(0,8).ToUpper());
        print(sb.ToString());
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
        // "knbr/p3/4/3P/RBNK w Qk -"
    }
    public void StartDemiChess()
    {
        // "kbnr/pppp/4/4/4/4/pppp/KBNR w KK -"
    }
    public void StartBabyChess()
    {
        // "kqbnr/ppppp/5/PPPPP/RNBQK w 
    }

    [SerializeField] TextAsset m8n2, m8n3, m8n4, quotes;
    [SerializeField] Font chosenFont; // TODO: choices and textmeshpro
    [Serializable] public class Puzzle
    {
        public string name;
        public string FEN;
        public string PGN;
        public bool solved;
    }
    List<Puzzle> puzzles2, puzzles3, puzzles4;
    void Awake()
    {
        puzzles2 = InitPuzzles(m8n2.text, Application.persistentDataPath+"/m8n2.gd");
        puzzles3 = InitPuzzles(m8n3.text, Application.persistentDataPath+"/m8n3.gd");
        puzzles4 = InitPuzzles(m8n4.text, Application.persistentDataPath+"/m8n4.gd");
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
    [SerializeField] Text progress2, progress3, progress4;
    void ShowAllProgress()
    {
        ShowProgress(puzzles2, progress2);
        ShowProgress(puzzles3, progress3);
        ShowProgress(puzzles4, progress4);
    }
    void ShowProgress(List<Puzzle> puzzles, Text progress)
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
    public void RandomPuzzle2()
    {
        StartPuzzle(puzzles2);
    }
    public void RandomPuzzle3()
    {
        StartPuzzle(puzzles3);
    }
    public void RandomPuzzle4()
    {
        StartPuzzle(puzzles4);
    }

    void StartPuzzle(List<Puzzle> choices)
    {
        int unsolved=0;
        foreach (Puzzle p in choices)
        {
            if (!p.solved)
                unsolved += 1;
        }
        int choice = UnityEngine.Random.Range(0,unsolved);
        int counter = 0;
        Puzzle chosenPuzzle = null;
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
        if (chosenPuzzle == null)
            throw new Exception("could not choose puzzle");

        gm.SetTitle(chosenPuzzle.name);
        gm.StartPuzzle(chosenPuzzle.FEN, chosenPuzzle.PGN,
                       ()=>{ chosenPuzzle.solved = true; SaveAllPuzzles(); ShowAllProgress(); });
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