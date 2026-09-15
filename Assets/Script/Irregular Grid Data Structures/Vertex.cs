using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Vertex
{
    public Vector3 point;

    public List<Edge> Edges = new List<Edge>();

    public int RingIndex;

    public bool IsBoundary;

    public Vertex(Vector3 point, int ringIndex)
    {
        this.point = point;
        this.RingIndex = ringIndex;
    }

}

