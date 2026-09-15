// using System;
// using System.Collections;
// using System.Collections.Generic;
// // using System.Linq;
// // using Unity.Mathematics;
// using UnityEngine;

// namespace WFC
// {
//     public class WaveFunctionCollapse2D : MonoBehaviour
//     {
//         public int dimensions;
//         public Tile[] tileNodes;
//         public List<Cell> GridOfCells;
//         public Cell cellPrefab;

//         int iterations = 0;

//         void Awake()
//         {
//             GridOfCells = new List<Cell>();
//             InitializeGrid();
//         }

//         void InitializeGrid()
//         {
//             for (int y = 0; y < dimensions; y++)
//             {
//                 for (int x = 0; x < dimensions; x++)
//                 {
//                     Cell newCell = Instantiate(cellPrefab, new Vector2(x, y), Quaternion.identity, this.transform);
//                     newCell.CreateCell(false, tileNodes, 4);
//                     GridOfCells.Add(newCell);
//                 }
//             }

//             StartCoroutine(CheckEntropy());
//         }

//         IEnumerator CheckEntropy()
//         {
//             List<Cell> tempGridOfCells = GridOfCells.FindAll(cell => !cell.collapsed); // get all cells that are not collapsed
//             tempGridOfCells.Sort((a, b) => a.tileOptions.Length.CompareTo(b.tileOptions.Length)); // sort by entropy (aka number of tile options per cell) in ascending order (from lowest to highest)

//             int minEntropy = tempGridOfCells[0].tileOptions.Length; // get the minimum entropy (starting from the first cell since it has the lowest number of tile options)
//             tempGridOfCells = tempGridOfCells.FindAll(cell => cell.tileOptions.Length == minEntropy); // get all cells that have the same entropy as the minimum entropy
//             yield return new WaitForSeconds(0.01f);
//             CollapseCell(tempGridOfCells);
//         }

//         // IEnumerator CheckEntropy()
//         // {
//         //     List<Cell> tempGrid = new List<Cell>(GridOfCells);
//         //     tempGrid.RemoveAll(c => c.collapsed);
//         //     tempGrid.Sort((a, b) => { return a.tileOptions.Length - b.tileOptions.Length; });

//         //     int minEntropy = tempGrid[0].tileOptions.Length;
//         //     int stopIndex = default;

//         //     for (int i = 1; i < tempGrid.Count; i++)
//         //     {
//         //         if (tempGrid[i].tileOptions.Length > minEntropy)
//         //         {
//         //             stopIndex = i;
//         //             break;
//         //         }
//         //     }

//         //     if (stopIndex > 0)
//         //     {
//         //         tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);
//         //     }

//         //     yield return new WaitForSeconds(0.01f);
//         //     CollapseCell(tempGrid);
//         // }

//         void CollapseCell(List<Cell> tempGridOfCells) // to collapse the cells with the lowest entropy we have to choose a random cell from the list of cells with the lowest entropy
//         {
//             int randomCellIndex = UnityEngine.Random.Range(0, tempGridOfCells.Count);
//             Cell cellToCollapse = tempGridOfCells[randomCellIndex];
//             Tile selectedTileToPlace = cellToCollapse.tileOptions[UnityEngine.Random.Range(0, cellToCollapse.tileOptions.Length)];

//             cellToCollapse.collapsed = true;
//             cellToCollapse.tileOptions = new Tile[] { selectedTileToPlace };

//             Instantiate(selectedTileToPlace, cellToCollapse.transform.position, Quaternion.identity);
//             UpdateGeneration(); // wave propagation
//         }

//         void UpdateGeneration() // aka Propagate
//         {
//             List<Cell> newGenerationCells = new List<Cell>(GridOfCells);

//             for (int y = 0; y < dimensions; y++)
//             {
//                 for (int x = 0; x < dimensions; x++)
//                 {
//                     int currentCellIndex = x + y * dimensions;
//                     bool isCurrentCellCollapsed = GridOfCells[currentCellIndex].collapsed;

//                     if (isCurrentCellCollapsed)
//                     {
//                         newGenerationCells[currentCellIndex] = GridOfCells[currentCellIndex];
//                     }
//                     else
//                     {
//                         List<Tile> potentialNodes = new List<Tile>(tileNodes); // aka possible tiles. This list starts with all possible tiles but will be reduced as we check each neighbor cell.
//                         UpdateTileOptionsBasedOnNeighbours(x, y, potentialNodes);
//                         newGenerationCells[currentCellIndex].UpdateCellTileOptions(potentialNodes.ToArray());
//                     }
//                 }
//             }

//             GridOfCells = newGenerationCells;
//             iterations++;

//             if (iterations < dimensions * dimensions)
//             {
//                 StartCoroutine(CheckEntropy());
//             }
//         }

//         void UpdateTileOptionsBasedOnNeighbours(int x, int y, List<Tile> potentialNodes)
//         {
//             CheckNeighbourValidity(x, y - 1, tile => tile.upNeighbours, potentialNodes); // Up
//             CheckNeighbourValidity(x + 1, y, tile => tile.leftNeighbours, potentialNodes); // Right
//             CheckNeighbourValidity(x, y + 1, tile => tile.downNeighbours, potentialNodes); // Down
//             CheckNeighbourValidity(x - 1, y, tile => tile.rightNeighbours, potentialNodes); // Left
//         }


//         // This method is responsible for adjusting the list of potential tiles nodes
//         // for a specific cell based on the possible configurations of its neighbors
//         void CheckNeighbourValidity(int x, int y, Func<Tile, Tile[]> getValidNeighbours, List<Tile> potentialNodes)
//         {
//             if (IsInsideGrid(x, y))
//             {
//                 Cell neighbour = GridOfCells[x + y * dimensions]; // neighbour can be collapsed or not collapsed?
//                 List<Tile> validNodes = new List<Tile>();

//                 foreach (Tile tile in neighbour.tileOptions) // neighbour.tileOptions = neighbour's potential nodes
//                 {
//                     Tile[] validNeighbours = getValidNeighbours(tile); // a function that retrieves valid neighbors for a given potential/possible tile.
//                     validNodes.AddRange(validNeighbours);
//                 }

//                 CheckValidity(potentialNodes, validNodes);
//             }
//         }

//         private bool IsInsideGrid(int x, int y)
//         {
//             return x >= 0 && x < dimensions && y >= 0 && y < dimensions;
//         }

//         // void UpdateGeneration()
//         // {
//         //     List<Cell> newGenerationCell = new List<Cell>(gridComponents);

//         //     for (int y = 0; y < dimensions; y++)
//         //     {
//         //         for (int x = 0; x < dimensions; x++)
//         //         {
//         //             var currentCellindex = x + y * dimensions; // current cell index
//         //             if (gridComponents[currentCellindex].collapsed)
//         //             {
//         //                 // Debug.Log("called");
//         //                 newGenerationCell[currentCellindex] = gridComponents[currentCellindex];
//         //             }
//         //             else
//         //             {
//         //                 List<Tile> options = new List<Tile>();
//         //                 foreach (Tile t in tileObjects)
//         //                 {
//         //                     options.Add(t);
//         //                 }

//         //                 //update above
//         //                 if (y > 0)
//         //                 {
//         //                     Cell up = gridComponents[x + (y - 1) * dimensions];
//         //                     List<Tile> validOptions = new List<Tile>();

//         //                     foreach (Tile possibleOptions in up.tileOptions)
//         //                     {
//         //                         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
//         //                         // var valid = possibleOptions.upNeighbours;
//         //                         var valid = tileObjects[valOption].upNeighbours;

//         //                         validOptions = validOptions.Concat(valid).ToList();
//         //                     }

//         //                     CheckValidity(options, validOptions);
//         //                 }

//         //                 //update right
//         //                 if (x < dimensions - 1)
//         //                 {
//         //                     Cell right = gridComponents[x + 1 + y * dimensions];
//         //                     List<Tile> validOptions = new List<Tile>();

//         //                     foreach (Tile possibleOptions in right.tileOptions)
//         //                     {
//         //                         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
//         //                         var valid = tileObjects[valOption].leftNeighbours;

//         //                         validOptions = validOptions.Concat(valid).ToList();
//         //                     }

//         //                     CheckValidity(options, validOptions);
//         //                 }

//         //                 //look down
//         //                 if (y < dimensions - 1)
//         //                 {
//         //                     Cell down = gridComponents[x + (y + 1) * dimensions];
//         //                     List<Tile> validOptions = new List<Tile>();

//         //                     foreach (Tile possibleOptions in down.tileOptions)
//         //                     {
//         //                         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
//         //                         var valid = tileObjects[valOption].downNeighbours;

//         //                         validOptions = validOptions.Concat(valid).ToList();
//         //                     }

//         //                     CheckValidity(options, validOptions);
//         //                 }

//         //                 //look left
//         //                 if (x > 0)
//         //                 {
//         //                     Cell left = gridComponents[x - 1 + y * dimensions];
//         //                     List<Tile> validOptions = new List<Tile>();

//         //                     foreach (Tile possibleOptions in left.tileOptions)
//         //                     {
//         //                         var valOption = Array.FindIndex(tileObjects, obj => obj == possibleOptions);
//         //                         var valid = tileObjects[valOption].rightNeighbours;

//         //                         validOptions = validOptions.Concat(valid).ToList();
//         //                     }

//         //                     CheckValidity(options, validOptions);
//         //                 }

//         //                 Tile[] newTileList = new Tile[options.Count];

//         //                 for (int i = 0; i < options.Count; i++)
//         //                 {
//         //                     newTileList[i] = options[i];
//         //                 }

//         //                 newGenerationCell[currentCellindex].RecreateCell(newTileList);
//         //             }
//         //         }
//         //     }

//         //     gridComponents = newGenerationCell;
//         //     iterations++;

//         //     if (iterations < dimensions * dimensions)
//         //     {
//         //         StartCoroutine(CheckEntropy());
//         //     }

//         // }

//         void CheckValidity(List<Tile> potentialNodes, List<Tile> validNodes)
//         {
//             for (int i = potentialNodes.Count - 1; i >= 0; i--)
//             {
//                 Tile potentialNode = potentialNodes[i];
//                 if (!validNodes.Contains(potentialNode))
//                 {
//                     potentialNodes.RemoveAt(i);
//                 }
//             }
//         }
//     }
// }