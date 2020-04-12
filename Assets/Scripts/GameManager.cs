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
    [SerializeField] TMPro.TextMeshProUGUI titleText, movesText;
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
    [SerializeField] BoardAscii board;
    
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
        {
            board.FlipBoard(true);
            movesText.text = "1...";
        }
        else
        {
            board.FlipBoard(false);
            movesText.text = "1.";
        }
        
        quitButton.GetComponent<Image>().color = Color.red;
        StartGame(FEN, 2, false);
    }
    bool? computerPlayingWhite = null;
    public void StartFullGame(string FEN, int puush, bool castle960, bool? computerPlaysWhite=null)
    {
        sequence = null;
        movesText.text = "1.";

        quitButton.GetComponent<Image>().color = Color.red;
        StartGame(FEN, puush, castle960);

        computerPlayingWhite = computerPlaysWhite;
        board.FlipBoard(!(computerPlayingWhite??true));
        if (computerPlayingWhite ?? false) {
            PlayComputerMove();
        }
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

        // Perft(3);
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
        DisableKeyboard();

        // check for win conditions
        if (allCandidates.Count == 0) {
            quitButton.GetComponent<Image>().color = Color.green;
        } else {
            quitButton.GetComponent<Image>().color = Color.red;
        }
        if (candidate == null) {
            return; // candidate is set to null when a puzzle is over 
        }
        int idx = candidate.Length;
        bksp.interactable = candidate.Length > 0;
        foreach (string move in allCandidates)
        {
            if (move.Length <= idx || move.Substring(0,idx) != candidate)
                continue;

            char c = move[idx];
            // print(move + " " + c);

            if (c >= 'a' && c <= 'w') // collision with capture :(
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
    void DisableKeyboard()
    {
        N.interactable = false;
        B.interactable = false;
        R.interactable = false;
        Q.interactable = false;
        K.interactable = false;
        eq.interactable = false;
        x.interactable = false;
        bksp.interactable = false;
        foreach (Button b in files)
        {
            b.interactable = false;
        }
        foreach (Button b in ranks)
        {
            b.interactable = false;
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
                    StartCoroutine(DisplayTitleForTime("WRONG", 1, Color.red, TMPro.FontStyles.Bold));
                    candidate = "";
                    ShowPossibleChars();
                }
            }
            else // not puzzle
            {
                PlayMove(candidate);

                if (computerPlayingWhite != null)
                {
                    bool white = (bool)computerPlayingWhite;
                    if (white == (undos.Count%2==0)) {
                        PlayComputerMove();
                    }
                }
            }
        }
        else
        {
            ShowPossibleChars();
        }
    }

    Stack<string> undos = new Stack<string>();
    Stack<string> redos = new Stack<string>();
    void PlayMove(string move, bool updateKeyboard=true)
    {
        thomas.PlayPGN(move);
        undos.Push(move);
        redos.Clear();
        board.FEN = thomas.ToFEN();
        if (updateKeyboard)
        {
            allCandidates = new HashSet<string>(thomas.GetPGNs());
            candidate = "";
            ShowPossibleChars();
        }
        else
        {
            DisableKeyboard();
        }
        undoButton.interactable = true;
        redoButton.interactable = false;
        WriteMoveSheet();
    }
    void PlayComputerMove()
    {
        IEnumerator DelayThenPlayComputerMove()
        {
            yield return null;
            PlayMove(thomas.EvaluateBestMove(2).Item1);
        }
        StartCoroutine(DelayThenPlayComputerMove());
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
            candidate = sequence==null? "" : null; // this disables the keyboard

            redos.Push(undos.Pop());
            board.FEN = thomas.ToFEN();
            if (sequence == null)
            {
                ShowPossibleChars();
            }
            else
            {
                DisableKeyboard();
            }
        }
        redoButton.interactable = true;
        undoButton.interactable = undos.Count > 0;
        WriteMoveSheet();
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
            board.FEN = thomas.ToFEN();
            if (sequence==null || (sequence.Count>0 && redos.Count==0))
            {
                ShowPossibleChars();
            }
        }
        redoButton.interactable = redos.Count > 0;
        undoButton.interactable = true;
        WriteMoveSheet();
    }
    void UndoChar()
    {
        if (candidate.Length == 0)
            return;
        
        candidate = candidate.Substring(0, candidate.Length-1);
        ShowPossibleChars();
    }
    void WriteMoveSheet()
    {
        var moveList = new List<string>(undos);
        if (moveList.Count == 0)
        {
            if (movesText.text.Length >= 4 && movesText.text.Substring(0,4) == "1...")
                movesText.text = "1...";
            else
                movesText.text = "1.";
            return;
        }

        var sb = new StringBuilder();
        int i=moveList.Count-1;
        if (movesText.text.Length >= 4 && movesText.text.Substring(0,4) == "1...")
        {
            sb.Append("1... ").Append(moveList[i]);
            i -= 1;
        }
        else
        {
            sb.Append("1. ").Append(moveList[i]);
            i -= 1;
            if (moveList.Count >= 2)
            {
                sb.Append(" ").Append(moveList[i]);
                i -= 1;
            }
        }
        int moveNum=2;
        for (; i>=0; i--)
        {
            if (moveNum%2 == 0)
            {
                sb.Append("\n").Append(moveNum/2).Append(".");
            }
            sb.Append(" ").Append(moveList[i]);
            moveNum += 1;
        }
        movesText.text = sb.ToString();

        var movesRT = movesText.GetComponent<RectTransform>();
        var movesParentRT = movesRT.transform.parent.GetComponent<RectTransform>();
        if (movesRT.sizeDelta.y > movesParentRT.sizeDelta.y) {
            movesRT.pivot = new Vector2(.5f, 0);
            movesRT.anchoredPosition = Vector2.zero;
        } else {
            movesRT.pivot = new Vector2(.5f, .5f);
        }
    }
}
