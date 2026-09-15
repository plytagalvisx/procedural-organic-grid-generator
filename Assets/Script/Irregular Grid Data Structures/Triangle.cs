using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Triangle
{
    public bool isMergedIntoQuad = false;
    public List<Vertex> Vertices;
    public List<Edge> Edges;

    public Triangle(List<Edge> edges, Vertex a, Vertex b, Vertex c)
    {
        this.Edges = edges;
        this.Vertices = new List<Vertex>(3) { a, b, c };
    }

    public Vector3 GetCentroid()
    {
        Vector3 centroid = Vector3.zero;
        foreach (Vertex vertex in Vertices)
        {
            centroid += vertex.point;
        }
        centroid /= (float)Vertices.Count;
        return centroid;
    }
}