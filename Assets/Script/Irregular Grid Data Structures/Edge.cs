using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Edge
{
    public List<Vertex> Vertices = new List<Vertex>(2);
    public Vertex a;
    public Vertex b;
    public List<CubeVertex> CubeVertices = new List<CubeVertex>(2);
    public CubeVertex a3;
    public CubeVertex b3;

    public List<Triangle> Triangles = new List<Triangle>();

    public bool isBoundaryRing = false; // This is used to determine if the edge is on the last (boundary) ring of the hexagon (aka the outer ring of the hexagon)

    public Edge(Vertex firstVertex, Vertex secondVertex)
    {
        Vertices.Add(firstVertex);
        Vertices.Add(secondVertex);
        this.a = firstVertex;
        this.b = secondVertex;
    }

    public Edge(CubeVertex firstCubeVertex, CubeVertex secondCubeVertex)
    {
        CubeVertices.Add(firstCubeVertex);
        CubeVertices.Add(secondCubeVertex);
        this.a3 = firstCubeVertex;
        this.b3 = secondCubeVertex;
    }

    public Vector3 GetCenter()
    {
        // return (Vertices[0].point + Vertices[1].point) / 2;
        return (a.point + b.point) / 2;
    }

    public Vertex GetOtherVertex(Vertex vertex)
    {
        if (vertex == a) return b;
        else if (vertex == b) return a;
        else throw new ArgumentException("The provided vertex is not part of this edge.");
    }

}