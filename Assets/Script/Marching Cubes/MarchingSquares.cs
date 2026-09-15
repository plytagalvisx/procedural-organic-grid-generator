using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingSquares : MonoBehaviour
{
    [SerializeField][Range(5, 200)] int size = 15;
    [SerializeField][Range(0.01f, 0.2f)] float noiseResolution = 0.1f;
    [SerializeField][Range(0.05f, 1f)] float resolution = 1;
    [SerializeField][Range(0f, 1f)] float heightTreshold = 0.5f;

    [SerializeField] Transform circleParent;
    [SerializeField] Transform circlePrefab;

    private MeshFilter meshFilter;

    private float[,] heights;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();

        SetHeights();

        StartCoroutine(UpdateAll());
    }

    void Update()
    {
        Draw();
    }

    private IEnumerator UpdateAll()
    {
        while (true)
        {
            MarchSquares();
            CreateMesh();
            CreateGrid();
            yield return new WaitForSeconds(0.01f);
        }
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    private void MarchSquares()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                // square corners:
                float a = heights[x, y];
                float b = heights[x + 1, y];
                float c = heights[x + 1, y + 1];
                float d = heights[x, y + 1];

                MarchSquare(a, b, c, d, x, y);
            }
        }
    }

    int drawRange = 3;
    private void Draw()
    {
        Vector3Int mousePos = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.ScreenToWorldPoint(Vector3.zero));

        float finalDrawPower = 0;
        float drawPower = 0.3f;

        if (Input.GetMouseButton(0))
        {
            finalDrawPower = 1 * drawPower;
        }
        else if (Input.GetMouseButton(1))
        {
            finalDrawPower = -1 * drawPower;
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            for (int x = mousePos.x - drawRange; x < mousePos.x + drawRange; x++)
            {
                for (int y = mousePos.y - drawRange; y < mousePos.y + drawRange; y++)
                {
                    if (x > 0 && x < size && y > 0 && y < size)
                    {
                        float distanceToMouse = Vector2.Distance(new Vector2(x, y), new Vector2(mousePos.x, mousePos.y));
                        heights[x, y] = Mathf.Clamp(heights[x, y] + Time.deltaTime * finalDrawPower * Mathf.Clamp(drawRange - distanceToMouse, 0, 1000), 0, 1);
                    }
                }
            }
        }
    }

    private void MarchSquare(float a, float b, float c, float d, float offsetX, float offsetY)
    {
        int value = GetHeight(a) * 8 + GetHeight(b) * 4 + GetHeight(c) * 2 + GetHeight(d) * 1;

        Vector3 pointA = new Vector3(0, 0);
        Vector3 pointB = new Vector3(1, 0);
        Vector3 pointC = new Vector3(1, 1);
        Vector3 pointD = new Vector3(0, 1);

        Vector3[] verticesLocal = new Vector3[6];
        int[] trianglesLocal = new int[6];
        int vertexCount = vertices.Count;

        switch (value)
        {
            case 0:
                return;
            case 1:
                verticesLocal = new Vector3[]
                { new Vector3(0, 1f), new Vector3(Lerp(d, c, pointD, pointC).x, 1), new Vector3(0, Lerp(d, a, pointD, pointA).y) };

                trianglesLocal = new int[]
                { 0, 1, 2};
                break;
            case 2:
                verticesLocal = new Vector3[]
                { new Vector3(1, 1), new Vector3(1, Lerp(c, b, pointC, pointB).y), new Vector3(Lerp(c, d, pointC, pointD).x, 1) };

                trianglesLocal = new int[]
                { 0, 1, 2};
                break;
            case 3:
                verticesLocal = new Vector3[]
                { new Vector3(0, Lerp(d, a, pointD, pointA).y), new Vector3(0, 1), new Vector3(1, 1), new Vector3(1, Lerp(c, b, pointC, pointB).y) };

                trianglesLocal = new int[]
                { 0, 1, 2, 0, 2, 3};
                break;
            case 4:
                verticesLocal = new Vector3[]
                { new Vector3(1, 0), new Vector3(Lerp(b, a, pointB, pointA).x, 0), new Vector3(1, Lerp(b, c, pointB, pointC).y) };

                trianglesLocal = new int[]
                { 0, 1, 2};
                break;
            case 5:
                verticesLocal = new Vector3[]
                { new Vector3(0, Lerp(d, a, pointD, pointA).y), new Vector3(0, 1), new Vector3(Lerp(d, c, pointD, pointC).x, 1), new Vector3(1, 0), new Vector3(Lerp(b, a, pointB, pointA).x, 0), new Vector3(1, Lerp(b, c, pointB, pointC).y) };

                trianglesLocal = new int[]
                { 0, 1, 2, 3, 4, 5};
                break;
            case 6:
                verticesLocal = new Vector3[]
                { new Vector3(Lerp(b, a, pointB, pointA).x, 0), new Vector3(Lerp(c, d, pointC, pointD).x, 1), new Vector3(1, 1), new Vector3(1, 0) };

                trianglesLocal = new int[]
                { 0, 1, 2, 0, 2, 3};
                break;
            case 7:
                verticesLocal = new Vector3[]
                { new Vector3(0, 1), new Vector3(1, 1), new Vector3(1, 0), new Vector3(Lerp(b, a, pointB, pointA).x, 0), new Vector3(0, Lerp(d, a, pointD, pointA).y) };

                trianglesLocal = new int[]
                { 2, 3, 1, 3, 4, 1, 4, 0, 1};
                break;
            case 8:
                verticesLocal = new Vector3[]
                { new Vector3(0, Lerp(a, d, pointA, pointD).y), new Vector3(0, 0), new Vector3(Lerp(a, b, pointA, pointB).x, 0) };

                trianglesLocal = new int[]
                { 2, 1, 0};
                break;
            case 9:
                verticesLocal = new Vector3[]
                { new Vector3(0, 0), new Vector3(Lerp(a, b, pointA, pointB).x, 0), new Vector3(Lerp(d, c, pointD, pointC).x, 1), new Vector3(0, 1) };

                trianglesLocal = new int[]
                { 1, 0, 2, 0, 3, 2};
                break;
            case 10:
                verticesLocal = new Vector3[]
                { new Vector3(0, 0), new Vector3(0, Lerp(a, d, pointA, pointD).y), new Vector3(Lerp(a, b, pointA, pointB).x, 0), new Vector3(1, 1), new Vector3(Lerp(c, d, pointC, pointD).x, 1), new Vector3(1, Lerp(c, b, pointC, pointB).y) };

                trianglesLocal = new int[]
                { 0, 1, 2, 5, 4, 3};
                break;
            case 11:
                verticesLocal = new Vector3[]
                { new Vector3(0, 0), new Vector3(0, 1), new Vector3(1, 1), new Vector3(1, Lerp(c, b, pointC, pointB).y), new Vector3(Lerp(a, b, pointA, pointB).x, 0) };

                trianglesLocal = new int[]
                { 0, 1, 2, 0, 2, 3, 4, 0, 3};
                break;
            case 12:
                verticesLocal = new Vector3[]
                { new Vector3(0, 0), new Vector3(1, 0), new Vector3(1, Lerp(b, c, pointB, pointC).y), new Vector3(0, Lerp(a, d, pointA, pointD).y) };

                trianglesLocal = new int[]
                { 0, 3, 2, 0, 2, 1};
                break;
            case 13:
                verticesLocal = new Vector3[]
                { new Vector3(0, 0), new Vector3(0, 1), new Vector3(Lerp(d, c, pointD, pointC).x, 1), new Vector3(1, Lerp(b, c, pointB, pointC).y), new Vector3(1, 0) };

                trianglesLocal = new int[]
                { 0, 1, 2, 0, 2, 3, 0, 3, 4};
                break;
            case 14:
                verticesLocal = new Vector3[]
                { new Vector3(1, 1), new Vector3(1, 0), new Vector3(0, 0), new Vector3(0, Lerp(a, d, pointA, pointD).y), new Vector3(Lerp(c, d, pointC, pointD).x, 1) };

                trianglesLocal = new int[]
                { 0, 1, 4, 1, 3, 4, 1, 2, 3};
                break;
            case 15:
                verticesLocal = new Vector3[]
                { new Vector3(0, 0), new Vector3(0, 1), new Vector3(1, 1), new Vector3(1, 0) };

                trianglesLocal = new int[]
                { 0, 1, 2, 0, 2, 3};
                break;
        }

        foreach (Vector3 vert in verticesLocal)
        {
            Vector3 newVert = new Vector3((vert.x + offsetX) * resolution, (vert.y + offsetY) * resolution, 0);
            vertices.Add(newVert);
        }

        foreach (int triangle in trianglesLocal)
        {
            triangles.Add(triangle + vertexCount);
        }
    }

    private int GetHeight(float value)
    {
        return value < heightTreshold ? 0 : 1;
    }

    private void SetHeights()
    {
        heights = new float[size + 1, size + 1];

        for (int x = 0; x <= size; x++)
        {
            for (int y = 0; y <= size; y++)
            {
                heights[x, y] = Mathf.PerlinNoise(x * noiseResolution, y * noiseResolution);
            }
        }
    }

    private void CreateGrid()
    {
        foreach (Transform child in circleParent)
        {
            Destroy(child.gameObject);
        }

        for (int x = 0; x <= size; x++)
        {
            for (int y = 0; y <= size; y++)
            {
                Vector2 pos = transform.TransformPoint(new Vector2(x * resolution, y * resolution));
                Transform newCircle = Instantiate(circlePrefab, pos, new Quaternion(), circleParent);
                newCircle.localScale = Vector2.one * resolution / 2;
                newCircle.GetComponent<SpriteRenderer>().color = new Color(heights[x, y], heights[x, y], heights[x, y], 1);
            }
        }
    }

    private Vector3 Lerp(float edgeStart, float edgeEnd, Vector3 edgeStartVector, Vector3 edgeEndVector)
    {
        return Vector3.Lerp(edgeStartVector, edgeEndVector, (heightTreshold - edgeStart) / (edgeEnd - edgeStart));
    }
}

