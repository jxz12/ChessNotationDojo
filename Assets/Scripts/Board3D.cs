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
        // if (flipped)
        //     transform.localRotation = Quaternion.Euler(0,180,0);
        // else
        //     transform.localRotation = Quaternion.identity;

        // TODO: make this drag to spin
        // TODO: highlight ranks and files
    }
    GameObject[,] squares;
    void InitSquares(int nRanks, int nFiles)
    {
        if (squares != null)
        {
            foreach (var go in squares)
                GameObject.Destroy(go);
        }
        squares = new GameObject[nFiles, nRanks];
        float scale = 1f/Mathf.Max(nFiles, nRanks);
        Vector3 origin = new Vector3(-(nFiles-1)*scale*.5f, 0, -(nRanks-1)*scale*.5f);
        for (int rank=0; rank<nRanks; rank++)
        {
            for (int file=0; file<nFiles; file++)
            {
                squares[file, rank] = Instantiate(squarePrefab, transform);
                squares[file, rank].name = (char)(file+'a') + "" + (rank+1);
                squares[file, rank].transform.localScale = new Vector3(scale, scale, scale);
                squares[file, rank].transform.localPosition = origin + new Vector3(file*scale, 0, rank*scale);
                squares[file, rank].GetComponent<MeshRenderer>().material.color = (rank+file)%2==0? darkSq:lightSq;
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
                if (char.IsUpper(c))
                {
                    piece.GetComponent<MeshRenderer>().material.color = Color.white;
                    piece.transform.rotation = Quaternion.identity;
                }
                else
                {
                    piece.GetComponent<MeshRenderer>().material.color = Color.black;
                    piece.transform.rotation = Quaternion.Euler(0,180,0);
                }
            }
        }
    }
    public void PlayMoveUCI(string UCI)
    {
        int sourceFile = UCI[0] - 'a';
        int sourceRank = UCI[1];
        int targetFile = UCI[2] - 'a';
        int targetRank = UCI[3];

        if (squares[targetFile, targetRank].transform.childCount > 0)
        {
            Destroy(squares[targetFile, targetRank].transform.GetChild(0));
        }
        squares[sourceFile, sourceRank].transform.GetChild(0).SetParent(squares[targetFile, targetRank].transform, false);
        if (UCI.Length > 4)
        {
            squares[sourceFile, sourceRank].transform.GetChild(0).GetComponent<MeshFilter>().mesh = pieceMeshes[UCI[4]];
        }
    }
}