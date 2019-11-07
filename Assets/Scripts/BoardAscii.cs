using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class BoardAscii : MonoBehaviour
{
    [SerializeField] GameObject squarePrefab;
    [SerializeField] Color lightSq, darkSq;

    List<Text> squares;

    void Update()
    {
        var rect = GetComponent<RectTransform>().rect;
        float width = rect.height / (float)nRanks;
        GetComponent<GridLayoutGroup>().cellSize = new Vector2(width, width);
    }

    void InitSquares()
    {
        squares = new List<Text>();
        for (int i=0; i<nRanks; i++)
        {
            for (int j=0; j<nFiles; j++)
            {
                var square = Instantiate(squarePrefab);
                square.GetComponent<Image>().color = (i+j)%2==0? darkSq : lightSq;
                square.name = i+" "+j;
                square.transform.SetParent(transform, false);
                square.transform.SetAsLastSibling();

                squares.Add(square.GetComponentInChildren<Text>());
            }
        }
        GetComponent<AspectRatioFitter>().aspectRatio = (float)nFiles/nRanks;
    }
    private int nRanks, nFiles;
    public void SetFEN(string FEN)
    {
        int nRanksNew = FEN.Count(c=>c=='/') + 1;
        int nFilesNew = FEN.IndexOf('/');
        if (nRanksNew != nRanks || nFilesNew != nRanks)
        {
            nRanks = nRanksNew;
            nFiles = nFilesNew;
            foreach (Transform child in transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            InitSquares();
        }

        int rank = nRanks-1;
        int file = -1;
        foreach (char c in FEN)
        {
            file += 1;
            int pos = rank * nFiles + file;
            if (c == ' ')
            {
                break;
            }
            if (c == '/')
            {
                if (file != nFiles)
                    throw new Exception("wrong number of squares in FEN rank " + rank);

                rank -= 1;
                file = -1;
            }
            else if (c > '0' && c <= '9')
            {
                int newFile = file + (c - '1'); // -1 because file will be incremented regardless
                for (int j=file; j<=newFile; j++)
                {
                    squares[pos+j].text = "";
                }
                file = newFile;
            }
            else
            {
                squares[pos].text = pieceSymbols[c];
                squares[pos].color = char.IsUpper(c)? Color.white : Color.black;
            }
        }
    }
    private static Dictionary<char, string> pieceSymbols = new Dictionary<char, string>() {
        { 'p', "♟" },
        { 'P', "♟" },
        { 'r', "♜" },
        { 'R', "♜" },
        { 'n', "♞" },
        { 'N', "♞" },
        { 'b', "♝" },
        { 'B', "♝" },
        { 'q', "♛" },
        { 'Q', "♛" },
        { 'k', "♚" },
        { 'K', "♚" },
    };
    public void FlipBoard(bool flipped)
    {
        if (flipped)
        {
            GetComponent<GridLayoutGroup>().startCorner = GridLayoutGroup.Corner.UpperRight;
        }
        else
        {
            GetComponent<GridLayoutGroup>().startCorner = GridLayoutGroup.Corner.LowerLeft;
        }
    }

}