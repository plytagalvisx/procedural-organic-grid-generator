using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubQuad
{
    public List<Vertex> Vertices;
    public List<Edge> Edges;
    public Vector3 centroid;

    public SubQuad(List<Edge> edges)
    {
        this.Edges = edges;

        List<Vertex> vertices = new List<Vertex>();
        foreach (Edge edge in edges)
        {
            foreach (Vertex vertex in edge.Vertices)
            {
                if (!vertices.Contains(vertex))
                {
                    vertices.Add(vertex);
                }
            }
        }

        this.Vertices = vertices;
        this.centroid = GetCentroid();
    }

    public Vector3 GetCentroid()
    {
        Vector3 centroid = Vector3.zero;
        foreach (Vertex vertex in this.Vertices)
        {
            centroid += vertex.point;
        }
        return centroid / Vertices.Count;
    }
}
