using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeVertex
{
    public float Y; // the y value of the vertex represents the height of the block/cube vertex
    public Vertex Vertex; // is a 2D vertex version of this 3D block/cube vertex
    public Vector3 worldPoint; // The position in 3D space, calculated as the 2D position of the vertex plus the height in the y direction.
    public bool IsActive; // as in is clicked in the game
    public bool IsBoundary; // is the vertex on the boundary of the mesh
    public SubQuad SubQuad;

    // Floor * Height passed as float y in the constructor below is used to determine 
    // the vertical position of the CubeVertex instances within a cube/block.
    // - Floor: This represents the level or layer of the cube/block. For example, a cube/block could be 
    //     stacked on top of another cube/block, making its floor level higher than the one below it.
    // - Height: This represents the height of each floor or layer in the stack of cubes/blocks.
    public CubeVertex(Vertex vertex, float y)
    {
        this.Y = y;
        this.Vertex = vertex;
        this.IsBoundary = vertex.IsBoundary;
        this.worldPoint = vertex.point + Vector3.up * y; // we want to have the world point of the vertex in the y plane, up direction is the y direction
    }
}
