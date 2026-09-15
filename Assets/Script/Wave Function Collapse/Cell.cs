using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Cell : MonoBehaviour
{
    public bool collapsed;
    public Tile[] tileOptions;
    public int NeighbourCount;
    public Vector3 Position;

    // Create a list of Neighbors:
    public List<Cell> Neighbors = new List<Cell>();
    public List<int> NeighbourIndices = new List<int>(); // direction index

    public void CreateCell(bool collapseState, Tile[] tiles, int neighbourCount, Vector3 position)
    {
        collapsed = collapseState;
        tileOptions = tiles;
        NeighbourCount = neighbourCount;
        Position = position;
    }

    public void UpdateCellTileOptions(Tile[] tiles)
    {
        tileOptions = tiles;
    }
}
