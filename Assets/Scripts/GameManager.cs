using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] TextAsset m8n2, m8n3, m8n4, quotes;
    [Serializable] public class Puzzle
    {
        public string name;
        public string FEN;
        public string PGN;
        public bool solved;
    }
    List<Puzzle> puzzles2, puzzles3, puzzles4;
    [SerializeField] Text str2, str3, str4;
    void Awake()
    {
        puzzles2 = InitPuzzles(m8n2.text, Application.persistentDataPath+"m8n2.gd", str2);
        puzzles3 = InitPuzzles(m8n3.text, Application.persistentDataPath+"m8n3.gd", str3);
        puzzles4 = InitPuzzles(m8n4.text, Application.persistentDataPath+"m8n4.gd", str4);
        quotesList = LoadQuotes(quotes.text);
    }

    List<Puzzle> InitPuzzles(string input, string path, Text toAppend)
    {
        List<Puzzle> puzzles = null;
        try
        {
            if (!File.Exists(path)) 
            {
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
                SaveAllPuzzles();
            }
            else
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = File.Open(path, FileMode.Open);
                puzzles = (List<Puzzle>)bf.Deserialize(fs);
            }
        }
        catch (Exception e)
        {
            print(e);
        }

        int total=0, solved=0;
        foreach (Puzzle p in puzzles)
        {
            total += 1;
            if (p.solved)
                solved += 1;
        }
        toAppend.text += "\n"+solved+"/"+total;
        return puzzles;
    }
    List<Tuple<string, string>> quotesList;
    [SerializeField] Text quoteText, titleText; // TODO: scroll here if not fit
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
                authoring = true;
            }
            else if (c == ')' && !quoting)
            {
                authoring = false;
                list.Add(Tuple.Create(author.ToString(), quote.ToString()));
                // print(list[list.Count-1].Item2);
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
        SavePuzzle(puzzles2, Application.persistentDataPath+"m8n2.gd");
        SavePuzzle(puzzles3, Application.persistentDataPath+"m8n3.gd");
        SavePuzzle(puzzles4, Application.persistentDataPath+"m8n4.gd");
    }
    void SavePuzzle(List<Puzzle> puzzles, string path)
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = File.Create(path);
            bf.Serialize(fs, puzzles);
            fs.Close();
        }
        catch (Exception e)
        {
            print(e);
        }
    }
    public void ResetProgress()
    {
        try
        {
            File.Delete(Application.persistentDataPath+"m8n2.gd");
            File.Delete(Application.persistentDataPath+"m8n3.gd");
            File.Delete(Application.persistentDataPath+"m8n4.gd");
            // TODO: reload scene
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

    Queue<string> sequence;
    Puzzle chosenPuzzle;
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
        foreach (Puzzle p in choices)
        {
            if (!p.solved)
                counter += 1;
            if (counter == choice)
                break;
        }
        chosenPuzzle = choices[0];
        sequence = new Queue<string>();
        foreach (string token in Regex.Split(chosenPuzzle.PGN, " "))
        {
            if (!Regex.IsMatch(token, @"[0-9]+\.+") && token.Length > 0)
            {
                sequence.Enqueue(token.Trim(new char[]{'#','+'}));
            }
        }
        titleText.text = chosenPuzzle.name;
        StartGame(chosenPuzzle.FEN);
        if (chosenPuzzle.PGN.Substring(0,4) == "1...")
            FlipBoard(true);
        else
            FlipBoard(false);
    }
    public void StartFullGame()
    {
        // thomas = new Engine("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -");
        // thomas = new Engine("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - ");
        // thomas = new Engine("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
        // thomas = new Engine("r2qkb1r/pp2nppp/3p4/2pNN1B1/2BnP3/3P4/PPP2PPP/R2bK2R w KQkq - 1 0");
        // thomas = new Engine("knbr/p3/4/3P/RBNK w Qk -", 1, 2, false, false);
        // thomas = new Engine("kbnr/pppp/4/4/4/4/pppp/KBNR w KK -", 1, 2, false, false);
        sequence = null;
        StartGame();
    }


    [SerializeField] Button N, B, R, Q, K, x, eq;
    [SerializeField] Button O_O, O_O_O;

    void Start()
    {
        N.onClick.AddListener(()=> InputChar('N'));
        B.onClick.AddListener(()=> InputChar('B'));
        R.onClick.AddListener(()=> InputChar('R'));
        Q.onClick.AddListener(()=> InputChar('Q'));
        K.onClick.AddListener(()=> InputChar('K'));
        x.onClick.AddListener(()=> InputChar('x'));
        eq.onClick.AddListener(()=> InputChar('='));
        O_O.onClick.AddListener(()=> InputChar('>'));
        O_O_O.onClick.AddListener(()=> InputChar('<'));
    }


    private Engine thomas;

    [SerializeField] GameObject squarePrefab;
    [SerializeField] GameObject coordPrefab;

    [SerializeField] GridLayoutGroup squaresParent;
    [SerializeField] HorizontalLayoutGroup ranksParent;
    [SerializeField] HorizontalLayoutGroup filesParent;

    [SerializeField] Color lightSq, darkSq;
    List<Text> squares;
    List<Button> ranks;
    List<Button> files;

    private HashSet<string> allCandidates;
    private string candidate;

    void StartGame(string FEN="rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
    {
        if (thomas != null)
            ClearGame();

        thomas = new Engine(FEN);
        files = new List<Button>();
        for (int i=0; i<thomas.NFiles; i++)
        {
            char input = (char)('a'+i);
            var file = Instantiate(coordPrefab);
            file.GetComponent<Button>().onClick.AddListener(()=> InputChar(input));
            file.GetComponentInChildren<Text>().text = file.name = input.ToString();
            file.transform.SetParent(filesParent.transform, false);
            file.transform.SetAsLastSibling();

            files.Add(file.GetComponent<Button>());
        }
        ranks = new List<Button>();
        for (int i=0; i<thomas.NRanks; i++)
        {
            char input = (char)('1'+i);
            var rank = Instantiate(coordPrefab);
            rank.GetComponent<Button>().onClick.AddListener(()=> InputChar(input));
            rank.GetComponentInChildren<Text>().text = rank.name = input.ToString();
            rank.transform.SetParent(ranksParent.transform, false);
            rank.transform.SetAsLastSibling();

            ranks.Add(rank.GetComponent<Button>());
        }
        squares = new List<Text>();
        for (int i=0; i<thomas.NRanks; i++)
        {
            for (int j=0; j<thomas.NFiles; j++)
            {
                var square = Instantiate(squarePrefab);
                square.GetComponent<Image>().color = (i+j)%2==0? darkSq : lightSq;
                square.name = i+" "+j;
                square.transform.SetParent(squaresParent.transform, false);
                square.transform.SetAsLastSibling();

                squares.Add(square.GetComponentInChildren<Text>());
            }
        }
        squaresParent.gameObject.GetComponent<AspectRatioFitter>().aspectRatio = (float)thomas.NFiles/thomas.NRanks;
        var rect = squaresParent.GetComponent<RectTransform>().rect;
        float width = rect.height / (float)thomas.NRanks;
        squaresParent.cellSize = new Vector2(width, width);

        candidate = "";
        allCandidates = new HashSet<string>(thomas.GetLegalMovesAlgebraic());
        ShowPossibleChars();
        Display();

        var chosenQuote = quotesList[UnityEngine.Random.Range(0, quotesList.Count)];
        quoteText.text = "\"" + chosenQuote.Item2 + "\"—" + chosenQuote.Item1;
        GetComponent<Animator>().SetBool("Curtains", false);
    }
    public void EndGame()
    {
        GetComponent<Animator>().SetBool("Curtains", true);
    }
    void ClearGame()
    {
        foreach (var square in squares)
        {
            Destroy(square.transform.parent.gameObject);
        }
        squares.Clear();
        foreach (var rank in ranks)
        {
            Destroy(rank.gameObject);
        }
        ranks.Clear();
        foreach (var file in files)
        {
            Destroy(file.gameObject);
        }
        files.Clear();
    }

    [SerializeField] int ply;
    public async void Perft()
    {
        float start = Time.time;
        await Task.Run(()=> print(thomas.Perft(ply)));
        print(Time.time - start);
    }
    public async void Evaluate()
    {
        float start = Time.time;
        Tuple<string, float> best;
        best = await Task.Run(()=> thomas.EvaluateBestMove(ply));
        print(best.Item1 + " " + best.Item2);
        print(Time.time - start);
    }
    public void FlipBoard(bool flipped)
    {
        if (flipped)
        {
            squaresParent.startCorner = GridLayoutGroup.Corner.UpperRight;
        }
        else
        {
            squaresParent.startCorner = GridLayoutGroup.Corner.LowerLeft;
        }
    }



    void ShowPossibleChars()
    {
        N.interactable = false;
        B.interactable = false;
        R.interactable = false;
        Q.interactable = false;
        K.interactable = false;
        eq.interactable = false;
        x.interactable = false;
        O_O.interactable = false;
        O_O_O.interactable = false;
        foreach (Button b in files)
        {
            b.interactable = false;
        }
        foreach (Button b in ranks)
        {
            b.interactable = false;
        }

        int idx = candidate.Length;
        foreach (string move in allCandidates)
        {
            if (move.Length <= idx || move.Substring(0,idx) != candidate)
                continue;

            char c = move[idx];
            // print(move + " " + c);

            if (c >= 'a' && c <= 'h')
            {
                files[c - 'a'].interactable = true;
            }
            else if (c >= '1' && c <= '8') 
            {
                ranks[c - '1'].interactable = true;
            }
            else if (c == 'N') N.interactable = true;
            else if (c == 'B') B.interactable = true;
            else if (c == 'R') R.interactable = true;
            else if (c == 'Q') Q.interactable = true;
            else if (c == 'K') K.interactable = true;
            else if (c == 'x') x.interactable = true;
            else if (c == '=') eq.interactable = true;
            else if (c == '>') O_O.interactable = true;
            else if (c == '<') O_O_O.interactable = true;
        }
    }
    void InputChar(char input)
    {
        if (thomas == null)
            return;

        candidate += input;
        if (allCandidates.Contains(candidate))
        {
            if (sequence != null && sequence.Count > 0) // if puzzle
            {
                if (candidate == sequence.Peek())
                {
                    // thomas.PlayMoveAlgebraic(sequence.Dequeue());
                    // Display(); // don't call PlayMove as we don't want to change keyboard
                    PlayMove(sequence.Dequeue());
                    if (sequence.Count > 0)
                    {
                        StartCoroutine(WaitThenPlayMove(sequence.Dequeue(), 1));
                        StartCoroutine(DisplayTitleForTime("Correct!", 1));
                    }
                    else
                    {
                        titleText.text = "Well done!";
                        chosenPuzzle.solved = true;
                        SaveAllPuzzles();
                    }
                }
                else
                {
                    StartCoroutine(DisplayTitleForTime("WRONG", 1));
                    candidate = "";
                }
            }
            else // not puzzle
            {
                PlayMove(candidate);
            }
        }
        ShowPossibleChars();
    }
    void PlayMove(string move)
    {
        thomas.PlayMoveAlgebraic(candidate);
        candidate = "";
        allCandidates = new HashSet<string>(thomas.GetLegalMovesAlgebraic());
        moves.Push(candidate);
        undos.Clear();
        Display();
    }
    IEnumerator WaitThenPlayMove(string move, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        PlayMove(move);
    }
    IEnumerator DisplayTitleForTime(string message, float seconds)
    {
        string before = titleText.text;
        titleText.text = message;
        yield return new WaitForSeconds(seconds);
        titleText.text = before;
    }
    void UndoMove()
    {
        if (moves.Count > 0)
        {
            thomas.UndoLastMove();
            candidate = "";
            allCandidates = new HashSet<string>(thomas.GetLegalMovesAlgebraic());
            undos.Push(moves.Pop());
            Display();
            ShowPossibleChars();
        }
    }
    void RedoMove()
    {
        if (undos.Count > 0)
        {
            string redo = undos.Pop();
            thomas.PlayMoveAlgebraic(redo);
            allCandidates = new HashSet<string>(thomas.GetLegalMovesAlgebraic());
            moves.Push(redo);
            Display();
            ShowPossibleChars();
        }
    }

    Stack<string> moves = new Stack<string>();
    Stack<string> undos = new Stack<string>();
    string MovesToString()
    {
        var sb = new StringBuilder();
        int i=0;
        foreach (string move in moves)
        {
            if (i%2 == 1)
            {
                sb.Append(' ').Append(move);
            }
            else
            {
                if (i != 0) sb.Append(';');
                sb.Append(move);
            }
        }
        return sb.ToString();
    }
    void UndoChar()
    {
        if (candidate.Length == 0)
            return;
        
        candidate = candidate.Substring(0, candidate.Length-1);
        ShowPossibleChars();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) UndoMove();
        else if (Input.GetKeyDown(KeyCode.RightArrow)) RedoMove();
        else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.N) && N.interactable) InputChar('N');
            else if (Input.GetKeyDown(KeyCode.B) && B.interactable) InputChar('B');
            else if (Input.GetKeyDown(KeyCode.R) && R.interactable) InputChar('R');
            else if (Input.GetKeyDown(KeyCode.Q) && Q.interactable) InputChar('Q');
            else if (Input.GetKeyDown(KeyCode.K) && K.interactable) InputChar('K');
            else if (Input.GetKeyDown(KeyCode.Comma) && O_O_O.interactable) InputChar('<');
            else if (Input.GetKeyDown(KeyCode.Period) && O_O.interactable) InputChar('>');
        }
        else if (Input.GetKeyDown(KeyCode.Backspace)) UndoChar();
        else if (Input.GetKeyDown(KeyCode.X) && x.interactable) InputChar('x');
        else if (Input.GetKeyDown(KeyCode.Equals) && eq.interactable) InputChar('=');

        // else if (Input.GetKeyDown(KeyCode.A) && files[0].interactable) InputChar('a');
        // else if (Input.GetKeyDown(KeyCode.B) && files[1].interactable) InputChar('b');
        // else if (Input.GetKeyDown(KeyCode.C) && files[2].interactable) InputChar('c');
        // else if (Input.GetKeyDown(KeyCode.D) && files[3].interactable) InputChar('d');
        // else if (Input.GetKeyDown(KeyCode.E) && files[4].interactable) InputChar('e');
        // else if (Input.GetKeyDown(KeyCode.F) && files[5].interactable) InputChar('f');
        // else if (Input.GetKeyDown(KeyCode.G) && files[6].interactable) InputChar('g');
        // else if (Input.GetKeyDown(KeyCode.H) && files[7].interactable) InputChar('h');

        // else if (Input.GetKeyDown(KeyCode.Alpha1) && ranks[0].interactable) InputChar('1');
        // else if (Input.GetKeyDown(KeyCode.Alpha2) && ranks[1].interactable) InputChar('2');
        // else if (Input.GetKeyDown(KeyCode.Alpha3) && ranks[2].interactable) InputChar('3');
        // else if (Input.GetKeyDown(KeyCode.Alpha4) && ranks[3].interactable) InputChar('4');
        // else if (Input.GetKeyDown(KeyCode.Alpha5) && ranks[4].interactable) InputChar('5');
        // else if (Input.GetKeyDown(KeyCode.Alpha6) && ranks[5].interactable) InputChar('6');
        // else if (Input.GetKeyDown(KeyCode.Alpha7) && ranks[6].interactable) InputChar('7');
        // else if (Input.GetKeyDown(KeyCode.Alpha8) && ranks[7].interactable) InputChar('8');
    }

    void ClearBoard()
    {
        foreach (Text square in squares)
        {
            square.text = "";
        }
    }
    void DisplayPiece(int position, string letter, Color colour)
    {
        squares[position].color = colour;
        squares[position].text = letter;
    }
    void Display()
    {
        // TODO: animations
        ClearBoard();
        for (int i=0; i<squares.Count; i++)
        {
            string piece = thomas.PieceOnSquare(i, true);
            if (piece != null)
                DisplayPiece(i, piece, Color.white);
            else
            {
                piece = thomas.PieceOnSquare(i, false);
                if (piece != null)
                    DisplayPiece(i, piece, Color.black);
            }
        }
    }
}
