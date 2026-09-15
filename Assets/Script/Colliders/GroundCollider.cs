using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCollider : MonoBehaviour
{
    // We create a grid ground mesh collider for each subquad in the grid (for the entire grid).
    public void CreateGridGroundCollider(Grid grid)
    {
        foreach (SubQuad subQuad in grid.SubQuads)
        {
            List<Vector3> subquadVertices = new List<Vector3>();
            foreach (Vertex vertex in subQuad.Vertices)
            {
                subquadVertices.Add(vertex.point);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = subquadVertices.ToArray();
            mesh.triangles = new int[6] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();

            GameObject ground = new GameObject("GroundCollider_" + grid.SubQuads.IndexOf(subQuad), typeof(MeshCollider), typeof(GroundColliderSubQuad));
            ground.transform.parent = transform;
            ground.GetComponent<MeshCollider>().sharedMesh = mesh;
            ground.GetComponent<GroundColliderSubQuad>().SubQuad = subQuad;
            ground.layer = LayerMask.NameToLayer("GroundCollider");
        }
    }
}

public class GroundColliderSubQuad : MonoBehaviour
{
    public SubQuad SubQuad;
}
