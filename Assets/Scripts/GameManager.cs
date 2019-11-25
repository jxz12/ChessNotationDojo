using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] Button N, B, R, Q, K, x, eq, bksp;
    [SerializeField] HorizontalLayoutGroup ranksParent, filesParent;
    [SerializeField] Button undoButton, redoButton, quitButton, evalButton;
    [SerializeField] TMPro.TextMeshProUGUI titleText;
    [SerializeField] MessageScroller quoteScroller;

    void Start()
    {
        N.onClick.AddListener(()=> InputChar('N'));
        B.onClick.AddListener(()=> InputChar('B'));
        R.onClick.AddListener(()=> InputChar('R'));
        Q.onClick.AddListener(()=> InputChar('Q'));
        K.onClick.AddListener(()=> InputChar('K'));
        x.onClick.AddListener(()=> InputChar('x'));
        eq.onClick.AddListener(()=> InputChar('='));
        bksp.onClick.AddListener(()=> InputChar('\b'));

        undoButton.onClick.AddListener(()=> UndoMove());
        redoButton.onClick.AddListener(()=> RedoMove());
    }

    private Engine thomas;

    // private BoardAscii board;
    // [SerializeField] BoardAscii boardPrefab;
    [SerializeField] BoardAscii board;
    // [SerializeField] Board3D board;
    
    [SerializeField] GameObject inputPrefab;
    List<Button> ranks;
    List<Button> files;

    private HashSet<string> allCandidates;
    private string candidate;
    private Queue<string> sequence;
    private Action OnSolved;

    public void StartPuzzle(string FEN, string PGN, Action OnSolved)
    {
        sequence = new Queue<string>();
        foreach (string token in Regex.Split(PGN, " "))
        {
            if (!Regex.IsMatch(token, @"[0-9]+\.+") && token.Length > 0)
            {
                sequence.Enqueue(token.Trim(new char[]{'#','+'}));
            }
        }
        this.OnSolved = OnSolved;
        if (PGN.Substring(0,4) == "1...")
            board.FlipBoard(true);
        else
            board.FlipBoard(false);
        
        quitButton.GetComponent<Image>().color = Color.red;
        StartGame(FEN, 2, false);
    }
    public void StartFullGame(string FEN, int puush, bool castle960)
    {
        sequence = null;
        StartGame(FEN, puush, castle960);
    }

    void StartGame(string FEN, int puush, bool castle960)
    {
        if (thomas != null)
            ClearGame();

        thomas = new Engine(FEN, puush, castle960);
        files = new List<Button>();
        for (int i=0; i<thomas.NFiles; i++)
        {
            char input = (char)('a'+i);
            var file = Instantiate(inputPrefab);
            file.GetComponent<Button>().onClick.AddListener(()=> InputChar(input));
            file.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = file.name = input.ToString();
            file.transform.SetParent(filesParent.transform, false);
            file.transform.SetAsLastSibling();

            files.Add(file.GetComponent<Button>());
        }
        ranks = new List<Button>();
        for (int i=0; i<thomas.NRanks; i++)
        {
            char input = (char)('1'+i);
            var rank = Instantiate(inputPrefab);
            rank.GetComponent<Button>().onClick.AddListener(()=> InputChar(input));
            rank.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = rank.name = (i+1).ToString();
            rank.transform.SetParent(ranksParent.transform, false);
            rank.transform.SetAsLastSibling();

            ranks.Add(rank.GetComponent<Button>());
        }
        board.FEN = thomas.ToFEN();

        candidate = "";
        allCandidates = new HashSet<string>(thomas.GetPGNs());
        ShowPossibleChars();

        Perft(3);
    }
    public void SetQuote(string quote)
    {
        quoteScroller.SetText(quote);
    }
    public void SetTitle(string title)
    {
        titleText.text = title;
    }
    void ClearGame()
    {
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

    public async void Perft(int ply)
    {
        float start = Time.time;
        int nodes = await Task.Run(()=> thomas.Perft(ply));
        print(nodes);
        print(Time.time - start);
    }
    public async void Evaluate(int ply)
    {
        float start = Time.time;

        evalButton.interactable = false;
        var best = await Task.Run(()=> thomas.EvaluateBestMove(ply));
        evalButton.interactable = true;

        print(best.Item1 + " " + best.Item2);
        print(Time.time - start);
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
        foreach (Button b in files)
        {
            b.interactable = false;
        }
        foreach (Button b in ranks)
        {
            b.interactable = false;
        }
        if (candidate == null)
        {
            return;
        }

        int idx = candidate.Length;
        bksp.interactable = candidate.Length > 0;
        foreach (string move in allCandidates)
        {
            if (move.Length <= idx || move.Substring(0,idx) != candidate)
                continue;

            char c = move[idx];
            // print(move + " " + c);

            if (c >= 'a' && c <= 'z')
            {
                files[c - 'a'].interactable = true;
            }
            else if (c >= '1' && c <= '@') // @ is '0'+16
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
            else throw new Exception("Unexpected input " + c);
        }
    }
    void InputChar(char input)
    {
        if (thomas == null)
            return;
        if (input == '\b')
        {
            UndoChar();
            return;
        }

        candidate += input;
        if (allCandidates.Contains(candidate))
        {
            if (sequence != null && sequence.Count > 0) // if puzzle
            {
                if (candidate == sequence.Peek())
                {
                    PlayMove(sequence.Dequeue(), false);
                    if (sequence.Count > 0)
                    {
                        StartCoroutine(WaitThenPlayMove(sequence.Dequeue(), 1));
                        StartCoroutine(DisplayTitleForTime("Correct!", 1, Color.green, TMPro.FontStyles.Bold));
                    }
                    else
                    {
                        titleText.text = "Well done!";
                        quitButton.GetComponent<Image>().color = Color.green;
                        // evalButton.interactable = false;
                        OnSolved.Invoke();
                    }
                }
                else
                {
                    // TODO: show escaping variation
                    StartCoroutine(DisplayTitleForTime("WRONG", 1, Color.red, TMPro.FontStyles.Bold));
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

    Stack<string> undos = new Stack<string>();
    Stack<string> redos = new Stack<string>();
    void PlayMove(string move, bool updateKeyboard=true)
    {
        thomas.PlayPGN(move);
        undos.Push(move);
        redos.Clear();
        // board.PlayMoveUCI(thomas.GetLastUCI());
        board.FEN = thomas.ToFEN();
        if (updateKeyboard)
        {
            allCandidates = new HashSet<string>(thomas.GetPGNs());
            candidate = "";
            ShowPossibleChars();
        }
        undoButton.interactable = true;
        redoButton.interactable = false;
    }
    IEnumerator WaitThenPlayMove(string move, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        PlayMove(move);
    }
    IEnumerator DisplayTitleForTime(string message, float seconds, Color col, TMPro.FontStyles style)
    {
        string before = titleText.text;
        Color colBefore = titleText.color;
        TMPro.FontStyles styBefore = titleText.fontStyle;

        titleText.text = message;
        titleText.color = col;
        titleText.fontStyle = style;
        yield return new WaitForSeconds(seconds);
        titleText.text = before;
        titleText.color = colBefore;
        titleText.fontStyle = styBefore;
    }
    void UndoMove()
    {
        if (undos.Count > 0)
        {
            thomas.UndoLastMove();
            allCandidates = new HashSet<string>(thomas.GetPGNs());
            candidate = sequence==null? "" : null;

            redos.Push(undos.Pop());
            // board.PlayMoveUCI(thomas.GetLastUCI());
            board.FEN = thomas.ToFEN();
            ShowPossibleChars();
        }
        redoButton.interactable = true;
        undoButton.interactable = undos.Count > 0;
    }
    void RedoMove()
    {
        if (redos.Count > 0)
        {
            string redo = redos.Pop();
            thomas.PlayPGN(redo);
            allCandidates = new HashSet<string>(thomas.GetPGNs());
            candidate = sequence==null || redos.Count==0? "" : null;

            undos.Push(redo);
            // board.PlayMoveUCI(thomas.GetLastUCI());
            board.FEN = thomas.ToFEN();
            ShowPossibleChars();
        }
        redoButton.interactable = redos.Count > 0;
        undoButton.interactable = true;
    }
    void UndoChar()
    {
        if (candidate.Length == 0)
            return;
        
        candidate = candidate.Substring(0, candidate.Length-1);
        ShowPossibleChars();
    }
    // void Update()
    // {
        // if (Input.GetKeyDown(KeyCode.LeftArrow)) UndoMove();
        // else if (Input.GetKeyDown(KeyCode.RightArrow)) RedoMove();
        // else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        // {
        //     if (Input.GetKeyDown(KeyCode.N) && N.interactable) InputChar('N');
        //     else if (Input.GetKeyDown(KeyCode.B) && B.interactable) InputChar('B');
        //     else if (Input.GetKeyDown(KeyCode.R) && R.interactable) InputChar('R');
        //     else if (Input.GetKeyDown(KeyCode.Q) && Q.interactable) InputChar('Q');
        //     else if (Input.GetKeyDown(KeyCode.K) && K.interactable) InputChar('K');
        //     else if (Input.GetKeyDown(KeyCode.Comma) && O_O_O.interactable) InputChar('<');
        //     else if (Input.GetKeyDown(KeyCode.Period) && O_O.interactable) InputChar('>');
        // }
        // else if (Input.GetKeyDown(KeyCode.Backspace)) UndoChar();
        // else if (Input.GetKeyDown(KeyCode.X) && x.interactable) InputChar('x');
        // else if (Input.GetKeyDown(KeyCode.Equals) && eq.interactable) InputChar('=');

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
    // }
}
