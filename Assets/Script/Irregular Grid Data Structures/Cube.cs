using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube
{
    public float Height;
    public int Floor;

    public List<CubeVertex> Vertices; // The vertices of the cube/block. Total 8 vertices if it's a 4-sided quad block/cube; 4 lower and 4 upper vertices.
    public List<Edge> Edges; // The edges of the cube/block
    public SubQuad SubQuad; // The subquad that this cube/block is based on (aka a face)
    public Vector3 centroid; // The centroid point of the cube/block obtained from the subquad (face)

    public int CubeCornerBitValue; // The module name/index/bitmask of the cube/block
    public int PrevCubeCornerBitValue;


    // This calculation ensures that each cube/block has a consistent height and is positioned correctly 
    // in a stacked structure, with the vertices of each cube/block being placed appropriately in 3D space based on their floor level.
    public Cube(SubQuad subQuad, int floor, float height)
    {
        this.SubQuad = subQuad; // The 2D face (subquad) that the cube/block is based on
        this.Height = height; // The height of the cube/block
        this.Floor = floor; // The floor level of the cube/block

        this.centroid = CalculateCubeCentroid(subQuad, floor, height);

        List<CubeVertex> upperVertices;
        List<CubeVertex> lowerVertices;

        this.Vertices = CreateVertices(subQuad, floor, height, out upperVertices, out lowerVertices);
        this.Edges = CreateEdges(subQuad, upperVertices, lowerVertices);
    }

    private Vector3 CalculateCubeCentroid(SubQuad subQuad, int floor, float height)
    {
        return subQuad.GetCentroid() + Vector3.up * ((floor + 0.5f) * height);
    }

    private List<CubeVertex> CreateVertices(SubQuad subQuad, int floor, float height, out List<CubeVertex> upperVertices, out List<CubeVertex> lowerVertices)
    {
        upperVertices = new List<CubeVertex>();
        lowerVertices = new List<CubeVertex>();

        foreach (Vertex vertex in subQuad.Vertices)
        {
            lowerVertices.Add(new CubeVertex(vertex, floor * height)); // create a 3D vertex with the same position as the 2D vertex but with the height of the floor and add the 3D vertex to the lower part of the cube/block.
            upperVertices.Add(new CubeVertex(vertex, (floor + 1) * height));
        }

        List<CubeVertex> vertices = new List<CubeVertex>();
        vertices.AddRange(lowerVertices);
        vertices.AddRange(upperVertices);

        return vertices;
    }

    private List<Edge> CreateEdges(SubQuad subQuad, List<CubeVertex> upperVertices, List<CubeVertex> lowerVertices)
    {
        List<Edge> edgeLower = new List<Edge>();
        List<Edge> edgeUpper = new List<Edge>();
        List<Edge> edgeMiddle = new List<Edge>();

        for (int i = 0; i < subQuad.Vertices.Count; i++)
        {
            edgeLower.Add(new Edge(lowerVertices[i], lowerVertices[(i + 1) % subQuad.Vertices.Count]));
            edgeUpper.Add(new Edge(upperVertices[i], upperVertices[(i + 1) % subQuad.Vertices.Count]));
            edgeMiddle.Add(new Edge(lowerVertices[i], upperVertices[i]));
        }

        List<Edge> edges = new List<Edge>();
        edges.AddRange(edgeLower);
        edges.AddRange(edgeUpper);
        edges.AddRange(edgeMiddle);

        return edges;
    }

    // Bitmasking Algorithm: allows us to select a mesh/geometry for each cube/block corner element:
    // https://www.youtube.com/watch?v=ZlZbzr-czxk
    public void UpdateCubeCornerBitValue()
    {
        int selectedCubeCornerBitValue = 0;
        Cube currentCube = this;
        CubeVertex[] cubeVertices = currentCube.Vertices.ToArray();

        // Cube/Block has 8 vertices (4 bottom and 4 top vertices). 
        // When we click on a subquad corner target point (to build a block/cube on it) in the grid
        // we activate the corner vertex of each subquad/block/cube surrounding (aka neighbouring) the target point. 
        // If the activated corner was connected to 4 edges/subquads, then the corner vertex will be activated for each subquad/block/cube.
        // The vertices are activated by setting their IsActive property to true.

        // The index of each 8 block/cube vertex that corresponds to the module name (cube corner value/bitmask).
        // int[] cubeCornerBitValues = { 2, 4, 8, 1, 32, 64, 128, 16 };
        int[] cubeCornerBitValues = { 64, 32, 16, 128, 4, 2, 1, 8 };
        // This list contains the module mesh names that correspond to the 8 block/cube vertices (Mesh Calculation).
        // Indices 0-3 correspond to the bottom vertices which are used for the roof module meshes.
        // Indices 4-7 correspond to the top vertices which are used for meshes will walls to be built on top of the roof module meshes.

        // Loop through each vertex, check if it's active and set/select the corresponding module index/name (cubeModuleIndex update)
        // that we want to build in the grid.
        for (int i = 0; i < cubeVertices.Length; i++)
        {
            if (cubeVertices[i] != null && cubeVertices[i].IsActive)
            {
                selectedCubeCornerBitValue += cubeCornerBitValues[i];
            }
        }

        this.PrevCubeCornerBitValue = this.CubeCornerBitValue;
        this.CubeCornerBitValue = selectedCubeCornerBitValue; // This module's bitvalue/bitmask is used to identify and retrieve the corresponding block/cube module mesh for the cube/block corner element.    
    }

}

