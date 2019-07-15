using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	// TODO: options to make invisible/pulse
	//       flip board
	//       record games to text
	//       color schemes

	[SerializeField] List<Text> squares; // TODO: change to a class Square and make it any size
	[SerializeField] Button N, B, R, Q, K, x, eq;
	[SerializeField] List<Button> files;
	[SerializeField] List<Button> ranks;
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
		eq.onClick.AddListener(()=> InputChar('='));
		for (int i=0; i<files.Count; i++)
		{
			char input = (char)('a'+i);
			files[i].onClick.AddListener(()=> InputChar(input)); // lol
		}
		for (int i=0; i<ranks.Count; i++)
		{
			char input = (char)('1'+i);
			ranks[i].onClick.AddListener(()=> InputChar(input)); // lol
		}

		candidate = "";
		allCandidates = new HashSet<string>(thomas.GetMovesAlgebraic());
		ShowPossibleChars();
		Display();
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

		int idx = candidate.Length;
		foreach (string move in allCandidates)
		{
			if (move.Length <= idx || move.Substring(0,idx) != candidate)
			{
				continue;
			}
			// print(move + " " + c);
			char c = move[idx];

			if (c >= 'a' && c <= 'h')
			{
				files[c - 'a'].interactable = true;
			}
			else if (c >= '1' && c <= '8')
			{
				ranks[c - '1'].interactable = true;
			}
			else if (c == 'N')
			{
				N.interactable = true;
			}
			else if (c == 'B')
			{
				B.interactable = true;
			}
			else if (c == 'R')
			{
				R.interactable = true;
			}
			else if (c == 'Q')
			{
				Q.interactable = true;
			}
			else if (c == 'K')
			{
				K.interactable = true;
			}
			else if (c == '=')
			{
				eq.interactable = true;
			}
			else if (c == 'x')
			{
				x.interactable = true;
			}
		}
	}
	public void InputChar(char input)
	{
		candidate += input;
		if (allCandidates.Contains(candidate))
		{
			// print(candidate);
			thomas.PerformMoveAlgebraic(candidate);
			candidate = "";
			allCandidates = new HashSet<string>(thomas.GetMovesAlgebraic());
			Display();
		}
		ShowPossibleChars();
	}

	void ClearBoard()
	{
		foreach (Text square in squares)
		{
			square.text = "";
		}
	}
	void DisplayPiece(IEnumerable<int> positions, string letter, Color colour)
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
		DisplayPiece(thomas.WhitePawns,   "p", Color.white);
		DisplayPiece(thomas.WhiteRooks,   "R", Color.white);
		DisplayPiece(thomas.WhiteKnights, "N", Color.white);
		DisplayPiece(thomas.WhiteBishops, "B", Color.white);
		DisplayPiece(thomas.WhiteQueens,  "Q", Color.white);
		DisplayPiece(thomas.WhiteKings,   "K", Color.white);
		DisplayPiece(thomas.BlackPawns,   "p", Color.black);
		DisplayPiece(thomas.BlackRooks,   "R", Color.black);
		DisplayPiece(thomas.BlackKnights, "N", Color.black);
		DisplayPiece(thomas.BlackBishops, "B", Color.black);
		DisplayPiece(thomas.BlackQueens,  "Q", Color.black);
		DisplayPiece(thomas.BlackKings,   "K", Color.black);
	}
}
