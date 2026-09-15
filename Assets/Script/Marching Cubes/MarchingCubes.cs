using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MC
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MarchingCubes : MonoBehaviour
    {
        //         4             5
        //         +-------------+
        //       / |           / |         
        //     /   |         /   |         
        // 7 +-----+-------+  6  |         
        //   |   0 +-------+-----+ 1       
        //   |   /         |   /           
        //   | /           | /             
        // 3 +-------------+ 2             
        // Bit values for each corner of a Marching Cube Cell.
        // Starting at the bottom-top-left corner, moving to the right.
        // Corner starts at origin and all corners move in a positive direction.

        [SerializeField] public int width;
        [SerializeField] public int height;

        [SerializeField] float resolution = 0f; //1f;
        [SerializeField] float noiseScale = 0f; //1f;

        [SerializeField] public float heightTresshold = 0f; //0.5f;

        [SerializeField] bool visualizeNoise;
        [SerializeField] bool use3DNoise;
        [SerializeField] public bool interpolate;

        public List<Vector3> vertices = new List<Vector3>();
        public List<int> triangles = new List<int>();
        public float[,,] heights; // aka scalar field
        public MeshFilter meshFilter;

        // public Mesh GenerateMesh(Func<Vector3, float> densityFunc)
        // {
        //     vertices.Clear();
        //     triangles.Clear();

        //     for (int x = 0; x < width; x++)
        //     {
        //         for (int y = 0; y < height; y++)
        //         {
        //             for (int z = 0; z < width; z++)
        //             {
        //                 Vector3 position = new Vector3(x, y, z) * resolution;
        //                 float[] cubeCorners = new float[8];

        //                 for (int i = 0; i < 8; i++)
        //                 {
        //                     Vector3 cornerPos = position + MarchingTable.Corners[i] * resolution;
        //                     cubeCorners[i] = densityFunc(cornerPos);
        //                 }

        //                 MarchCube(position, cubeCorners);
        //             }
        //         }
        //     }

        //     Mesh mesh = new Mesh();
        //     mesh.vertices = vertices.ToArray();
        //     mesh.triangles = triangles.ToArray();
        //     mesh.RecalculateNormals();

        //     return mesh;
        // }

        public Mesh GenerateMesh(System.Func<Vector3, float> densityFunc)
        {
            vertices.Clear();
            triangles.Clear();

            heights = new float[width + 1, height + 1, width + 1];

            // Sample scalar field
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    for (int z = 0; z <= width; z++)
                    {
                        Vector3 localPos = new Vector3(x, y, z);

                        heights[x, y, z] = densityFunc(localPos);
                    }
                }
            }

            // Run marching cubes
            MarchCubes();

            // Build mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            return mesh;
        }

        void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            // StartCoroutine(TestAll());
        }

        private IEnumerator TestAll()
        {
            while (true)
            {
                // PopulateTerrainMap();
                // PopulateSphereMap();
                // PopulateCubeMap();
                PopulateIrregularCubeMap();
                // PopulatePlaneMap();
                MarchCubes();
                SetMesh();
                yield return new WaitForSeconds(1f);
            }
        }

        public void SetMesh()
        {
            Mesh mesh = new Mesh();

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        // aka setHeights() aka PopulateTerrainMap() - is responsible for populating a 3D array called terrainMap/heights 
        // with scalar values that represent the density or "height" of the terrain at each point in a grid. 
        // These values will later be used by the Marching Cubes algorithm to generate a mesh representing the terrain's surface.
        public void PopulateTerrainMap()
        {
            heights = new float[width + 1, height + 1, width + 1];

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < width + 1; z++)
                    {
                        // Get a terrain height using regular old Perlin noise.
                        float currentHeight = height * Mathf.PerlinNoise(x / 16f * 1.5f + 0.001f, z / 16f * 1.5f + 0.001f);
                        // Set the value of this point in the terrainMap/heights array.
                        // heights array represents a grid (terrain map) of float values 
                        heights[x, y, z] = y - currentHeight;

                        // if (use3DNoise)
                        // {
                        //     float currentHeight = PerlinNoise3D((float)x / width * noiseScale, (float)y / height * noiseScale, (float)z / width * noiseScale);
                        //     heights[x, y, z] = currentHeight;
                        // }
                        // else
                        // {
                        //     float currentHeight = height * Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
                        //     float distToSufrace;

                        //     if (y <= currentHeight - 0.5f)
                        //         distToSufrace = 0f;
                        //     else if (y > currentHeight + 0.5f)
                        //         distToSufrace = 1f;
                        //     else if (y > currentHeight)
                        //         distToSufrace = y - currentHeight;
                        //     else
                        //         distToSufrace = currentHeight - y;

                        //     heights[x, y, z] = distToSufrace;
                        // }
                    }
                }
            }
        }

        public void PopulateSphereMap()
        {
            heights = new float[width + 1, height + 1, width + 1];
            Vector3 center = new Vector3(width / 2, height / 2, width / 2); // Center of the sphere
            float radius = width / 4; // Radius of the sphere

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < width + 1; z++)
                    {
                        // Compute the distance from the center of the sphere to the current grid point
                        float dx = x - center.x;
                        float dy = y - center.y;
                        float dz = z - center.z;

                        // Distance squared (more efficient than computing square root)
                        float distanceSquared = dx * dx + dy * dy + dz * dz;

                        // Compare this distance with the radius squared to determine the point's density value
                        float radiusSquared = radius * radius;
                        // We basically here calculate the implicit function of a sphere x^2 + y^2 + z^2 - r^2 = 0 to check if the xyz grid point is inside the sphere or not (aka satisfies the implicit function condition/constraint)
                        float density = distanceSquared - radiusSquared;

                        // Implicit equations for polygons:
                        //   - a sphere: sqrt(x^2 + y^2 + z^2) - r =0
                        //   - a cylinder/circle: sqrt(x^2 + z^2) - r = 0
                        //   - a plane: y - h = 0
                        //   - a box/cube: max(|x|, |y|, |z|) - r = 0


                        // Store the float density (grid) value in the scalar field
                        heights[x, y, z] = density;
                    }
                }
            }
        }

        public void PopulateCubeMap()
        {
            heights = new float[width + 1, height + 1, width + 1];
            float radius = width / 4;

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < width + 1; z++)
                    {
                        // Compute the distance from the center of the cube to the current grid point
                        float dx = Mathf.Abs(x - width / 2);
                        float dy = Mathf.Abs(y - height / 2);
                        float dz = Mathf.Abs(z - width / 2);

                        // Compare this distance with the radius squared to determine the point's density value
                        float density = Mathf.Max(dx, Mathf.Max(dy, dz)) - radius;

                        // Store the float density (grid) value in the scalar field
                        heights[x, y, z] = density;
                    }
                }
            }
        }

        public void PopulateIrregularCubeMap()
        {
            heights = new float[width + 1, height + 1, width + 1];
            float radius = width / 4;

            // Parameters for the irregularity
            float noiseScale = 0.5f; // Adjust this for more or less distortion
            float irregularityFactor = 2.0f; // How much the irregularity affects the shape

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < width + 1; z++)
                    {
                        // Compute the distance from the center of the cube to the current grid point
                        float dx = Mathf.Abs(x - width / 2);
                        float dy = Mathf.Abs(y - height / 2);
                        float dz = Mathf.Abs(z - width / 2);

                        // Base density value based on max distance
                        float density = Mathf.Max(dx, Mathf.Max(dy, dz)) - radius;

                        // Introduce noise or irregularity into the density calculation
                        float noiseValue = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
                        noiseValue += Mathf.PerlinNoise(y * noiseScale, x * noiseScale);

                        // Apply the irregularity to the density value
                        density += noiseValue * irregularityFactor;

                        // Store the float density (grid) value in the scalar field
                        heights[x, y, z] = density;
                    }
                }
            }
        }

        public void PopulatePlaneMap()
        {
            heights = new float[width + 1, height + 1, width + 1];
            float planeHeight = height / 2;

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    for (int z = 0; z < width + 1; z++)
                    {
                        // Compute the distance from the center of the plane to the current grid point
                        float dy = y - planeHeight;

                        // Store the float density (grid) value in the scalar field
                        heights[x, y, z] = dy;
                    }
                }
            }
        }

        private float PerlinNoise3D(float x, float y, float z)
        {
            float xy = Mathf.PerlinNoise(x, y);
            float xz = Mathf.PerlinNoise(x, z);
            float yz = Mathf.PerlinNoise(y, z);

            float yx = Mathf.PerlinNoise(y, x);
            float zx = Mathf.PerlinNoise(z, x);
            float zy = Mathf.PerlinNoise(z, y);

            return (xy + xz + yz + yx + zx + zy) / 6;
        }

        private int GetConfigIndex(float[] cubeCorners)
        {
            int configIndex = 0;

            for (int i = 0; i < 8; i++)
            {
                if (cubeCorners[i] > heightTresshold)
                {
                    configIndex |= 1 << i;
                }
            }

            return configIndex;
        }

        // resolution is numPointsPerAxis
        // public int indexFromCoord(int x, int y, int z)
        // {
        //     return z * (int)resolution * (int)resolution + y * (int)resolution + x;
        // }


        // private Vector3 Bilinear(Vector3 v00, Vector3 v10, Vector3 v11, Vector3 v01, float u, float v)
        // {
        //     return (1 - u) * (1 - v) * v00 +
        //         u * (1 - v) * v10 +
        //         u * v * v11 +
        //         (1 - u) * v * v01;
        // }

        // Vector3 DeformToSubQuad(int x, int y, int z, Cube cube)
        // {
        //     float u = (float)x / width;
        //     float v = (float)z / width;

        //     CubeVertex[] cubeVertices = cube.Vertices.ToArray();
        //     Vector3 v00 = cubeVertices[0].worldPoint;
        //     Vector3 v10 = cubeVertices[1].worldPoint;
        //     Vector3 v11 = cubeVertices[2].worldPoint;
        //     Vector3 v01 = cubeVertices[3].worldPoint;

        //     // Bilinear mapping (same as Approach 1)
        //     Vector3 bottom = Bilinear(v00, v10, v11, v01, u, v);

        //     // Bilinear mapping (same as Approach 1)
        //     // Vector3 bottom = Bilinear(
        //     //     cube.cubeVertices2[0].worldPoint,
        //     //     cube.cubeVertices2[1].worldPoint,
        //     //     cube.cubeVertices2[2].worldPoint,
        //     //     cube.cubeVertices2[3].worldPoint,
        //     //     u, v
        //     // );

        //     float height = (float)y / this.height;

        //     return bottom + Vector3.up * height;
        // }

        // float SampleDensity(Vector3 worldPos)
        // {
        //     // Example: cube centered around origin
        //     Vector3 center = new Vector3(width / 2f, height / 2f, width / 2f);
        //     float radius = width / 4f;

        //     float dx = Mathf.Abs(worldPos.x - center.x);
        //     float dy = Mathf.Abs(worldPos.y - center.y);
        //     float dz = Mathf.Abs(worldPos.z - center.z);

        //     return Mathf.Max(dx, Mathf.Max(dy, dz)) - radius;
        // }

        public void MarchCubes()
        {
            vertices.Clear();
            triangles.Clear();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < width; z++)
                    {
                        float[] cubeCorners = new float[8];

                        // Alternative 1 - define cube corners:
                        for (int i = 0; i < 8; i++)
                        {
                            Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                            cubeCorners[i] = heights[corner.x, corner.y, corner.z];

                            // World-space sampling
                            // Vector3 worldPos = DeformToSubQuad(x, y, z, cube);
                            // cubeCorners[i] = SampleDensity(worldPos);
                        }

                        // Alternative 2 - define cube corners:
                        // cubeCorners[0] = heights[x, y, z];
                        // cubeCorners[1] = heights[x + 1, y, z];
                        // cubeCorners[2] = heights[x + 1, y + 1, z];
                        // cubeCorners[3] = heights[x, y + 1, z];
                        // cubeCorners[4] = heights[x, y, z + 1];
                        // cubeCorners[5] = heights[x + 1, y, z + 1];
                        // cubeCorners[6] = heights[x + 1, y + 1, z + 1];
                        // cubeCorners[7] = heights[x, y + 1, z + 1];

                        MarchCube(new Vector3(x, y, z), cubeCorners);
                    }
                }
            }
        }

        private void MarchCube(Vector3 position, float[] cubeCorners)
        {
            int configIndex = GetConfigIndex(cubeCorners);

            if (configIndex == 0 || configIndex == 255)
            {
                return;
            }

            // Alternative 1 - construct vertices and triangles:
            for (int i = 0; MarchingTable.Triangles[configIndex, i] != -1; i++)
            {
                int edgeIndex = MarchingTable.Triangles[configIndex, i];

                Vector3 edgeStart = position + MarchingTable.Edges[edgeIndex, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[edgeIndex, 1];

                float val1 = heights[Vector3Int.FloorToInt(edgeStart).x, Vector3Int.FloorToInt(edgeStart).y, Vector3Int.FloorToInt(edgeStart).z];
                float val2 = heights[Vector3Int.FloorToInt(edgeEnd).x, Vector3Int.FloorToInt(edgeEnd).y, Vector3Int.FloorToInt(edgeEnd).z];

                float t = val1 / (val1 - val2);

                Vector3 vertex = Vector3.zero;
                if (interpolate) // Here we are deciding where along the edge (between edgeStart and edgeEnd points) we should construct the vertex.
                {
                    // vertex = (edgeStart + t * (edgeEnd - edgeStart)) / resolution; // We interpolate the vertex position between the edgeStart and edgeEnd points.
                    vertex = InterpolateVertex(edgeStart, edgeEnd, val1, val2);
                }
                else
                {
                    vertex = (edgeStart + edgeEnd) / 2; // We just take the middle point of the edge
                }

                vertices.Add(vertex);
                triangles.Add(vertices.Count - 1);
            }


            // Alternative 2 - construct vertices and triangles:
            // int edgeIndex = 0;
            // for (int t = 0; t < 5; t++)
            // {
            //     for (int v = 0; v < 3; v++)
            //     {
            //         int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

            //         if (triTableValue == -1)
            //         {
            //             return;
            //         }

            //         Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
            //         Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];

            //         float val1 = heights[Vector3Int.FloorToInt(edgeStart).x, Vector3Int.FloorToInt(edgeStart).y, Vector3Int.FloorToInt(edgeStart).z];
            //         float val2 = heights[Vector3Int.FloorToInt(edgeEnd).x, Vector3Int.FloorToInt(edgeEnd).y, Vector3Int.FloorToInt(edgeEnd).z];

            //         float tt = val1 / (val1 - val2);

            //         Vector3 vertex = Vector3.zero;
            //         if (interpolate)
            //         {
            //             vertex = (edgeStart + tt * (edgeEnd - edgeStart)) / resolution;
            //         }
            //         else
            //         {
            //             vertex = (edgeStart + edgeEnd) / 2;
            //         }

            //         vertices.Add(vertex);
            //         triangles.Add(vertices.Count - 1);

            //         edgeIndex++;
            //     }
            // }
        }

        // public Vector3 InterpolateVerts(Vector3 p1, Vector3 p2, float v1, float v2)
        // {
        //     float t = (heightTresshold - v1) / (v2 - v1);
        //     return Vector3.Lerp(p1, p2, t);
        // }

        // Calculate a point between two vertex using the weight of each vertex, used in interpolation voxel building.
        public Vector3 InterpolateVertex(Vector3 p1, Vector3 p2, float val1, float val2)
        {
            return Vector3.Lerp(p1, p2, (heightTresshold - val1) / (val2 - val1));
        }

        // public void OnDrawGizmosSelected()
        // {
        //     if (!visualizeNoise || !Application.isPlaying)
        //     {
        //         return;
        //     }

        //     for (int x = 0; x < width + 1; x++)
        //     {
        //         for (int y = 0; y < height + 1; y++)
        //         {
        //             for (int z = 0; z < width + 1; z++)
        //             {
        //                 Gizmos.color = new Color(heights[x, y, z], heights[x, y, z], heights[x, y, z], 1);
        //                 Gizmos.DrawSphere(new Vector3(x * resolution, y * resolution, z * resolution), 0.2f * resolution);
        //             }
        //         }
        //     }
        // }

        // public void UpdateMesh(List<Vector3> positions, List<int> indices)
        // {
        //     Mesh mesh = new Mesh();
        //     mesh.vertices = positions.ToArray();
        //     mesh.triangles = indices.ToArray();
        //     mesh.RecalculateNormals();

        //     _MeshFilter.mesh = mesh;
        //     _MeshCollider.sharedMesh = mesh;
        // }

    }

    // public struct Triangle_MC
    // {
    //     public Vector3 a;
    //     public Vector3 b;
    //     public Vector3 c;

    //     public Triangle_MC(Vector3 a, Vector3 b, Vector3 c)
    //     {
    //         this.a = a;
    //         this.b = b;
    //         this.c = c;
    //     }
    // }

    // public struct VoxelGrid
    // {
    //     public List<float> data;
    //     public int resolution; // = 1

    //     public VoxelGrid(int resolution)
    //     {
    //         this.resolution = resolution;
    //         data = new List<float>(resolution * resolution * resolution);
    //     }

    //     // public Vector3 this[int x, int y, int z] {
    //     //     get {
    //     //         return data[x + y * resolution + z * resolution * resolution];
    //     //     }
    //     //     set {
    //     //         data[x + y * resolution + z * resolution * resolution] = value;
    //     //     }
    //     // }

    //     public float Read(int x, int y, int z)
    //     {
    //         return data[x + y * resolution + z * resolution * resolution];
    //     }

    //     public void Push(float value)
    //     {
    //         data.Add(value);
    //     }
    // }
}