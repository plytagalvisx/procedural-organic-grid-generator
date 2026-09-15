using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace WFC
{
    [Serializable]
    public class TilePattern
    {
        public string name;
        public TileType type;   // e.g. LAND
        public int size;        // number of tiles in group
    }

    public class WaveFunctionCollapse3D : MonoBehaviour
    {
        public int dimensions;
        public Tile[] tileObjects; // for a tile with 4 neighbours (aka 4 sides)
        public List<Cell> GridOfCells;
        public Cell cellObj;
        public Tile backupTile;
        private int iteration;
        private List<Vertex> IrregularGridVertices;
        public Tile selectedTile;


        // Experimenting with WFC and irregular grid:
        public Tile[] tileObjectsForTriangle; // 3 sides
        public Tile[] tileObjectsForQuad; // 4 sides
        public Tile[] tileObjectsForPentagon; // 5 sides
        public Tile[] tileObjectsForHexagon; // 6 sides

        public Cell TargetCell;

        // Store the tile types for each vertex in the irregular grid. This will allow us to visualize the WFC output on the irregular grid by mapping the collapsed tile types to their corresponding vertices.
        public Dictionary<Vertex, TileType> vertexTileTypes = new Dictionary<Vertex, TileType>(); // state layer 
        public Dictionary<Vertex, Cell> vertexToCell = new Dictionary<Vertex, Cell>();


        // void Awake()
        // {
        //     GridOfCells = new List<Cell>();
        //     InitializeCellGrid();
        // }

        // public void InitializeCellGrid()
        // {
        //     for (int y = 0; y < dimensions; y++)
        //     {
        //         for (int x = 0; x < dimensions; x++)
        //         {
        //             Transform CellsGameObjectTransform = this.transform.GetChild(0);
        //             Cell newCell = Instantiate(cellObj, new Vector3(x, 0, y), Quaternion.identity, CellsGameObjectTransform);
        //             newCell.CreateCell(false, tileObjects);
        //             GridOfCells.Add(newCell);
        //         }
        //     }

        //     StartCoroutine(CheckEntropy());
        // }

        public void InitializeCellGrid(List<Vertex> gridVertices)
        {
            IrregularGridVertices = gridVertices;
            GridOfCells = new List<Cell>();
            foreach (Vertex vertex in IrregularGridVertices)
            {
                if (vertex.IsBoundary) continue;

                Transform CellsGameObjectTransform = this.transform.GetChild(0);
                Cell newCell = Instantiate(cellObj, new Vector3(vertex.point.x, 0, vertex.point.z), Quaternion.identity, CellsGameObjectTransform);

                if (vertex.Edges.Count == 3) // if the cell is a triangle shape (vertex represents triangle's center point)
                {
                    newCell.CreateCell(false, tileObjectsForTriangle, 3, vertex.point);
                }
                if (vertex.Edges.Count == 4) // if the cell is a quad/square shape (vertex represents quad's center point)
                {
                    newCell.CreateCell(false, tileObjectsForQuad, 4, vertex.point);
                }
                if (vertex.Edges.Count == 5) // if the cell is a pentagon shape (vertex represents pentagon's center point)
                {
                    newCell.CreateCell(false, tileObjectsForPentagon, 5, vertex.point);
                }
                if (vertex.Edges.Count == 6) // if the cell is a hexagon shape (vertex represents hexagon's center point)
                {
                    newCell.CreateCell(false, tileObjectsForHexagon, 6, vertex.point);
                }

                GridOfCells.Add(newCell);
                vertexToCell[vertex] = newCell; // to map each vertex to its corresponding cell (since the vertex represents the center point of the cell)
            }

            foreach (Vertex vertex in IrregularGridVertices)
            {
                if (vertex.IsBoundary) continue;

                Cell cell = vertexToCell[vertex];

                for (int i = 0; i < vertex.Edges.Count; i++)
                {
                    Edge edge = vertex.Edges[i];
                    Vertex neighborVertex = edge.GetOtherVertex(vertex);

                    if (neighborVertex.IsBoundary) continue;

                    if (vertexToCell.TryGetValue(neighborVertex, out Cell neighborCell))
                    {
                        if (!cell.Neighbors.Contains(neighborCell))
                        {
                            cell.Neighbors.Add(neighborCell);
                            cell.NeighbourIndices.Add(i);
                        }
                    }
                }
            }

            // Build neighbor connections
            // foreach (Vertex vertex in IrregularGridVertices)
            // {
            //     if (vertex.IsBoundary) continue;

            //     Cell cell = vertexToCell[vertex];

            //     foreach (Edge edge in vertex.Edges)
            //     {
            //         Vertex neighborVertex = edge.GetOtherVertex(vertex);

            //         if (neighborVertex.IsBoundary) continue;

            //         if (vertexToCell.TryGetValue(neighborVertex, out Cell neighborCell))
            //         {
            //             if (!cell.Neighbors.Contains(neighborCell))
            //             {
            //                 cell.Neighbors.Add(neighborCell);
            //                 cell.NeighbourIndices.Add(vertex.Edges.IndexOf(edge)); 
            //             }
            //         }
            //     }
            // }

            StartCoroutine(CheckEntropy());
        }

        public void SetTargetCell(Vertex targetVertex, float targetY)
        {
            Debug.Log("Target vertex: " + targetVertex.point);
            foreach (Cell cell in GridOfCells)
            {
                if (cell.Position == targetVertex.point)
                {
                    TargetCell = cell;
                    break;
                }
            }
        }

        public IEnumerator CheckEntropy()
        {
            List<Cell> tempGridOfCells = new List<Cell>(GridOfCells);

            tempGridOfCells.RemoveAll(cell => cell.collapsed); // get all cells that are not collapsed

            // foreach (Cell tempCell in tempGridOfCells)
            // {
            //     if (tempCell.NeighbourCount == 3) // if triangle shape
            //     {                    
            //     }
            //     if (tempCell.NeighbourCount == 4) // if quad/square shape
            //     {
            //     }
            //     if (tempCell.NeighbourCount == 5) // if pentagon shape
            //     {
            //     }
            //     if (tempCell.NeighbourCount == 6) // if hexagon shape
            //     {
            //     }
            // }

            tempGridOfCells.Sort((a, b) => a.tileOptions.Length - b.tileOptions.Length); // sort by entropy (aka number of tile options per cell) in ascending order (from lowest to highest)

            int minEntropy = tempGridOfCells[0].tileOptions.Length;

            tempGridOfCells.RemoveAll(cell => cell.tileOptions.Length != minEntropy); // get the minimum entropy (starting from the first cell since it has the lowest number of tile options). Then get all cells that have the same entropy as the minimum entropy

            // Alternative:
            // List<Cell> tempGrid = GridOfCells.Where(c => !c.collapsed).OrderBy(c => c.tileOptions.Length).ToList();

            // int stopIndex = default;
            // for (int i = 1; i < tempGridOfCells.Count; i++)
            // {
            //     if (tempGridOfCells[i].tileOptions.Length > minEntropy) // if any cell tile options size > first cell tile options size (aka minimum entropy), we stop the loop since we only want to get the cells with the lowest entropy (the same number of tile options as the minimum entropy)
            //     {
            //         stopIndex = i;
            //         break;
            //     }
            // }

            // if (stopIndex > 0)
            // {
            //     tempGridOfCells.RemoveRange(stopIndex, tempGridOfCells.Count - stopIndex); // remove all cells that have more tile options than the minimum entropy
            // }

            yield return new WaitForSeconds(0.01f); // 25f);
            CollapseCell(tempGridOfCells);
        }

        public void CollapseCell()
        { // pass:
          // TargetCell.collapsed = true;

            // try
            // {
            //     Debug.Log("Target cell tile selected");
            //     Tile selectedTile = TargetCell.tileOptions[UnityEngine.Random.Range(0, TargetCell.tileOptions.Length)];
            //     TargetCell.tileOptions = new Tile[] { selectedTile };
            // }
            // catch
            // {
            //     Debug.Log("Backup tile selected");
            //     Tile selectedTile = backupTile;
            //     TargetCell.tileOptions = new Tile[] { selectedTile };
            // }

            // Tile selectedTileToPlace = TargetCell.tileOptions[0];
        }

        public void CollapseCell(List<Cell> tempGridOfCells) // to collapse the cells with the lowest entropy we have to choose a random cell from the list of cells with the lowest entropy
        {
            int randomCellIndex = UnityEngine.Random.Range(0, tempGridOfCells.Count);
            Cell cellToCollapse = tempGridOfCells[randomCellIndex];
            cellToCollapse.collapsed = true;

            try
            {
                Debug.Log("Random target cell tile selected");
                Tile selectedTile = cellToCollapse.tileOptions[UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length)];
                cellToCollapse.tileOptions = new Tile[] { selectedTile };
            }
            catch
            {
                Debug.Log("Backup tile selected");
                Tile selectedTile = backupTile;
                cellToCollapse.tileOptions = new Tile[] { selectedTile };
            }

            Tile selectedTileToPlace = cellToCollapse.tileOptions[0];
            // selectedTile = selectedTileToPlace;

            // selectedTileToPlace.GetComponent<MeshFilter>().mesh = selectedTileToPlace.GetComponent<MeshFilter>().sharedMesh; // to update the mesh of the tile to place according to the selected tile option (in case the tile options are different from each other)
            Transform TilesGameObjectTransform = this.transform.GetChild(1);
            Instantiate(selectedTileToPlace, cellToCollapse.transform.position, selectedTileToPlace.transform.rotation, TilesGameObjectTransform);

            // blockSlotGameObject.GetComponent<MeshFilter>().mesh = mesh;
            UpdateGeneration(); // wave propagation

        }

        // public void CollapseCell2() // to collapse the cells with the lowest entropy we have to choose a random cell from the list of cells with the lowest entropy
        // {
        //     List<Cell> tempGridOfCells = new List<Cell>(GridOfCells);
        //     tempGridOfCells.RemoveAll(cell => cell.collapsed); // get all cells that are not collapsed
        //     // tempGridOfCells.Sort((c1, c2) => c1.tileOptions.Length - c2.tileOptions.Length); // sort by entropy (aka number of tile options per cell) in ascending order (from lowest to highest)
        //     // int minEntropy = tempGridOfCells[0].tileOptions.Length;
        //     // tempGridOfCells.RemoveAll(cell => cell.tileOptions.Length != minEntropy); // get the minimum entropy (starting from the first cell since it has the lowest number of tile options). Then get all cells that have the same entropy as the minimum entropy

        //     Debug.Log("TargetCell: " + TargetCell.Position);
        //     // int randomCellIndex = UnityEngine.Random.Range(0, tempGridOfCells.Count);
        //     // Cell cellToCollapse = tempGridOfCells[randomCellIndex];

        //     Cell cellToCollapse = TargetCell;
        //     cellToCollapse.collapsed = true;

        //     try
        //     {
        //         Debug.Log("Target cell tile selected");
        //         Tile selectedTile = cellToCollapse.tileOptions[UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length)];
        //         cellToCollapse.tileOptions = new Tile[] { selectedTile };
        //     }
        //     catch
        //     {
        //         Debug.Log("Backup tile selected");
        //         Tile selectedTile = backupTile;
        //         cellToCollapse.tileOptions = new Tile[] { selectedTile };
        //     }

        //     Tile selectedTileToPlace = cellToCollapse.tileOptions[0];
        //     selectedTile = selectedTileToPlace;


        //     // NEW:
        //     TileType type = selectedTileToPlace.tileType;

        //     // Map to vertex
        //     Vertex vertex = IrregularGridVertices.First(v => Vector3.Distance(v.point, cellToCollapse.Position) < 0.001f); // (v => v.point == cellToCollapse.Position);
        //     vertexTileTypes[vertex] = type;

        //     // selectedTileToPlace.GetComponent<MeshFilter>().mesh = 
        //     // Transform TilesGameObjectTransform = this.transform.GetChild(1);
        //     // Instantiate(selectedTileToPlace, cellToCollapse.transform.position, selectedTileToPlace.transform.rotation, TilesGameObjectTransform);

        //     // blockSlotGameObject.GetComponent<MeshFilter>().mesh = mesh;
        //     // UpdateGeneration(); // wave propagation

        // }

        public Tile GetSelectedTile()
        {
            return selectedTile;
        }

        public TileType GetSelectedTileType()
        {
            return selectedTile.tileType;
        }

        public void UpdateGeneration2() // aka Propagate
        {
            List<Cell> newGenerationCell = new List<Cell>(GridOfCells);

            for (int y = 0; y < dimensions; y++)
            {
                for (int x = 0; x < dimensions; x++)
                {
                    int currentCellIndex = x + y * dimensions;
                    bool isCurrentCellCollapsed = GridOfCells[currentCellIndex].collapsed;

                    if (!isCurrentCellCollapsed)
                    {
                        List<Tile> validTileOptions = GetValidTileOptionsBasedOnNeighbours(x, y);  // aka possible tiles. This list starts with all possible tiles but will be reduced as we check each neighbor cell.
                        newGenerationCell[currentCellIndex].UpdateCellTileOptions(validTileOptions.ToArray());
                    }
                }
            }

            GridOfCells = newGenerationCell;
            iteration++;

            if (iteration < dimensions * dimensions) // (dimension * dimension) indicates the number of cells in the grid
            {
                StartCoroutine(CheckEntropy());
            }
        }

        public void UpdateGeneration3() // aka Propagate
        {
            List<Cell> newGenerationCell = new List<Cell>(GridOfCells);

            foreach (Vertex vertex in IrregularGridVertices)
            {
                int currentCellIndex = (int)vertex.point.x + (int)vertex.point.z * dimensions;
                bool isCurrentCellCollapsed = GridOfCells[currentCellIndex].collapsed;

                if (!isCurrentCellCollapsed)
                {
                    List<Tile> validTileOptions = GetValidTileOptionsBasedOnNeighbours((int)vertex.point.x, (int)vertex.point.z);  // aka possible tiles. This list starts with all possible tiles but will be reduced as we check each neighbor cell.
                    newGenerationCell[currentCellIndex].UpdateCellTileOptions(validTileOptions.ToArray());
                }
            }

            GridOfCells = newGenerationCell;
            iteration++;

            if (iteration < dimensions * dimensions)
            {
                StartCoroutine(CheckEntropy());
                // check entropy:
                // List<Cell> tempGridOfCells = new List<Cell>(GridOfCells);
                // tempGridOfCells.RemoveAll(cell => cell.collapsed); // get all cells that are not collapsed
                // tempGridOfCells.Sort((c1, c2) => c1.tileOptions.Length - c2.tileOptions.Length); // sort by entropy (aka number of tile options per cell) in ascending order (from lowest to highest)
                // int minEntropy = tempGridOfCells[0].tileOptions.Length;
                // tempGridOfCells.RemoveAll(cell => cell.tileOptions.Length != minEntropy); // get the minimum entropy (starting from the first cell since it has the lowest number of tile options). Then get all cells that have the same entropy as the minimum entropy (we will collapse one of these cells in the next iteration).
            }
        }

        // public void UpdateGeneration()
        // {
        //     Queue<Cell> queue = new Queue<Cell>();

        //     // Start from the collapsed cell
        //     queue.Enqueue(GridOfCells.First(cell => cell.collapsed));

        //     while (queue.Count > 0)
        //     {
        //         Cell current = queue.Dequeue();

        //         foreach (Cell neighbor in current.Neighbors)
        //         {
        //             if (neighbor.collapsed) continue;

        //             int before = neighbor.tileOptions.Length;

        //             List<Tile> validOptions = GetValidOptionsFromNeighbours(neighbor);

        //             if (validOptions.Count == 0)
        //             {
        //                 validOptions.Add(backupTile);
        //             }

        //             neighbor.UpdateCellTileOptions(validOptions.ToArray());

        //             int after = neighbor.tileOptions.Length;

        //             // If reduced → propagate further
        //             if (after < before)
        //             {
        //                 queue.Enqueue(neighbor);
        //             }
        //         }
        //     }
        // }

        // private List<Tile> GetValidOptionsFromNeighbours(Cell cell)
        // {
        //     List<Tile> validOptions = new List<Tile>(cell.tileOptions);

        //     foreach (Cell neighbor in cell.Neighbors)
        //     {
        //         List<Tile> allowed = new List<Tile>();

        //         foreach (Tile neighborTile in neighbor.tileOptions)
        //         {
        //             allowed.AddRange(GetAllValidNeighbours(neighborTile));
        //         }

        //         validOptions = validOptions
        //             .Where(tile => allowed.Contains(tile))
        //             .ToList();
        //     }

        //     return validOptions;
        // }

        // private Tile[] GetAllValidNeighbours(Tile tile)
        // {
        //     return tile.upNeighbours
        //         .Concat(tile.downNeighbours)
        //         .Concat(tile.leftNeighbours)
        //         .Concat(tile.rightNeighbours)
        //         .Distinct()
        //         .ToArray();
        // }

        public void UpdateGeneration()
        {
            List<Cell> newGeneration = new List<Cell>(GridOfCells);

            foreach (Cell cell in GridOfCells)
            {
                if (cell.collapsed) continue;

                List<Tile> validOptions = GetValidOptions(cell);

                newGeneration[GridOfCells.IndexOf(cell)]
                    .UpdateCellTileOptions(validOptions.ToArray());
            }

            GridOfCells = newGeneration;
            iteration++;

            if (iteration < GridOfCells.Count)
            {
                StartCoroutine(CheckEntropy());
            }
        }

        private List<Tile> GetValidOptions(Cell cell)
        {
            List<Tile> possibleTileOptions = new List<Tile>(cell.tileOptions);

            for (int i = 0; i < cell.Neighbors.Count; i++)
            {
                Cell neighbor = cell.Neighbors[i];
                int edgeIndex = cell.NeighbourIndices[i];

                List<Tile> validFromNeighbor = new List<Tile>();

                foreach (Tile neighborTile in neighbor.tileOptions)
                {
                    if (neighborTile.neighboursByEdge.Count > edgeIndex)
                    {
                        // validFromNeighbor.AddRange(
                        //     neighborTile.neighboursByEdge[edgeIndex]
                        // );
                        validFromNeighbor.AddRange(
                            neighborTile.neighboursByEdge[edgeIndex].neighbours
                        );
                    }
                }

                FilterValidTiles(possibleTileOptions, validFromNeighbor);
            }

            return possibleTileOptions;
        }

        private void FilterValidTiles(List<Tile> possible, List<Tile> valid)
        {
            for (int i = possible.Count - 1; i >= 0; i--)
            {
                if (!valid.Contains(possible[i]))
                {
                    possible.RemoveAt(i);
                }
            }
        }





        // This method is responsible for adjusting the list of potential/possible tiles/nodes
        // for a specific cell based on the possible configurations (valid compatible tiles) of its neighbors
        public List<Tile> GetValidTileOptionsBasedOnNeighbours(int x, int y)
        {
            List<Tile> possibleTileOptions = new List<Tile>(tileObjects);

            CheckNeighbourValidity(x, y - 1, possibleTileOptions, tile => tile.downNeighbours);  // Up
            CheckNeighbourValidity(x + 1, y, possibleTileOptions, tile => tile.rightNeighbours); // Right
            CheckNeighbourValidity(x, y + 1, possibleTileOptions, tile => tile.upNeighbours);    // Down
            CheckNeighbourValidity(x - 1, y, possibleTileOptions, tile => tile.leftNeighbours);  // Left

            return possibleTileOptions;
        }

        public void CheckNeighbourValidity(int x, int y, List<Tile> possibleTileOptions, Func<Tile, Tile[]> getNeighborsOptions)
        {
            if (IsInsideGrid(x, y))
            {
                Cell neighbour = GridOfCells[x + y * dimensions]; // neighbour can be collapsed or not collapsed?
                List<Tile> validNeighbourTileOptions = new List<Tile>();

                // Collect valid neighboring tiles:
                foreach (Tile tile in neighbour.tileOptions) // neighbour.tileOptions = neighbour's potential nodes
                {
                    Tile[] validNeighbours = getNeighborsOptions(tile); // a function that retrieves valid neighbors for a given potential/possible tile.
                    validNeighbourTileOptions.AddRange(validNeighbours);
                }

                CheckTileValidity(possibleTileOptions, validNeighbourTileOptions);
            }
        }

        private bool IsInsideGrid(int x, int y)
        {
            return x >= 0 && x < dimensions && y >= 0 && y < dimensions;
        }

        private void CheckTileValidity(List<Tile> tileOptionList, List<Tile> validTiles)
        {
            for (int i = tileOptionList.Count - 1; i >= 0; i--)
            {
                Tile potentialTile = tileOptionList[i];
                if (!validTiles.Contains(potentialTile))
                {
                    tileOptionList.RemoveAt(i);
                }
            }
        }
    }
}