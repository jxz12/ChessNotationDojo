using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

	[SerializeField] List<Text> squares;
	                             // 8 7 6 5 4 3 2 1
	private UInt64 whitePawns =   0x000000000000FF00;
	private UInt64 whiteKnights = 0x0000000000000042;
	private UInt64 whiteBishops = 0x0000000000000024;
	private UInt64 whiteRooks =   0x0000000000000081;
	private UInt64 whiteQueen =   0x0000000000000008;
	private UInt64 whiteKing =    0x0000000000000010;
	                             // 8 7 6 5 4 3 2 1
	private UInt64 blackPawns =   0x00FF000000000000;
	private UInt64 blackKnights = 0x4200000000000000;
	private UInt64 blackBishops = 0x2400000000000000;
	private UInt64 blackRooks =   0x8100000000000000;
	private UInt64 blackQueen =   0x0800000000000000;
	private UInt64 blackKing =    0x1000000000000000;

	// TODO: game rules
	//       parse input
	//       record games to text
	//       make AI
	//       fit to API?
	
	void Start()
	{
		Display();
	}
	void Display()
	{
		UInt64 whitePieces = whitePawns | whiteKnights | whiteBishops | whiteRooks | whiteQueen | whiteKing;
		UInt64 blackPieces = blackPawns | blackKnights | blackBishops | blackRooks | blackQueen | blackKing;
		for (Int16 i=0; i<64; i++)
		{
			UInt64 pos = (UInt64)1 << i;
			if ((whitePieces & pos) != 0)
			{
				squares[i].color = Color.white;
			}
			if ((blackPieces & pos) != 0)
			{
				squares[i].color = Color.black;
			}
			if (((whitePawns | blackPawns) & pos) != 0)
			{
				squares[i].text = "p";
			}
			if (((whiteKnights | blackKnights) & pos) != 0)
			{
				squares[i].text = "N";
			}
			if (((whiteBishops | blackBishops) & pos) != 0)
			{
				squares[i].text = "B";
			}
			if (((whiteRooks | blackRooks) & pos) != 0)
			{
				squares[i].text = "R";
			}
			if (((whiteQueen | blackQueen) & pos) != 0)
			{
				squares[i].text = "Q";
			}
			if (((whiteKing | blackKing) & pos) != 0)
			{
				squares[i].text = "K";
			}
		}
	}
	
	bool whiteMove = true;
	// string move = "";
	public void Input(string s)
	{
		if (s.Length != 1)
			throw new Exception("malformed input string");

		char c = s[0];
		print(c - 'a');
	}
	// Update is called once per frame
	void Update () {
		
	}
}
