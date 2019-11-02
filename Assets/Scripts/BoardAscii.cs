using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class BoardAscii : MonoBehaviour
{
    [SerializeField] GridLayoutGroup squaresParent;
    [SerializeField] GameObject squarePrefab;
    [SerializeField] Color lightSq, darkSq;

    List<Text> squares;
    private int nRanks, nFiles;

    public void InitFEN(string FEN)
    {
        nRanks = FEN.IndexOf('/');
        nFiles = FEN.Split('/').Length - 1;
        squares = new List<Text>();
        for (int i=0; i<nRanks; i++)
        {
            for (int j=0; j<nFiles; j++)
            {
                var square = Instantiate(squarePrefab);
                square.GetComponent<Image>().color = (i+j)%2==0? darkSq : lightSq;
                square.name = i+" "+j;
                square.transform.SetParent(squaresParent.transform, false);
                square.transform.SetAsLastSibling();

                squares.Add(square.GetComponentInChildren<Text>());
            }
        }
        squaresParent.gameObject.GetComponent<AspectRatioFitter>().aspectRatio = (float)nFiles/nRanks;
        var rect = squaresParent.GetComponent<RectTransform>().rect;
        float width = rect.height / (float)nRanks;
        squaresParent.cellSize = new Vector2(width, width);

        SetFEN(FEN);
    }
    public void SetFEN(string FEN)
    {
        int rank = nRanks-1;
        int file = -1;
        int i = 0;
        while (FEN[i] != ' ')
        {
            file += 1;
            int pos = rank * nFiles + file;
            if (FEN[i] == '/')
            {
                if (file != nFiles)
                    throw new Exception("wrong number of squares in FEN rank " + rank);

                rank -= 1;
                file = -1;
            }
            else if (FEN[i] > '0' && FEN[i] <= '9')
            {
                file += FEN[i] - '1'; // -1 because file will be incremented regardless
            }
            else
            {
                squares[pos].text = pieceSymbols[FEN[i]];
            }

            squares[pos].color = char.IsUpper(FEN[i])? Color.white : Color.black;

            i += 1;
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
            squaresParent.startCorner = GridLayoutGroup.Corner.UpperRight;
        }
        else
        {
            squaresParent.startCorner = GridLayoutGroup.Corner.LowerLeft;
        }
    }

}