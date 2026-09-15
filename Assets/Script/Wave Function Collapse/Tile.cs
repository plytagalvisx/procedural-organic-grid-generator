// using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    WATER,
    LAND
}

[System.Serializable]
public class TileEdge
{
    public List<Tile> neighbours;
}

public class Tile : MonoBehaviour
{
    public TileType tileType;

    public Tile[] upNeighbours;
    public Tile[] rightNeighbours;
    public Tile[] downNeighbours;
    public Tile[] leftNeighbours;

    // public List<Tile[]> neighboursByEdge;
    public List<TileEdge> neighboursByEdge;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        // Uses DOTween to animate the scale of the tile from zero to one over 1 second with an elastic easing effect.
        // transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutElastic);
    }
}
