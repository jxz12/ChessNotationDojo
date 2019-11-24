using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

public class BoardAscii : MonoBehaviour
{
    [SerializeField] GameObject squarePrefab;
    [SerializeField] Color lightSq, darkSq;
    [SerializeField] Material lightPc, darkPc;

    List<TMPro.TextMeshProUGUI> squares;

    void LateUpdate()
    {
        // var size = GetComponent<RectTransform>().sizeDelta;
        // float width = Mathf.Min(size.y / nRanks, size.x / nFiles);
        var rect = GetComponent<RectTransform>().rect;
        float width = Mathf.Min(rect.width / nRanks, rect.height / nFiles);
        GetComponent<GridLayoutGroup>().cellSize = new Vector2(width, width);
        GetComponent<GridLayoutGroup>().constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        GetComponent<GridLayoutGroup>().constraintCount = nFiles;
    }

    private int nRanks, nFiles;
    void InitSquares()
    {
        squares = new List<TMPro.TextMeshProUGUI>();
        for (int i=0; i<nRanks; i++)
        {
            for (int j=0; j<nFiles; j++)
            {
                var square = Instantiate(squarePrefab);
                square.GetComponent<Image>().color = (i+j)%2==0? darkSq : lightSq;
                square.name = i+" "+j;
                square.transform.SetParent(transform, false);
                square.transform.SetAsLastSibling();

                squares.Add(square.GetComponentInChildren<TMPro.TextMeshProUGUI>());
            }
        }
        // GetComponent<AspectRatioFitter>().aspectRatio = (float)nFiles/nRanks;
    }
    private string fen;
    public string FEN {
        get { return fen; }
        set {
            fen = value;
            int nRanksNew = fen.Count(c=>c=='/') + 1;
            int nFilesNew = 0;
            foreach (char c in fen)
            {
                if (c == '/')
                    break;
                else if (c >= '1' && c <= '9')
                    nFilesNew += c - '0';
                else
                    nFilesNew += 1;
            }
            if (nRanksNew != nRanks || nFilesNew != nRanks)
            {
                nRanks = nRanksNew;
                nFiles = nFilesNew;
                foreach (Transform child in transform)
                {
                    Destroy(child.gameObject);
                }
                InitSquares();
            }

            int rank = nRanks-1;
            int file = -1;
            foreach (char c in fen)
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
                else if (c >= '1' && c <= '9')
                {
                    int newFile = file + (c - '0');
                    for (int j=0; j<newFile-file; j++)
                    {
                        squares[pos+j].text = "";
                    }
                    file = newFile - 1; // -1 because file will be incremented regardless
                }
                else
                {
                    squares[pos].text = pieceSymbols[c];
                    squares[pos].fontMaterial = char.IsUpper(c)? lightPc : darkPc;
                }
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
    public void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        squares?.Clear();
        nFiles = nRanks = 0;
        fen = "";
    }

}