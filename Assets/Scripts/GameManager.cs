using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // TODO: options to make invisible/pulse
    //       flip board
    //       color schemes

    [SerializeField] GridLayoutGroup board;
    [SerializeField] List<Text> squares; // TODO: change to a class Square and make it any size
    [SerializeField] Button N, B, R, Q, K, x, eq;
    [SerializeField] Button O_O, O_O_O;
    [SerializeField] List<Button> files; // TODO: change this to be variable based on board size
    [SerializeField] List<Button> ranks;

	List<string> moves = new List<string>();
	[SerializeField] string moveSheet;

    private Engine thomas;
    void Awake()
    {
        thomas = new Engine(ranks.Count, files.Count);
    }

    private HashSet<string> allCandidates;
    private string candidate;
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
        for (int i=0; i<files.Count; i++)
        {
            char input = (char)('a'+i);
            files[i].onClick.AddListener(()=> InputChar(input));
        }
        for (int i=0; i<ranks.Count; i++)
        {
            char input = (char)('1'+i);
            ranks[i].onClick.AddListener(()=> InputChar(input));
        }
        // var rect = board.GetComponent<RectTransform>().rect;
        // board.cellSize = new Vector2(rect.width, rect.height) / 8;

        candidate = "";
        allCandidates = new HashSet<string>(thomas.GetLegalMovesAlgebraic());
        ShowPossibleChars();
        Display();
    }
    void Update()
    {
        ReadCharFromKeyboard();
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
        candidate += input;
        if (allCandidates.Contains(candidate))
        {
            bool check = thomas.PlayMoveAlgebraic(candidate);
            allCandidates = new HashSet<string>(thomas.GetLegalMovesAlgebraic());
			WriteMoveSheet(check);
            candidate = "";
            Display();
        }
        ShowPossibleChars();
    }
	void WriteMoveSheet(bool check)
	{
        if (check && allCandidates.Count == 0) // checkmate
        {
            candidate += '#';
            if (moves.Count%2 == 0)
            {
                candidate += " 1-0";
            }
            else
            {
                candidate += " 0-1";
            }
        }
        else if (check && allCandidates.Count > 0) // check
        {
            candidate += '+';
        }
        else if (!check && allCandidates.Count == 0) // draw
        {
            candidate += " ½-½";
        }
        // print(candidate);
        moves.Add(candidate);

		var sb = new StringBuilder(moves[0]);
		for (int i=1; i<moves.Count; i++)
		{
			if (i%2 == 1)
			{
				sb.Append(' ').Append(moves[i]);
			}
			else
			{
				sb.Append(';').Append(moves[i]);
			}
		}
		moveSheet = sb.ToString();
	}
    void UndoChar()
    {
        if (candidate.Length == 0)
            return;
        
        candidate = candidate.Substring(0, candidate.Length-1);
        ShowPossibleChars();
    }
    void ReadCharFromKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Backspace)) UndoChar();

        else if (Input.GetKeyDown(KeyCode.A) && files[0].interactable) InputChar('a');
        else if (Input.GetKeyDown(KeyCode.B) && files[1].interactable) InputChar('b');
        else if (Input.GetKeyDown(KeyCode.C) && files[2].interactable) InputChar('c');
        else if (Input.GetKeyDown(KeyCode.D) && files[3].interactable) InputChar('d');
        else if (Input.GetKeyDown(KeyCode.E) && files[4].interactable) InputChar('e');
        else if (Input.GetKeyDown(KeyCode.F) && files[5].interactable) InputChar('f');
        else if (Input.GetKeyDown(KeyCode.G) && files[6].interactable) InputChar('g');
        else if (Input.GetKeyDown(KeyCode.H) && files[7].interactable) InputChar('h');

        else if (Input.GetKeyDown(KeyCode.Alpha1) && ranks[0].interactable) InputChar('1');
        else if (Input.GetKeyDown(KeyCode.Alpha2) && ranks[1].interactable) InputChar('2');
        else if (Input.GetKeyDown(KeyCode.Alpha3) && ranks[2].interactable) InputChar('3');
        else if (Input.GetKeyDown(KeyCode.Alpha4) && ranks[3].interactable) InputChar('4');
        else if (Input.GetKeyDown(KeyCode.Alpha5) && ranks[4].interactable) InputChar('5');
        else if (Input.GetKeyDown(KeyCode.Alpha6) && ranks[5].interactable) InputChar('6');
        else if (Input.GetKeyDown(KeyCode.Alpha7) && ranks[6].interactable) InputChar('7');
        else if (Input.GetKeyDown(KeyCode.Alpha8) && ranks[7].interactable) InputChar('8');

        else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.N) && N.interactable) InputChar('N');
            else if (Input.GetKeyDown(KeyCode.B) && B.interactable) InputChar('B');
            else if (Input.GetKeyDown(KeyCode.R) && R.interactable) InputChar('R');
            else if (Input.GetKeyDown(KeyCode.Q) && Q.interactable) InputChar('Q');
            else if (Input.GetKeyDown(KeyCode.K) && K.interactable) InputChar('K');
        }
        else if (Input.GetKeyDown(KeyCode.X) && x.interactable) InputChar('x');
        else if (Input.GetKeyDown(KeyCode.Equals) && eq.interactable) InputChar('=');
        else if (Input.GetKeyDown(KeyCode.Home) && O_O.interactable) InputChar('>');
        else if (Input.GetKeyDown(KeyCode.End) && O_O_O.interactable) InputChar('<');
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
    void DisplayPieces(IEnumerable<int> positions, string letter, Color colour)
    {
        foreach (int i in positions)
        {
			squares[i].color = colour;
			squares[i].text = letter;
        }
    }
    void Display()
    {
        ClearBoard();
        DisplayPieces(thomas.WhitePawns,   "♟", Color.white);
        DisplayPieces(thomas.WhiteRooks,   "♜", Color.white);
        DisplayPieces(thomas.WhiteKnights, "♞", Color.white);
        DisplayPieces(thomas.WhiteBishops, "♝", Color.white);
        DisplayPieces(thomas.WhiteQueens,  "♛", Color.white);
        DisplayPiece(thomas.WhiteKing,     "♚", Color.white);

        DisplayPieces(thomas.BlackPawns,   "♟", Color.black);
        DisplayPieces(thomas.BlackRooks,   "♜", Color.black);
        DisplayPieces(thomas.BlackKnights, "♞", Color.black);
        DisplayPieces(thomas.BlackBishops, "♝", Color.black);
        DisplayPieces(thomas.BlackQueens,  "♛", Color.black);
        DisplayPiece(thomas.BlackKing,     "♚", Color.black);
    }
}
