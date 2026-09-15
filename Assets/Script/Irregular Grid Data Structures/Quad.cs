using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class Quad
{
    public List<Vertex> Vertices;
    public List<Edge> Edges;
    public Quad(List<Edge> edges, Vertex a, Vertex b, Vertex c, Vertex d)
    {
        this.Edges = edges;
        this.Vertices = new List<Vertex>(4) { a, b, c, d };
    }

    public Quad(List<Edge> edges, Vertex a, Vertex b, Vertex c)
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

    // This method will handle the creation of edge centers and the addition of new vertices and edges to the grid. 
    // It will also remove the old edges from the grid and replace them with the new subdivided edges that connect the old vertices to the new edge centers. 
    public void Subdivide(Grid grid, Vertex quadCentroidVertex, int ringCount)
    {
        List<Vertex> edgeCenters = new List<Vertex>();

        foreach (Edge quadEdge in Edges)
        {
            Vector3 edgeCenterPoint = quadEdge.GetCenter();
            Vertex edgeCenterVertex = grid.LookupVertexByPoint(edgeCenterPoint); // aka GetVertexAtPoint - checks if the vertex already exists in the (general) list of Vertices because we don't want to create duplicate vertices
            if (edgeCenterVertex == null)
            {
                grid.Edges.Remove(quadEdge);

                int boundary_ring_index = (quadEdge.isBoundaryRing) ? ringCount - 1 : 0; // if the edge is on the boundary ring, then the new vertex will be on the last ring of the hex grid
                edgeCenterVertex = new Vertex(edgeCenterPoint, boundary_ring_index);
                grid.Vertices.Add(edgeCenterVertex);

                Edge newSubEdgeAtoCenter = new Edge(quadEdge.a, edgeCenterVertex);
                grid.Edges.Add(newSubEdgeAtoCenter);
                quadEdge.a.Edges.Remove(quadEdge);
                quadEdge.a.Edges.Add(newSubEdgeAtoCenter);
                edgeCenterVertex.Edges.Add(newSubEdgeAtoCenter);

                Edge newSubEdgeBtoCenter = new Edge(quadEdge.b, edgeCenterVertex);
                grid.Edges.Add(newSubEdgeBtoCenter);
                quadEdge.b.Edges.Remove(quadEdge);
                quadEdge.b.Edges.Add(newSubEdgeBtoCenter);
                edgeCenterVertex.Edges.Add(newSubEdgeBtoCenter);
            }
            edgeCenters.Add(edgeCenterVertex);
        }

        grid.Vertices.Add(quadCentroidVertex);
        foreach (Vertex edgeCenter in edgeCenters)
        {
            Edge newSubEdgeCenterToCentroid = new Edge(edgeCenter, quadCentroidVertex);
            grid.Edges.Add(newSubEdgeCenterToCentroid);
            quadCentroidVertex.Edges.Add(newSubEdgeCenterToCentroid);
            edgeCenter.Edges.Add(newSubEdgeCenterToCentroid);
        }
    }
}




