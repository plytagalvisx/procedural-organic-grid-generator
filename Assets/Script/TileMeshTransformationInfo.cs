using System;

namespace Tiles
{
    /// <summary>
    /// Structure containing information about the transformation required to transform a 
    /// unique tile mesh into another tile mesh variant. By using transformations, we reduce the number of required
    /// unique tile meshes we need to generate and store.
    /// </summary>
    [Serializable]
    public struct TileMeshTransformationInfo
    {
        /// <summary>
        /// Rotation in degrees required to transform the unique tile mesh into the variant tile mesh.
        /// </summary>
        public float RotationDegrees;
        /// <summary>
        /// Indicates whether the unique tile mesh must be flipped horizontally to produce the variant tile mesh.
        /// </summary>
        public bool FlipHorizontally;
        /// <summary>
        /// Indicates whether the unique tile mesh must be flipped vertically to produce the variant tile mesh.
        /// </summary>
        public bool FlipVertically;
    }
}