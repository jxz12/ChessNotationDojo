using UnityEngine;
using System.Collections.Generic;

public class Board3D : MonoBehaviour
{
    [SerializeField] GameObject squarePrefab;
    [SerializeField] GameObject piecePrefab;
    [SerializeField] Mesh pawn, rook, knight, bishop, queen, king;
    [SerializeField] Color lightSq, darkSq;

    public void FlipBoard(bool flipped)
    {
        // TODO: make this drag to spin
    }
    void InitSquares(int nRanks, int nFiles)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        // TODO: do things
    }
    public void InitFEN(string FEN)
    {
        int nRanks = FEN.IndexOf('/');
        int nFiles = FEN.Split('/').Length;
        InitSquares(nRanks, nFiles);
    }
    public void PlayMoveUCI(string UCI)
    {

    }
}