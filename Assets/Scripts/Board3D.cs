using System;
using UnityEngine;
using System.Collections.Generic;

public class Board3D : MonoBehaviour
{
    [SerializeField] GameObject squarePrefab;
    [SerializeField] GameObject piecePrefab;
    [SerializeField] Mesh pawn, rook, knight, bishop, queen, king;
    [SerializeField] Color lightSq, darkSq;

    private Dictionary<char, Mesh> pieceMeshes;
    void Awake()
    {
        pieceMeshes = new Dictionary<char, Mesh>() {
            { 'p', pawn },
            { 'P', pawn },
            { 'r', rook },
            { 'R', rook },
            { 'n', knight },
            { 'N', knight },
            { 'b', bishop },
            { 'B', bishop },
            { 'q', queen },
            { 'Q', queen },
            { 'k', king },
            { 'K', king },
        };
    }
    
    public void FlipBoard(bool flipped)
    {
        // TODO: make this drag to spin
    }
    GameObject[,] squares;
    void InitSquares(int nRanks, int nFiles)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        squares = new GameObject[nFiles, nRanks];
        for (int rank=0; rank<nRanks; rank++)
        {
            for (int file=0; file<nFiles; file++)
            {
                squares[file, rank] = Instantiate(squarePrefab, transform);
                squares[file, rank].transform.localPosition = new Vector3(file, 0, rank);
                squares[file, rank].GetComponent<MeshRenderer>().material.color = (rank+file)%2==0? lightSq:darkSq;
            }
        }
    }
    public void InitFEN(string FEN)
    {
        int nRanks = FEN.Split('/').Length;
        int nFiles = 0;
        foreach (char c in FEN)
        {
            if (c == '/')
                break;
            else if (c >= '1' && c <= '9')
                nFiles += c - '0';
            else
                nFiles += 1;
        }
        InitSquares(nRanks, nFiles);

        int rank = nRanks-1;
        int file = -1;
        foreach (char c in FEN)
        {
            file += 1;
            // int pos = rank * nFiles + file;
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
                    // empty square
                }
                file = newFile - 1; // -1 because file will be incremented regardless
            }
            else
            {
                var piece = Instantiate(piecePrefab, squares[file, rank].transform);
                piece.GetComponent<MeshFilter>().mesh = pieceMeshes[c];
                piece.GetComponent<MeshRenderer>().material.color = char.IsUpper(c)? Color.white : Color.black;
            }
        }
    }
    public void PlayMoveUCI(string UCI)
    {

    }
}