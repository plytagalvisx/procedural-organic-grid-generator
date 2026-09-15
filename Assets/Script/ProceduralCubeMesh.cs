using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralCubeMesh : MonoBehaviour
{
    private Mesh mesh;

    public void Build(Cube cube)
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "ProceduralCube";
            GetComponent<MeshFilter>().mesh = mesh;
        }
        else
        {
            mesh.Clear();
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // CubeVertex[] v = cube.cubeVertices2;
        CubeVertex[] v = cube.Vertices.ToArray();

        // -----------------------
        // TOP FACE (indices 4–7)
        // -----------------------
        bool anyTopActive = false;
        for (int i = 4; i < 8; i++)
        {
            if (v[i] != null && v[i].IsActive)
            {
                anyTopActive = true;
                break;
            }
        }

        if (anyTopActive)
        {
            int start = vertices.Count;

            vertices.Add(v[4].worldPoint);
            vertices.Add(v[5].worldPoint);
            vertices.Add(v[6].worldPoint);
            vertices.Add(v[7].worldPoint);

            // two triangles
            triangles.Add(start + 0);
            triangles.Add(start + 1);
            triangles.Add(start + 2);

            triangles.Add(start + 0);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        // -----------------------
        // BOTTOM FACE (optional)
        // -----------------------
        bool anyBottomActive = false;
        for (int i = 0; i < 4; i++)
        {
            if (v[i] != null && v[i].IsActive)
            {
                anyBottomActive = true;
                break;
            }
        }

        if (anyBottomActive)
        {
            int start = vertices.Count;

            vertices.Add(v[0].worldPoint);
            vertices.Add(v[1].worldPoint);
            vertices.Add(v[2].worldPoint);
            vertices.Add(v[3].worldPoint);

            // reverse winding (facing down)
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 0);

            triangles.Add(start + 3);
            triangles.Add(start + 2);
            triangles.Add(start + 0);
        }

        // -----------------------
        // SIDE WALLS
        // -----------------------
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;

            CubeVertex bottomA = v[i];
            CubeVertex bottomB = v[next];
            CubeVertex topA = v[i + 4];
            CubeVertex topB = v[next + 4];

            bool bottomActive = bottomA != null && bottomB != null &&
                                bottomA.IsActive && bottomB.IsActive;

            bool topActive = topA != null && topB != null &&
                             topA.IsActive && topB.IsActive;

            // Create wall if there is a difference OR top exists
            if (topActive || bottomActive)
            {
                int start = vertices.Count;

                vertices.Add(bottomA.worldPoint);
                vertices.Add(bottomB.worldPoint);
                vertices.Add(topB.worldPoint);
                vertices.Add(topA.worldPoint);

                triangles.Add(start + 0);
                triangles.Add(start + 1);
                triangles.Add(start + 2);

                triangles.Add(start + 0);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }
        }

        // -----------------------
        // APPLY MESH
        // -----------------------
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}