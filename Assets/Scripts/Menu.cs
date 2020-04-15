using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

public class Menu : MonoBehaviour
{
    [SerializeField] GameManager gm;
    [SerializeField] TextAsset input2, input3, input4, quotes;
    [SerializeField] Button progress2, progress3, progress4;
    [SerializeField] BoardAscii boardPuzzle;

    [SerializeField] BoardAscii boardClassical, board960, boardHorde, boardPeasants,
                                boardMicro, boardDemi, boardBaby, boardDouble;

    List<Puzzle> puzzles2, puzzles3, puzzles4;
    void Awake()
    {
        puzzles2 = InitPuzzles(input2.text, Application.persistentDataPath+"/m8n2.gd");
        puzzles3 = InitPuzzles(input3.text, Application.persistentDataPath+"/m8n3.gd");
        puzzles4 = InitPuzzles(input4.text, Application.persistentDataPath+"/m8n4.gd");

        quotesList = LoadQuotes(quotes.text);
    }
    void Start()
    {
        ShowAllProgress();

        boardClassical.FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w AHah - 0 1";
        StartCoroutine(Refresh960(.2f));
        boardHorde.FEN = "ppp2ppp/pppppppp/pppppppp/pppppppp/3pp3/8/PPPPPPPP/RNBQKBNR w AH - 0 1";
        boardPeasants.FEN = "1nn1knn1/4p3/8/8/8/8/PPPPPPPP/4K3 w - - 0 1";
        boardMicro.FEN = "knbr/p3/4/3P/RBNK w Ad - 0 1";
        boardDemi.FEN = "kbnr/pppp/4/4/4/4/PPPP/KBNR w Dd - 0 1";
        boardBaby.FEN = "kqbnr/ppppp/5/PPPPP/RNBQK w - - 0 1";
        boardDouble.FEN = "rnbqkbnrrnbqkbnr/pppppppppppppppp/88/88/88/88/88/88/88/88/PPPPPPPPPPPPPPPP/RNBQKBNRRNBQKBNR w AHIPahip - 0 1";
    }
    IEnumerator Refresh960(float delay)
    {
        while (true)
        {
            if (menuCanvas.enabled) {
                board960.FEN = Chess960();
            }
            yield return new WaitForSeconds(delay);
        }
    }

    Vector2 pivocity;
    Vector2 targetPivot = new Vector2(.5f, 1);
    void FixedUpdate()
    {
        var rt = GetComponent<RectTransform>();
        float delta = (rt.pivot - targetPivot).sqrMagnitude;
        if (delta > .000001f) {
            rt.pivot = Vector2.SmoothDamp(rt.pivot, targetPivot, ref pivocity, .2f);
        } else if (delta != 0) {
            rt.pivot = targetPivot;
            if (targetPivot.y == 0) {
                menuCanvas.enabled = false;
            }
        }
    }
    public void Hide()
    {
        targetPivot = new Vector2(.5f, 0);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }
    public void Show()
    {
        menuCanvas.enabled = true;
        targetPivot = new Vector2(.5f, 1);
        GetComponent<CanvasGroup>().blocksRaycasts = true;
        boardPuzzle.Clear();
        startPuzzleButton.gameObject.SetActive(false);
        puzzleTitle.text = "";
    }

    ////////////// 
    // VARIANTS //
    //////////////
    public void StartClassical() { StartFullGame(boardClassical.FEN, "Classical", true, false); }
    public void StartChess960() { StartFullGame(board960.FEN, "Chess960", true, true); }
    public void StartHordeChess() { StartFullGame(boardHorde.FEN, "Horde Chess", false, false); }
    public void StartPeasantsRevolt() { StartFullGame(boardClassical.FEN, "Peasant's Revolt", true, false); }
    public void StartMicroChess() { StartFullGame(boardMicro.FEN, "Micro Chess", false, false); }
    public void StartDemiChess() { StartFullGame(boardDemi.FEN, "Demi-Chess", true, false); }
    public void StartBabyChess() { StartFullGame(boardBaby.FEN, "Baby Chess", false, false); }
    public void StartDoubleChess() { StartFullGame(boardDouble.FEN, "Double Chess", false, false); }
    public void StartFullGame(string FEN, string title, bool puush, bool castle960)
    {
        gm.StartFullGame(FEN, puush, castle960, computerPlaysWhite);
        gm.SetTitle(title);
        ChooseQuote();
        Hide();
    }

    string Chess960()
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
          .Append(" - 0 1");

        sb.ToString();
        return sb.ToString();
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
    bool? computerPlaysWhite = null;
    public void SetPlayerVsPlayer(Toggle tog) { if (tog.isOn) computerPlaysWhite = null; }
    public void SetWhiteVsComputer(Toggle tog) { if (tog.isOn) computerPlaysWhite = false; }
    public void SetBlackVsComputer(Toggle tog) { if (tog.isOn) computerPlaysWhite = true; }

    ///////////// 
    // Puzzles //
    /////////////
    [Serializable]
    public class Puzzle
    {
        public string name;
        public string FEN;
        public string PGN;
        public bool solved;
    }

    List<Puzzle> InitPuzzles(string input, string path)
    {
        List<Puzzle> puzzles = null;
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = File.OpenRead(path))
            {
                puzzles = (List<Puzzle>)bf.Deserialize(fs);
                fs.Close();
            }
        }
        catch (Exception e)
        {
            print("Save file not present, so reading from TextAsset\nerror: " + e.Message);
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
            SavePuzzles(puzzles, path);
        }
        return puzzles;
    }
    void SaveAllPuzzles()
    {
        SavePuzzles(puzzles2, Application.persistentDataPath+"/m8n2.gd");
        SavePuzzles(puzzles3, Application.persistentDataPath+"/m8n3.gd");
        SavePuzzles(puzzles4, Application.persistentDataPath+"/m8n4.gd");
    }
    void SavePuzzles(List<Puzzle> puzzles, string path)
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = File.OpenWrite(path))
            {
                bf.Serialize(fs, puzzles);
                fs.Close();
            }
        }
        catch (Exception e)
        {
            print(e);
        }
    }
    void ShowAllProgress()
    {
        void ShowProgress(List<Puzzle> puzzles, Button progress)
        {
            int total=0, solved=0;
            foreach (Puzzle p in puzzles)
            {
                total += 1;
                if (p.solved) {
                    solved += 1;
                }
            }
            var tmp = progress.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmp.text = solved+"/"+total;
            tmp.color = Color.Lerp(Color.red, Color.green, (float)solved/total);

            progress.interactable = (solved != total || !excludeSolved);
        }
        ShowProgress(puzzles2, progress2);
        ShowProgress(puzzles3, progress3);
        ShowProgress(puzzles4, progress4);
        boardPuzzle.Clear();
        puzzleTitle.text = "";
        startPuzzleButton.gameObject.SetActive(false);
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
            print($"no save files present to delete. error: {e.Message}");
        }
    }

    public bool excludeSolved=true;
    public void SetExcludeSolved(Toggle tog)
    {
        excludeSolved = tog.isOn;
        ShowAllProgress();
    }
    [SerializeField] Button startPuzzleButton;
    [SerializeField] TMPro.TextMeshProUGUI puzzleTitle;
    public void ShowRandomPuzzle(int numMoves)
    {
        void ShowPuzzle(List<Puzzle> choices)
        {
            int unsolved;
            if (excludeSolved)
            {
                unsolved = 0;
                foreach (Puzzle p in choices)
                {
                    if (!p.solved) {
                        unsolved += 1;
                    }
                }
            }
            else
            {
                unsolved = choices.Count;
            }

            int choice = UnityEngine.Random.Range(0, unsolved);
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
                boardPuzzle.FEN = chosenPuzzle.FEN;
                if (chosenPuzzle.PGN.Substring(0,4) == "1...") {
                    boardPuzzle.FlipBoard(true);
                } else {
                    boardPuzzle.FlipBoard(false);
                }
                puzzleTitle.text = chosenPuzzle.name;
            }
            else
            {
                throw new Exception("could not choose puzzle");
            }
        }
        if (numMoves == 2) {
            ShowPuzzle(puzzles2);
        } else if (numMoves == 3) {
            ShowPuzzle(puzzles3);
        } else if (numMoves == 4) {
            ShowPuzzle(puzzles4);
        } else { throw new Exception("wrong number of moves"); }
        startPuzzleButton.gameObject.SetActive(true);
    }
    Puzzle chosenPuzzle;
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

    //////////////
    // navigation
    [SerializeField] Canvas menuCanvas, splashCanvas, puzzlesCanvas, variantsCanvas;
    public void GotoSplash()
    {
        puzzlesCanvas.enabled = variantsCanvas.enabled = false;
        splashCanvas.enabled = true;
    }
    public void GotoPuzzles()
    {
        ShowAllProgress();
        splashCanvas.enabled = false;
        puzzlesCanvas.enabled = true;
    }
    public void GotoVariants()
    {
        splashCanvas.enabled = false;
        variantsCanvas.enabled = true;
    }

    ////////////
    // QUOTES //
    ////////////
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
    void ChooseQuote()
    {
        var chosenQuote = quotesList[UnityEngine.Random.Range(0, quotesList.Count)];
        gm.SetQuote("\"" + chosenQuote.Item2 + "''—" + chosenQuote.Item1);
    }
}