using UnityEngine;

namespace MeshModification
{
    /// <summary>
    /// Structure containing pre-calculated data needed for mesh modification.
    /// </summary>
    [System.Serializable]
    public struct MeshModificationData
    {
        /// <summary>
        /// Normalized coordinates of the mesh vertices.
        /// </summary>
        public Vector3[] NormalizedCoordinates;

        /// <summary>
        /// Bounds of the original mesh.
        /// </summary>
        public Bounds OriginalMeshBounds;

        public MeshModificationData(Vector3[] normalizedCoordinates, Bounds originalMeshBounds)
        {
            this.NormalizedCoordinates = normalizedCoordinates;
            this.OriginalMeshBounds = originalMeshBounds;
        }
    }
}

