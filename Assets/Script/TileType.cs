using System;

namespace TilePlacement
{
    /// <summary>
    /// The type of tile that can be placed on the grid.
    /// </summary>
    [Serializable]
    public enum TileType : byte
    {
        WATER = 0,
        LAND = 1
    }

    public static class TileTypeExtensions
    {
        /// <summary>
        /// Returns the naming mark for the tile type.
        /// </summary>
        /// <param name="tileType">The tile type to get the naming mark for.</param>
        /// <returns>The naming mark for the tile type. This mark is used to construct the name of the tile mesh asset.</returns>
        public static string GetTileNameAbbrevation(this TileType tileType)
        {
            switch (tileType)
            {
                case TileType.LAND:
                    return "L";
                case TileType.WATER:
                    return "W";
                default:
                    return "W";
            }
        }

        /// <summary>
        /// Converts a tile name abbrevation to a tile type.
        /// </summary>
        /// <param name="abbreviation">The abbreviation to convert.</param>
        /// <returns>The tile type corresponding to the abbreviation.</returns>
        public static TileType FromNameAbbreviation(string abbreviation)
        {
            switch (abbreviation)
            {
                case "L":
                    return TileType.LAND;
                case "W":
                default:
                    return TileType.WATER;
            }
        }
    }
}
