using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DeformedSubQuadMesh : MonoBehaviour
{
    public Mesh baseMesh; // Assign your template quad mesh (0–1 space)

    private Mesh deformedMesh;

    public static Mesh CreateBaseQuad()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0), // 0
            new Vector3(1, 0, 0), // 1
            new Vector3(1, 0, 1), // 2
            new Vector3(0, 0, 1)  // 3
        };

        mesh.triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        mesh.RecalculateNormals();

        return mesh;
    }

    public void Build(Cube cube)
    {
        if (baseMesh == null)
        {
            baseMesh = CreateBaseQuad();
        }

        if (deformedMesh == null)
        {
            deformedMesh = new Mesh();
            deformedMesh.name = "DeformedMesh";
            GetComponent<MeshFilter>().mesh = deformedMesh;
        }
        else
        {
            deformedMesh.Clear();
        }

        Vector3[] baseVerts = baseMesh.vertices;
        Vector3[] newVerts = new Vector3[baseVerts.Length];

        // CubeVertex[] cv = cube.cubeVertices2;
        CubeVertex[] cv = cube.Vertices.ToArray();

        // --- Get quad corners ---
        // Bottom layer (use these for XZ shape)
        Vector3 v00 = cv[0].worldPoint;
        Vector3 v10 = cv[1].worldPoint;
        Vector3 v11 = cv[2].worldPoint;
        Vector3 v01 = cv[3].worldPoint;

        // Top layer (for height blending)
        Vector3 t00 = cv[4].worldPoint;
        Vector3 t10 = cv[5].worldPoint;
        Vector3 t11 = cv[6].worldPoint;
        Vector3 t01 = cv[7].worldPoint;

        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 v = baseVerts[i];

            float u = v.x; // assume 0–1
            float w = v.z; // assume 0–1

            // --- Bilinear interpolation for position ---
            Vector3 bottom = Bilinear(v00, v10, v11, v01, u, w);
            Vector3 top = Bilinear(t00, t10, t11, t01, u, w);

            // --- Height blending ---
            float heightLerp = GetHeightLerp(cube, u, w);

            newVerts[i] = Vector3.Lerp(bottom, top, heightLerp);
        }

        deformedMesh.vertices = newVerts;
        deformedMesh.triangles = baseMesh.triangles;
        deformedMesh.uv = baseMesh.uv;

        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();
    }

    // -------------------------
    // Bilinear interpolation
    // -------------------------
    private Vector3 Bilinear(Vector3 v00, Vector3 v10, Vector3 v11, Vector3 v01, float u, float v)
    {
        return (1 - u) * (1 - v) * v00 +
               u * (1 - v) * v10 +
               u * v * v11 +
               (1 - u) * v * v01;
    }

    // -------------------------
    // Decide how much "top" influence we want
    // -------------------------
    private float GetHeightLerp(Cube cube, float u, float v)
    {
        // Simple version: average active top vertices
        CubeVertex[] cv = cube.Vertices.ToArray();

        float total = 0f;
        float weight = 0f;

        for (int i = 4; i < 8; i++)
        {
            if (cv[i] != null && cv[i].IsActive)
            {
                total += 1f;
            }
            weight += 1f;
        }

        return weight > 0 ? total / weight : 0f;
    }
}