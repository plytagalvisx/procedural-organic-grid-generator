using UnityEngine;

namespace MeshModification
{
    /// <summary>
    /// Static utility class for mesh modification operations
    /// </summary>
    public static class MeshModificationUtils
    {
        /// <summary>
        /// Creates a copy of the provided mesh.
        /// </summary>
        /// <param name="originalMesh">The mesh to copy.</param>
        /// <returns>A copy of the provided mesh.</returns>
        public static Mesh CopyMesh(Mesh originalMesh)
        {
            if (originalMesh == null)
            {
                return null;
            }

            Mesh meshCopy = new Mesh();
            meshCopy.name = originalMesh.name + "_Deformed";

            // Copy basic mesh data
            meshCopy.vertices = originalMesh.vertices;
            meshCopy.normals = originalMesh.normals;
            meshCopy.tangents = originalMesh.tangents;
            meshCopy.uv = originalMesh.uv;
            meshCopy.uv2 = originalMesh.uv2;
            meshCopy.uv3 = originalMesh.uv3;
            meshCopy.uv4 = originalMesh.uv4;
            meshCopy.uv5 = originalMesh.uv5;
            meshCopy.uv6 = originalMesh.uv6;
            meshCopy.uv7 = originalMesh.uv7;
            meshCopy.uv8 = originalMesh.uv8;
            meshCopy.colors = originalMesh.colors;
            meshCopy.colors32 = originalMesh.colors32;

            // Copy submeshes
            meshCopy.subMeshCount = originalMesh.subMeshCount;
            for (int i = 0; i < originalMesh.subMeshCount; i++)
            {
                meshCopy.SetTriangles(originalMesh.GetTriangles(i), i);
            }

            return meshCopy;
        }

        /// <summary>
        /// Calculates normalized coordinates (0-1) for vertices within the mesh bounds
        /// </summary>
        /// <param name="vertices">Original mesh vertices</param>
        /// <param name="bounds">Original mesh bounds</param>
        /// <returns>MeshModificationData containing normalized coordinates and bounds</returns>
        public static MeshModificationData CalculateMeshModificationData(Vector3[] vertices, Bounds bounds)
        {
            Vector3 minPosition = bounds.min;
            Vector3 boundsSize = bounds.size;
            Vector3[] normalizedCoordinates = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                float u = boundsSize.x > 0f ? (vertices[i].x - minPosition.x) / boundsSize.x : 0.5f;
                float v = boundsSize.y > 0f ? (vertices[i].y - minPosition.y) / boundsSize.y : 0.5f;
                float w = boundsSize.z > 0f ? (vertices[i].z - minPosition.z) / boundsSize.z : 0.5f;

                normalizedCoordinates[i] = new Vector3(u, v, w);
            }

            return new MeshModificationData(normalizedCoordinates, bounds);
        }

        /// <summary>
        /// Applies lattice deformation to vertices using trilinear interpolation.
        /// </summary>
        /// <param name="originalVertices">Original mesh vertices.</param>
        /// <param name="data">Pre-calculated mesh modification data.</param>
        /// <param name="latticeControlPoints">8 lattice control points in local space, ordered clockwise as:
        /// [0]=back-bottom-left, [1]=back-bottom-right, [2]=front-bottom-right, [3]=front-bottom-left,
        /// [4]=back-top-left, [5]=back-top-right, [6]=front-top-right, [7]=front-top-left.</param>
        /// <returns>Deformed vertex array.</returns>
        public static Vector3[] ApplyLatticeDeformation(Vector3[] originalVertices, MeshModificationData data, Vector3[] latticeControlPoints)
        {
            if (latticeControlPoints == null || latticeControlPoints.Length != 8)
            {
                Debug.LogError("Lattice control points must contain exactly 8 points.");
                return originalVertices;
            }

            Vector3[] deformedVertices = new Vector3[originalVertices.Length];

            for (int i = 0; i < originalVertices.Length; i++)
            {
                deformedVertices[i] = TrilinearInterpolation(data.NormalizedCoordinates[i], latticeControlPoints);
            }

            return deformedVertices;
        }

        /// <summary>
        /// Performs trilinear interpolation between 8 lattice control points.
        /// </summary>
        private static Vector3 TrilinearInterpolation(Vector3 normalizedCoordinates, Vector3[] latticeControlPoints)
        {
            // Get normalized position (0-1) within the original lattice.
            float u = normalizedCoordinates.x;
            float v = normalizedCoordinates.y;
            float w = normalizedCoordinates.z;

            // Map lattice control points to trilinear coordinates.
            // c[u][v][w] where 0 = min, 1 = max
            // Note: In this coordinate system, back = z=max, front = z=min
            Vector3 c000 = latticeControlPoints[3]; // x=min, y=min, z=min (front-bottom-left)
            Vector3 c100 = latticeControlPoints[2]; // x=max, y=min, z=min (front-bottom-right)
            Vector3 c010 = latticeControlPoints[7]; // x=min, y=max, z=min (front-top-left)
            Vector3 c110 = latticeControlPoints[6]; // x=max, y=max, z=min (front-top-right)
            Vector3 c001 = latticeControlPoints[0]; // x=min, y=min, z=max (back-bottom-left)
            Vector3 c101 = latticeControlPoints[1]; // x=max, y=min, z=max (back-bottom-right)
            Vector3 c011 = latticeControlPoints[4]; // x=min, y=max, z=max (back-top-left)
            Vector3 c111 = latticeControlPoints[5]; // x=max, y=max, z=max (back-top-right)

            // Trilinear interpolation formula.
            Vector3 result =
                c000 * (1 - u) * (1 - v) * (1 - w) +
                c100 * u * (1 - v) * (1 - w) +
                c010 * (1 - u) * v * (1 - w) +
                c110 * u * v * (1 - w) +
                c001 * (1 - u) * (1 - v) * w +
                c101 * u * (1 - v) * w +
                c011 * (1 - u) * v * w +
                c111 * u * v * w;

            return result;
        }
    }
}

