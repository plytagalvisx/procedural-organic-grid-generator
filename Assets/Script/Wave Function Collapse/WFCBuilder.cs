using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class WFCBuilder : MonoBehaviour
{
    // The size of our world in grid cells
    [SerializeField] private int Width;
    [SerializeField] private int Height;

    // A 2D array that will store our collapsed tiles so we can reference them later
    private WFCNode[,] Grid;
    // A list containing all of our possible nodes
    public List<WFCNode> Nodes = new List<WFCNode>();
    // A list to store tile positions that need collapsing
    private List<Vector2Int> TilesToCollapse = new List<Vector2Int>();
    // An array of offsets to make it easier to check neighbours without using duplicate code
    private Vector2Int[] NeighbourOffsets = new Vector2Int[]
    {
        new Vector2Int(0, 1),  // Top
        new Vector2Int(0, -1), // Bottom
        new Vector2Int(1, 0),  // Right
        new Vector2Int(-1, 0)  // Left
    };

    private void Start()
    {
        Grid = new WFCNode[Width, Height];

        CollapseWorld();
    }

    private void CollapseWorld()
    {
        // Make sure we're starting fresh with our TilesToCollapse list
        TilesToCollapse.Clear();

        // Vector2Int randomTile = new Vector2Int(Random.Range(0, Width), Random.Range(0, Height));
        Vector2Int middleTile = new Vector2Int(Width / 2, Height / 2);
        // Our origin node to kick things off. Just put it roughly in the middle of the grid
        TilesToCollapse.Add(middleTile);

        while (TilesToCollapse.Count > 0)
        {
            // Select the first tile/node in the list to collapse:
            int x = TilesToCollapse[0].x;
            int y = TilesToCollapse[0].y;

            // By default, the potential nodes include every possible node. So, start with a list populated with all of the nodes
            List<WFCNode> potentialNodes = new List<WFCNode>(Nodes);

            // Loop through each neighbour of this node
            for (int i = 0; i < NeighbourOffsets.Length; i++)
            {
                // Node position + offset = neighbour position
                Vector2Int neighbour = new Vector2Int(x + NeighbourOffsets[i].x, y + NeighbourOffsets[i].y);

                // Check if the neighbour is within the bounds of the grid
                if (IsInsideGrid(neighbour))
                {
                    // Get the node at this neighbour
                    // Store the current neighbour for easier code
                    WFCNode neighbourNode = Grid[neighbour.x, neighbour.y];

                    // If the neighbour node/cell is not null (meaning it has been already collapsed) (aka the neighbour already exists/has a node assigned to it)
                    // we need to factor it into the potential nodes for this cell.
                    // Collapse means that the node has been assigned to the cell/tile, so it's not null anymore. (?)
                    // Whittle means to get rid off the nodes/cells/tiles that are not compatible with the neighbour node. (?)
                    if (neighbourNode != null)
                    {
                        // Depending on which direction we are checking, whittle down the list of potential nodes according
                        // to the valid nodes in our corresponding neighbour (top to bottom, left to right, vice versa)
                        switch (i)
                        {
                            case 0:
                                WhittleNodes(potentialNodes, neighbourNode.Bottom.CompatibleNodes);
                                break;
                            case 1:
                                WhittleNodes(potentialNodes, neighbourNode.Top.CompatibleNodes);
                                break;
                            case 2:
                                WhittleNodes(potentialNodes, neighbourNode.Left.CompatibleNodes);
                                break;
                            case 3:
                                WhittleNodes(potentialNodes, neighbourNode.Right.CompatibleNodes);
                                break;
                        }
                    }
                    // If the neighbouring cell IS null, we don't need to factor it in here, but 
                    // we do need to add it to the TilesToCollapse list and we'll repeat this process 
                    // for THAT cell next time around.
                    else
                    {
                        if (!TilesToCollapse.Contains(neighbour))
                        {
                            TilesToCollapse.Add(neighbour);
                        }
                    }
                }
            }

            // Now we have whittled our list down to only nodes that are compatible with all 
            // existing neighbours, we can collapse this wave!

            // If we don't have any potential nodes, use the blank node (placed in the first
            // position of the Nodes array) and give the user a debug warning.
            if (potentialNodes.Count < 1)
            {
                Grid[x, y] = Nodes[0];
                Debug.LogWarning("Attempted to collapse wave on " + x + ", " + y + " but found no compatible nodes.");
                // In the event that this happens, you could either create more tiles that would
                // be valid in this situation, or you can have the algorithm go back (backtracing?) and look at 
                // the neighbour tiles to find a combination of nodes that satisfies all of the
                // surrounding tiles.
            }
            // Else we can pick one of our potential nodes at random. This is the "collapse" part of the algorithm:
            else
            {
                Grid[x, y] = potentialNodes[Random.Range(0, potentialNodes.Count)];
            }

            float tileSize = 2f; // Assuming each tile is 1x1 units. Adjust this value if your tiles are larger.
            // Adjust the tile placement by multiplying x and y with the tile size
            // Vector3 position = new Vector3(x * tileSize, y * tileSize, 0f);
            // GameObject newNode = Instantiate(Grid[x, y].Prefab, position, Quaternion.identity, this.transform);

            GameObject newNode = Instantiate(Grid[x, y].Prefab, new Vector3(x, y, 0f), Quaternion.identity, this.transform);
            newNode.name = "Node " + x + ", " + y;

            // Remove the node we've just collapsed from the TilesToCollapse list so we don't
            // try to collapse it again.
            TilesToCollapse.RemoveAt(0);
        }
    }

    private void WhittleNodes(List<WFCNode> potentialNodes, List<WFCNode> validNodes)
    {
        for (int i = potentialNodes.Count - 1; i > -1; i--)
        {
            if (!validNodes.Contains(potentialNodes[i]))
            {
                potentialNodes.RemoveAt(i);
            }
        }
    }

    private bool IsInsideGrid(Vector2Int position)
    {
        return position.x > -1 && position.x < Width && position.y > -1 && position.y < Height;
    }
}