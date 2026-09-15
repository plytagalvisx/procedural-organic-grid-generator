using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SystemRandom = System.Random;
using Random = UnityEngine.Random;
using UnityEditor;
using MC;
using WFC;
using TilePlacement;
using MeshModification;
using Tiles;
using System.IO;

// GridManager aka BuildingGenerator class is responsible for generating the grid data structure and render its subquad lines to display on the Unity environment scene, 
// as well as, creating/updating the block slots, and managing the block module mesh deformation/fitting to the subquad vertices.
// This class utilizes the imported modules and prepares the slots for the block modules to fit the grid structure (i.e. the subquad vertices).
public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    public int ringCount = 6;
    public float ringRadius = 2f;
    public int floor = 6;
    public float height = 1f; // 1f;

    public GameObject lineRendererObject;

    public Grid grid;

    public List<SubQuad> subQuads = new List<SubQuad>();
    public List<Cube> cubes = new List<Cube>();
    public List<CubeVertex> cubeVertices = new List<CubeVertex>();

    public GroundCollider GroundCollider;
    public BlockModuleCollider BlockModuleCollider;

    public MarchingCubes MarchingCubes;
    public WaveFunctionCollapse3D WFC3D;

    public ModuleManager moduleManager;
    public Material blockModuleMaterial;
    public Vertex targetVertex;
    public float targetY;

    public List<Vertex> vertices = new List<Vertex>();



    private const float TILE_LATTICE_DEFAULT_HEIGHT = 2.6f;

    [Serializable]
    public struct PlaceholderTileInfo
    {
        public TileType TileType;
        public List<GameObject> SingleTiles;
        public List<GameObject> DoubleTiles;
        public List<GameObject> TripleTiles;
        public List<GameObject> FullTiles;
        public List<GameObject> OppositeCornersTiles;
    }

    [Header("References: ")]
    [SerializeField] private PlaceholderTileInfo[] _placeholderTileInfos = null;

    private Dictionary<SubQuad, GameObject> subQuadMeshes = new Dictionary<SubQuad, GameObject>();


    private Dictionary<Vertex, TileType> vertexTileTypes = new Dictionary<Vertex, TileType>();

    private void Awake()
    {
        instance = this;
        InitializeGridDataStructures();
        InitializeModuleManager();
        CreateGridSubQuadLines();
        CreateGridGroundCollider();
        // WFC3D.InitializeCellGrid(vertices);
    }

    public void SetTargetVertex(Vertex targetVertex, float targetY)
    {
        this.targetVertex = targetVertex;
        this.targetY = targetY;
    }

    private void InitializeGridDataStructures()
    {
        grid = new Grid(ringCount, ringRadius, floor, height);
        subQuads = grid.SubQuads;
        cubes = grid.Cubes;
        cubeVertices = grid.CubeVertices;
        vertices = grid.Vertices;
    }

    private void InitializeModuleManager()
    {
        moduleManager = Instantiate(moduleManager);
    }

    public void CreateGridSubQuadLines()
    {
        GameObject linesGameObject = new GameObject("SubQuadLines");
        linesGameObject.transform.parent = transform; // GridManager gameobject is the parent of the Lines gameobject

        foreach (SubQuad subQuad in subQuads)
        {
            GameObject lineGameObject = Instantiate(lineRendererObject, linesGameObject.transform); // Creates a Line(Clone) gameobject as a child of the Lines gameobject
            LineRenderer lineRender = lineGameObject.GetComponent<LineRenderer>(); // Each of the Line(Clone) gameobjects gets a LineRenderer component in the inspector

            grid.CreateGridLinesForTheSubQuad(subQuad, lineRender);
        }
    }

    public void CreateGridGroundCollider()
    {
        GroundCollider.CreateGridGroundCollider(grid);
    }

    // private void Update()
    // {
    //     // foreach (Cube cube in cubes) // for each block/module that is clicked/created in the grid
    //     // {
    //     //     cube.UpdateCubeCornerBitValue();
    //     //     if (cube.PrevCubeCornerBitValue != cube.CubeCornerBitValue) // if the index/bitmask of the block module has changed
    //     //     {
    //     //         // We have activated a new block/module in the grid,
    //     //         // by clicking on a target vertex on a subquad corner point in the grid,
    //     //         // thus we update the block slot with the new block data that contains
    //     //         // the activated vertex position and the generated module index of the block module
    //     //         // which is utilized to retrieve the block module mesh/tile from the module manager class
    //     //         // and use it to deform the block module mesh to fit the corresponding subquad vertices.
    //     //         // The block slot is a gameobject/placeholder that contains the re-adjusted/deformed block module mesh.

    //     //         // When we activate a vertex in the grid by clicking on it then each 3, 4 or 5 of 
    //     //         // the neighboring blocks/cubes surrounding the target vertex will contain this activated target vertex. 
    //     //         // Thus, the UpdateBlockSlot() method is called for each of the blocks which is 3, 4 or 5 times 
    //     //         // depending on the subquad face type of the block (triangle, quad or pentagon face type).
    //     //         // We create a block slot for each block module mesh to fit the subquad face's vertices.
    //     //         UpdateBlockSlot(cube);
    //     //         // updatedCube = cube;
    //     //     }
    //     // }
    // }

    // private void UpdateBlockSlot(Cube cube)
    // {
    //     // Uncomment this for marching cubes or WFC module if needed:
    //     // Vector3 subquadCentroid = cube.SubQuad.GetCentroid();
    //     // Debug.Log("cube index: " + grid.Cubes.IndexOf(cube) + ", subquadCentroid: " + subquadCentroid + ", cube centroid: " + cube.centroid);
    //     // foreach (Vector3 subquadVertex in cube.SubQuad.Vertices.Select(x => x.point))
    //     // {
    //     //     Debug.Log("subquadVertex: " + subquadVertex);
    //     // }

    //     // Vector3 targetPoint = targetVertex.point;
    //     // Vector3 cubeCentroidPoint = cube.centroid;
    //     // Vector3 targetDirection = (targetPoint - cubeCentroidPoint).normalized;
    //     // Debug.Log("targetPoint: " + targetPoint);
    //     // Debug.Log("cubeCentroidPoint: " + cubeCentroidPoint);
    //     // Debug.Log("targetDirection: " + targetDirection);

    //     // Debug.Log("--------------------");

    //     string name = $"BlockSlot_{grid.Cubes.IndexOf(cube)}_{cube.centroid.y}"; //_{grid.Vertices.IndexOf(targetVertex)}";
    //     // GridManager gameobject is the parent of the BlockSlot gameobject and 
    //     // the SubQuadLines gameobject, thus the first child of the GridManager gameobject 
    //     // is the BlockSlot gameobject and the second child is the SubQuadLines gameobject.
    //     Transform blockSlotChildTransform = transform.GetChild(0);
    //     GameObject blockSlotChildGameObject = blockSlotChildTransform.Find(name)?.gameObject;

    //     // The "house_web" module import object doesn't have a module index/name/bitmask of 0 or 255, only 1-254.
    //     // cube will contain value 0 if the block module is not activated (thus bitmask not set).

    //     if (cube.CubeCornerBitValue == 0 || cube.CubeCornerBitValue == 255)
    //     {
    //         DestroyBlockSlot(blockSlotChildGameObject);
    //         return;
    //     }

    //     if (blockSlotChildGameObject == null)
    //     {
    //         // Creates a new BlockSlot gameobject.
    //         CreateNewBlockSlot(name, cube);
    //     }
    //     else
    //     {
    //         // If we find an already existing/created BlockSlot gameobject, 
    //         // we update it (by deforming block module's contained mesh) with the new block module data.
    //         UpdateBlockSlot(blockSlotChildGameObject, cube);
    //     }
    // }

    // private void DestroyBlockSlot(GameObject blockSlotGameObject)
    // {
    //     if (blockSlotGameObject != null)
    //     {
    //         Destroy(blockSlotGameObject);
    //         Resources.UnloadUnusedAssets();
    //     }
    // }

    // private void CreateNewBlockSlot(string name, Cube cube)
    // {
    //     Transform blockSlotChildTransform = transform.GetChild(0);
    //     GameObject blockSlotGameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
    //     blockSlotGameObject.transform.SetParent(blockSlotChildTransform);

    //     // Approach 1: Use this with bitmasking module:
    //     // blockSlotGameObject.transform.localPosition = cube.centroid;


    //     // Approach 2: Use this with Marching Cubes module or WFC module:
    //     Vector3 targetPoint = targetVertex.point;
    //     Vector3 cubeCentroidPoint = cube.centroid;
    //     // Direction from the centroid to the target point
    //     Vector3 targetDirection = new Vector3(targetPoint.x - cubeCentroidPoint.x, 0, targetPoint.z - cubeCentroidPoint.z).normalized;
    //     // Define an offset distance towards the target point
    //     float offsetDistance = 0.52f;
    //     // offset blockSlotGameObject.transform.localPosition towards the target vertex from cube centroid:
    //     blockSlotGameObject.transform.localPosition = cubeCentroidPoint + targetDirection * offsetDistance;

    //     UpdateBlockSlot(blockSlotGameObject, cube); // cube/block represents the block module that is clicked/created in the grid
    // }

    // public void UpdateBlockSlot(GameObject blockSlotGameObject, Cube cube)
    // {
    //     //  The thing to observe now is that both of these operations can be implemented as actions on 
    //     //  the vertex buffer and NOT on the index buffer. This means you can do "implicit surface generation" 
    //     //  separately from the "spatial distortion". So you run marching cubes first, get a vertex buffer (vertices) and 
    //     //  an index buffer (triangles), then run through and apply changes to just the vertex buffer to match your spatial distortion. 

    //     // 1. Marching Cubes Block Module Mesh Deformation 
    //     // Uncomment this portion if you want to see the marching cubes mesh deformation to fit the subquad vertices.
    //     MarchingCubes.PopulateCubeMap();
    //     MarchingCubes.MarchCubes();

    //     Mesh mesh = new Mesh();
    //     mesh.vertices = MarchingCubes.vertices.ToArray();
    //     mesh.triangles = MarchingCubes.triangles.ToArray();
    //     blockSlotGameObject.GetComponent<MeshFilter>().mesh = mesh;

    //     DeformBlockModuleMeshToFitTheSubquadVerticesForMarchingCubes(blockSlotGameObject.GetComponent<MeshFilter>().mesh, cube);

    //     blockSlotGameObject.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    //     blockSlotGameObject.GetComponent<MeshFilter>().mesh.RecalculateNormals();
    //     blockSlotGameObject.GetComponent<MeshRenderer>().material = blockModuleMaterial;

    //     // ________________________________________________________________________________________________________________________________________________________________________

    //     // 2. WFC
    //     // Uncomment this portion if you want to see the WFC block module mesh deformation to fit the subquad vertices.
    //     // WFC3D.SetTargetCell(targetVertex, targetY);
    //     // WFC3D.CollapseCell();
    //     // Tile selectedTile = WFC3D.GetSelectedTile();
    //     // Mesh selectedMesh = selectedTile.GetComponent<MeshFilter>().sharedMesh;
    //     // blockSlotGameObject.GetComponent<MeshFilter>().mesh = selectedMesh;

    //     // DeformBlockModuleMeshToFitTheSubquadVerticesForMarchingCubes(blockSlotGameObject.GetComponent<MeshFilter>().mesh, cube);

    //     // blockSlotGameObject.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    //     // blockSlotGameObject.GetComponent<MeshFilter>().mesh.RecalculateNormals();
    //     // blockSlotGameObject.GetComponent<MeshRenderer>().material = blockModuleMaterial;

    //     // ________________________________________________________________________________________________________________________________________________________________________

    //     // 3. Bitmask Block Module Mesh Deformation
    //     // Uncomment this portion if you want to see the bitmask block module mesh deformation to fit the subquad vertices.
    //     // Mesh blockModuleMesh = moduleManager.GetBlockModuleMesh(cube.CubeCornerBitValue);
    //     // blockSlotGameObject.GetComponent<MeshFilter>().mesh = blockModuleMesh;

    //     // DeformBlockModuleMeshToFitTheSubquadVerticesForBitmaskModule(blockSlotGameObject.GetComponent<MeshFilter>().mesh, cube);

    //     // blockSlotGameObject.GetComponent<MeshFilter>().mesh.RecalculateNormals();
    //     // blockSlotGameObject.GetComponent<MeshFilter>().mesh.RecalculateBounds();
    //     // blockSlotGameObject.GetComponent<MeshRenderer>().material = blockModuleMaterial;

    // }

    // // OBS!
    // // Use this for Marching Cubes mesh deformation to fit the subquad vertices, we take the generated marching cubes mesh vertices 
    // // and we deform them to fit the subquad vertices of the corresponding block/cube in the grid.
    // private void DeformBlockModuleMeshToFitTheSubquadVerticesForMarchingCubes(Mesh mesh, Cube cube)
    // {
    //     SubQuad subQuad = cube.SubQuad;
    //     Vector3[] vertices = mesh.vertices;

    //     // Get the bounds of the subquad for scaling and positioning
    //     // Vector3 subQuadSize = new Vector3(
    //     //     Vector3.Distance(subQuad.Vertices[0].point, subQuad.Vertices[1].point), // Width along X axis
    //     //                                                                             // grid.Height,
    //     //     MarchingCubes.height / 2, // Use the height specified in the marching cubes (or block height)
    //     //     Vector3.Distance(subQuad.Vertices[0].point, subQuad.Vertices[3].point)  // Depth along Z axis
    //     // );

    //     for (int i = 0; i < vertices.Length; i++)
    //     {
    //         // Scale vertices to fit within the subquad's size
    //         // We normalized the x, y, and z coordinates of each vertex relative to the marching cube's dimensions. 
    //         // This ensures that the vertices are scaled proportionally to fit within the subquad.
    //         float interpolationFactorX = (vertices[i].x / MarchingCubes.width); // - 0.2f; // Normalize x to [0, 1] based on marching cube width
    //         float interpolationFactorZ = (vertices[i].z / MarchingCubes.width); // - 0.2f; // Normalize z to [0, 1] based on marching cube width
    //         float interpolationFactorY = (vertices[i].y / MarchingCubes.height); // Normalize y to [0, 1] based on marching cube height

    //         // Interpolate along the left and right edges based on the x factor
    //         // The interpolationFactorX and interpolationFactorZ values are now used to interpolate between the edges 
    //         // of the subquad, ensuring that the vertices are placed correctly within the subquad's bounds.
    //         Vector3 leftEdgePoint = grid.LinearInterpolate(subQuad.Vertices[0].point, subQuad.Vertices[3].point, interpolationFactorX);
    //         Vector3 rightEdgePoint = grid.LinearInterpolate(subQuad.Vertices[1].point, subQuad.Vertices[2].point, interpolationFactorX);

    //         // Interpolate between the left and right edges based on the z factor
    //         Vector3 newVertexPosition = grid.LinearInterpolate(leftEdgePoint, rightEdgePoint, interpolationFactorZ);

    //         // Adjust the vertex position vertically based on the normalized Y value and center it using the centroid
    //         newVertexPosition += Vector3.up * interpolationFactorY * (MarchingCubes.height / 2) - subQuad.GetCentroid(); //subQuadSize.y;
    //         // Center the vertex position around the subquad's centroid
    //         // Finally, the vertices are centered around the subquad's centroid, which helps position them correctly within the subquad.
    //         vertices[i] = newVertexPosition - new Vector3(0, 1f, 0);
    //         // vertices[i] = newVertexPosition - targetVertex.point;// + new Vector3(1f, -1f, 1f); // - new Vector3(0, grid.Height * 0.5f, 0);

    //         // Apply the center offset to ensure proper centering
    //         // vertices[i] = newVertexPosition - subQuad.centroid - centerOffset;
    //     }
    //     mesh.vertices = vertices;
    // }

    // // This method here is responsible for altering the vertices of the mesh based on the geometry of a Block object. 
    // // This involves a series of linear interpolations to adjust the positions of the vertices in the mesh, 
    // // effectively deforming it to fit within a specific region or shape (subquad) defined by the Block.
    // // In other words, the resulting grid is made out of similarly-sized quads (4 sided polygons).
    // // This means we can place pre-built modules (square tiles) on them (through we have to deform/skew them a bit to fit the grid).
    // // OBS!
    // // Use this for the Bitmask block module mesh deformation to fit the subquad vertices, we take the block module mesh vertices 
    // // and we deform them to fit the subquad vertices of the corresponding block/cube in the grid.
    // // Also, use this for the WFC block module mesh deformation to fit the subquad vertices, we take the WFC selected tile mesh vertices 
    // // and we deform them to fit the subquad vertices of the corresponding block/cube in the grid.
    // private void DeformBlockModuleMeshToFitTheSubquadVerticesForBitmaskModule(Mesh mesh, Cube cube)
    // {
    //     SubQuad subQuad = cube.SubQuad;
    //     Vector3[] vertices = mesh.vertices;

    //     for (int i = 0; i < vertices.Length; i++)
    //     {
    //         // An equation for the position of each block slot in Unity engine coordinates is given by:
    //         float interpolationFactorX = (vertices[i].x + 0.5f); // we move the mesh vertices to the left or right based on the x factor
    //         float interpolationFactorZ = (vertices[i].z + 0.5f); // we move the mesh vertices up or down based on the z factor

    //         // Interpolate along the left and right edges based on the x factor
    //         Vector3 leftEdgePoint = grid.LinearInterpolate(subQuad.Vertices[0].point, subQuad.Vertices[3].point, interpolationFactorX);
    //         Vector3 rightEdgePoint = grid.LinearInterpolate(subQuad.Vertices[1].point, subQuad.Vertices[2].point, interpolationFactorX);

    //         // Interpolate between the left and right edges based on the z factor
    //         Vector3 newVertexPosition = grid.LinearInterpolate(leftEdgePoint, rightEdgePoint, interpolationFactorZ);

    //         // Adjust the vertex position vertically and center it using the centroid
    //         newVertexPosition += Vector3.up * vertices[i].y * height - subQuad.GetCentroid(); // subQuad.centroid;

    //         // Update the vertex position
    //         vertices[i] = newVertexPosition;
    //     }
    //     mesh.vertices = vertices;
    // }





    // This method handles the event triggered when a block module is added or removed in the grid. It updates the block module collider accordingly,
    public void UpdateBlockModuleCollider(Vertex targetVertex, float targetY, CursorOperation cursorOperation, GameObject currentCursorObject)
    {
        string name = $"BuildingBlock_{grid.Vertices.IndexOf(targetVertex)}_{targetY}";
        Transform blockColliderChildTransform = transform.GetChild(2); // GridManager.instance.transform.GetChild(2);
        GameObject blockColliderChildGameObject = blockColliderChildTransform.Find(name)?.gameObject;

        if (blockColliderChildGameObject == null)
        {
            if (cursorOperation == CursorOperation.LeftClickToAddBlock)
            {
                WFC3D.SetTargetCell(targetVertex, targetY);
                WFC3D.CollapseCell();

                // TileType tileType = WFC3D.GetSelectedTileType();
                // vertexTileTypes[targetVertex] = tileType;

                blockColliderChildGameObject = new GameObject(name, typeof(BlockModuleCollider));
                blockColliderChildGameObject.transform.SetParent(blockColliderChildTransform);
                blockColliderChildGameObject.transform.localPosition = blockColliderChildTransform.position;

                BlockModuleCollider blockModuleCollider = blockColliderChildGameObject.GetComponent<BlockModuleCollider>();
                blockModuleCollider.InitializeGridProperties(); // Initializes grid and blockHeight.

                List<DualGridElement> dualGridElements = blockModuleCollider.AssembleDualGridElements(targetVertex, targetY);
                blockModuleCollider.CreateBlockModuleCollider(dualGridElements);

                // Since UpdateBlockModuleCollider() is called when we add a new grid block module, 
                // we also activate the vertices of the corresponding grid subquads that the added 
                // grid block/cube module occupies.
                // GetCubeVertices() method finds all affected subquads by the target vertex and the y value of the cube module.
                List<CubeVertex> targetCubeVertices = grid.GetCubeVertices(targetVertex, targetY);

                // For Debugging purposes:
                // targetBlockVertices[0].IsActive = true;

                foreach (CubeVertex cubeVertex in targetCubeVertices)
                {
                    cubeVertex.IsActive = true;
                }

                UpdateSubQuadsAroundVertex(targetVertex, targetY);

                // Check all nearby vertices
                // DOESN'T WORK:
                // CheckForBridgePatterns(targetVertex);

                // #if UNITY_EDITOR
                //                 SaveMergedTileForVertex(targetVertex, targetY);
                // #endif

            }
            // else if the block module is not created, we do nothing.
        }
        else // else if the block module is already created and was clicked on again, we recursively update the block module collider with an additional block module.
        {
            if (cursorOperation == CursorOperation.LeftClickToAddBlock)
            {
                if (currentCursorObject != null)
                {
                    if (currentCursorObject.layer == LayerMask.NameToLayer("BlockModuleCollider_TopBottom"))
                    {
                        int topOrBottomDirection = currentCursorObject.GetComponent<BlockColliderTopBottom>().TopOrBottomDirection;
                        Vertex nextTargetVertex = currentCursorObject.GetComponent<BlockColliderTopBottom>().TargetVertex;
                        float currentTargetY = currentCursorObject.GetComponent<BlockColliderTopBottom>().TargetY;

                        float nextTargetY = currentTargetY + topOrBottomDirection * grid.Height; // it's either up or down

                        if (nextTargetY < grid.Height * (grid.Floor - 1)) // if the next target y is within the grid height range
                        {
                            UpdateBlockModuleCollider(nextTargetVertex, nextTargetY, CursorOperation.LeftClickToAddBlock, currentCursorObject);
                        }
                    }

                    if (currentCursorObject.layer == LayerMask.NameToLayer("BlockModuleCollider_Sides"))
                    {
                        Vertex nextTargetVertex = currentCursorObject.GetComponent<BlockColliderSides>().SideDirectionVertex;
                        float nextTargetY = currentCursorObject.GetComponent<BlockColliderSides>().TargetY;

                        UpdateBlockModuleCollider(nextTargetVertex, nextTargetY, CursorOperation.LeftClickToAddBlock, currentCursorObject);
                    }
                }
            }

            if (cursorOperation == CursorOperation.RightClickToRemoveBlock)
            {
                List<CubeVertex> cubeVertices = grid.GetCubeVertices(targetVertex, targetY);
                foreach (CubeVertex cubeVertex in cubeVertices)
                {
                    cubeVertex.IsActive = false;
                }

                // Doesn't work:
                // var subQuads = GetSubQuadsAroundVertex(targetVertex);
                // foreach (var sq in subQuads)
                // {
                //     if (subQuadMeshes.TryGetValue(sq, out GameObject old))
                //     {
                //         Destroy(old);
                //         subQuadMeshes.Remove(sq);
                //         Resources.UnloadUnusedAssets();
                //     }
                // }
                // Destroy(blockColliderChildGameObject);
                // Resources.UnloadUnusedAssets();
            }

        }
    }

    private void CheckForBridgePatterns(Vertex placedVertex)
    {
        // Check neighbors AND itself
        List<Vertex> toCheck = new List<Vertex>();
        toCheck.Add(placedVertex);

        foreach (var edge in placedVertex.Edges)
            toCheck.Add(edge.GetOtherVertex(placedVertex));

        foreach (var v in toCheck)
        {
            if (TryDetectBridge(v, out Vertex left, out Vertex right))
            {
                PlaceBridge(left, v, right);
            }
        }
    }

    private bool TryDetectBridge(Vertex center, out Vertex left, out Vertex right)
    {
        left = null;
        right = null;

        if (!WFC3D.vertexTileTypes.TryGetValue(center, out TileType centerType))
            return false;

        // Middle must be WATER
        if (centerType != TileType.WATER)
            return false;

        // Get neighbors
        List<Vertex> neighbors = center.Edges
            .Select(e => e.GetOtherVertex(center))
            .Where(v => !v.IsBoundary)
            .ToList();

        // Find LAND neighbors
        var landNeighbors = neighbors
            .Where(v => WFC3D.vertexTileTypes.ContainsKey(v) &&
                        WFC3D.vertexTileTypes[v] == TileType.LAND)
            .ToList();

        // Need at least 2 LAND tiles
        if (landNeighbors.Count < 2)
            return false;

        // Try all pairs instead of just [0] and [1]
        for (int i = 0; i < landNeighbors.Count; i++)
        {
            for (int j = i + 1; j < landNeighbors.Count; j++)
            {
                Vertex a = landNeighbors[i];
                Vertex b = landNeighbors[j];

                // This is where our dot check goes
                float dot = Vector3.Dot(
                    (a.point - center.point).normalized,
                    (b.point - center.point).normalized
                );

                // Only accept "opposite" tiles
                if (dot < -0.8f)
                {
                    left = a;
                    right = b;
                    return true;
                }
            }
        }

        return false;

        // Pick any 2 (or improve later with direction check)
        // left = landNeighbors[0];
        // right = landNeighbors[1];

        // return true;
    }

    private void PlaceBridge(Vertex left, Vertex center, Vertex right)
    {
        // Remove existing tiles
        RemoveVertexMeshes(left);
        RemoveVertexMeshes(center);
        RemoveVertexMeshes(right);

        // Get prefab (use your TripleTiles or custom list)
        GameObject prefab = GetBridgePrefab();

        GameObject go = Instantiate(prefab, transform);

        // Position in the middle of 3 vertices
        Vector3 pos = (left.point + center.point + right.point) / 3f;
        go.transform.position = pos;

        // Rotate to align with direction
        Vector3 dir = (right.point - left.point).normalized;
        go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // Deform it to match terrain
        ApplyLatticeToPrefab(go, new List<Vertex> { left, center, right });
    }

    private void RemoveVertexMeshes(Vertex v)
    {
        var subQuads = GetSubQuadsAroundVertex(v);

        foreach (var sq in subQuads)
        {
            if (subQuadMeshes.TryGetValue(sq, out GameObject old))
            {
                Destroy(old);
                subQuadMeshes.Remove(sq);
            }
        }
    }

    private GameObject GetBridgePrefab()
    {
        foreach (var info in _placeholderTileInfos)
        {
            if (info.TileType == TileType.LAND && info.TripleTiles.Count > 0)
            {
                return info.TripleTiles[Random.Range(0, info.TripleTiles.Count)];
            }
        }

        return null;
    }

    private void ApplyLatticeToPrefab(GameObject go, List<Vertex> region)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) return;

        Mesh originalMesh = mf.sharedMesh;

        // Copy mesh
        Mesh mesh = MeshModificationUtils.CopyMesh(originalMesh);

        // Compute bounds
        Bounds bounds = mesh.bounds;

        // Normalize
        var data = MeshModificationUtils.CalculateMeshModificationData(mesh.vertices, bounds);

        // Build lattice from region
        Vector3[] lattice = BuildLatticeFromRegion(region, go.transform);

        // Apply deformation
        Vector3[] deformed = MeshModificationUtils.ApplyLatticeDeformation(mesh.vertices, data, lattice);

        mesh.vertices = deformed;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        mf.sharedMesh = mesh;
    }

    private Vector3[] BuildLatticeFromRegion(List<Vertex> region, Transform t)
    {
        // Simple bounding box (works well for now)
        Vector3 min = region[0].point;
        Vector3 max = region[0].point;

        foreach (var v in region)
        {
            min = Vector3.Min(min, v.point);
            max = Vector3.Max(max, v.point);
        }

        float bottomY = targetY - 3f;
        float topY = bottomY + 2.6f;

        Vector3[] lattice = new Vector3[8];

        // bottom
        lattice[0] = t.InverseTransformPoint(new Vector3(min.x, bottomY, max.z));
        lattice[1] = t.InverseTransformPoint(new Vector3(max.x, bottomY, max.z));
        lattice[2] = t.InverseTransformPoint(new Vector3(max.x, bottomY, min.z));
        lattice[3] = t.InverseTransformPoint(new Vector3(min.x, bottomY, min.z));

        // top
        lattice[4] = t.InverseTransformPoint(new Vector3(min.x, topY, max.z));
        lattice[5] = t.InverseTransformPoint(new Vector3(max.x, topY, max.z));
        lattice[6] = t.InverseTransformPoint(new Vector3(max.x, topY, min.z));
        lattice[7] = t.InverseTransformPoint(new Vector3(min.x, topY, min.z));

        return lattice;
    }








#if UNITY_EDITOR
    private void SaveMergedTileForVertex(Vertex vertex, float y)
    {
        var subQuads = GetSubQuadsAroundVertex(vertex);

        List<CombineInstance> combine = new List<CombineInstance>();
        List<Material> materials = new List<Material>();

        foreach (var sq in subQuads)
        {
            if (!subQuadMeshes.TryGetValue(sq, out GameObject go))
                continue;

            MeshFilter mf = go.GetComponent<MeshFilter>();
            MeshRenderer mr = go.GetComponent<MeshRenderer>();

            if (mf == null || mf.sharedMesh == null) continue;

            for (int i = 0; i < mf.sharedMesh.subMeshCount; i++)
            {
                CombineInstance ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    subMeshIndex = i,
                    transform = mf.transform.localToWorldMatrix
                };

                combine.Add(ci);

                if (mr != null && i < mr.sharedMaterials.Length)
                    materials.Add(mr.sharedMaterials[i]);
            }
        }

        if (combine.Count == 0)
        {
            Debug.LogWarning("Nothing to merge!");
            return;
        }

        Mesh merged = new Mesh();
        merged.name = "MergedVertexTile";
        merged.CombineMeshes(combine.ToArray(), false, true);

        GameObject finalGO = new GameObject("MergedTile");
        finalGO.AddComponent<MeshFilter>().sharedMesh = merged;
        finalGO.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();

        string shape = GetShapeNameFromVertex(vertex);
        string name = $"Tile_{shape}_{System.Guid.NewGuid()}";

        SaveTileAsPrefab(finalGO, name);

        DestroyImmediate(finalGO);
    }
#endif

    // Rebuild all neighboring subquads:
    private void UpdateSubQuadsAroundVertex(Vertex vertex, float y)
    {
        var affectedSubQuads = GetSubQuadsAroundVertex(vertex);

        foreach (var subQuad in affectedSubQuads)
        {
            UpdateSubQuadMesh(subQuad, y, vertex);
        }
    }

    private List<SubQuad> GetSubQuadsAroundVertex(Vertex vertex)
    {
        List<SubQuad> result = new List<SubQuad>();

        foreach (var subQuad in subQuads)
        {
            if (subQuad.Vertices.Contains(vertex))
                result.Add(subQuad);
        }

        return result;
    }

    private void UpdateSubQuadMesh(SubQuad subQuad, float y, Vertex targetVertex)
    {
        bool[] active = GetCornerStates(subQuad, y);

        // Remove old mesh if exists
        if (subQuadMeshes.TryGetValue(subQuad, out var old))
        {
            Destroy(old);
            subQuadMeshes.Remove(subQuad);
        }

        GameObject tileGO = BuildTileGameObject(active);
        if (tileGO == null) return;

        PositionTile(tileGO.transform, subQuad);
        DeformTileMesh(tileGO, subQuad);

        subQuadMeshes[subQuad] = tileGO;

        // #if UNITY_EDITOR
        //         string shape = GetShapeNameFromVertex(targetVertex);
        //         string name = $"Tile_{shape}"; // _{targetVertex.GetHashCode()}";

        //         SaveTileAsPrefab(tileGO, name);
        // #endif
    }

    private bool[] GetCornerStates(SubQuad subQuad, float y)
    {
        bool[] active = new bool[4];

        for (int i = 0; i < 4; i++)
        {
            var v = subQuad.Vertices[i];

            active[i] = grid.CubeVertices.Any(cv =>
                cv.SubQuad == subQuad &&
                Mathf.Approximately(cv.Y, y) &&
                cv.Vertex == v &&
                cv.IsActive);

            // if (WFC3D.vertexTileTypes.TryGetValue(v, out TileType type))
            // {
            //     active[i] = (type == TileType.LAND);
            // }
            // else
            // {
            //     active[i] = false;
            // }
        }

        return active;
    }

    // Tile creation
    private GameObject BuildTileGameObject(bool[] active)
    {
        TileType[] types = new TileType[4];

        for (int i = 0; i < 4; i++)
            types[i] = active[i] ? TileType.LAND : TileType.WATER; // Instead of LAND and WATER could use bitmask values or other tile types if needed

        GameObject tileGO = InstantiatePlaceholderTile(types);

        if (tileGO != null)
            tileGO.transform.SetParent(transform);

        return tileGO;
    }

    // Positioning
    private void PositionTile(Transform t, SubQuad subQuad)
    {
        Vector3 a = subQuad.Vertices[0].point;
        Vector3 b = subQuad.Vertices[1].point;
        Vector3 c = subQuad.Vertices[2].point;
        Vector3 d = subQuad.Vertices[3].point;

        Vector3 center = (a + b + c + d) * 0.25f;
        Vector3 forward = ((a + b) * 0.5f - center).normalized; // Forward direction from the center to the midpoint of edge AB

        t.position = center;
        t.rotation = Quaternion.LookRotation(forward, Vector3.up);
        t.localScale = Vector3.one;
    }

    // Deformation
    private void DeformTileMesh(GameObject tileGO, SubQuad subQuad)
    {
        MeshFilter mf = tileGO.GetComponent<MeshFilter>();
        Mesh original = mf.sharedMesh;

        ApplyMeshTransformation(original,
            new TileMeshTransformationInfo(),
            out Mesh transformed);

        // We use VERY SMALL bounds to ensure that the deformation is more pronounced and fits the subquad vertices better.
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(0.01f, 0.026f, 0.01f));

        // Calculate normalized coordinates based on the small bounds, this will help to achieve a more accurate deformation that fits the subquad vertices.
        // We don't need to normalize manually anymore.
        var data = MeshModificationUtils.CalculateMeshModificationData(
            transformed.vertices, bounds);

        Vector3[] lattice = BuildLattice(subQuad, tileGO.transform);

        Vector3[] deformed = MeshModificationUtils.ApplyLatticeDeformation(
            transformed.vertices, data, lattice);

        transformed.vertices = deformed;
        transformed.RecalculateBounds();
        transformed.RecalculateNormals();

        mf.sharedMesh = transformed;
    }

    private Vector3[] BuildLattice(SubQuad subQuad, Transform t)
    {
        Vector3 a = subQuad.Vertices[0].point;
        Vector3 b = subQuad.Vertices[1].point;
        Vector3 c = subQuad.Vertices[2].point;
        Vector3 d = subQuad.Vertices[3].point;

        float h = 2.6f; // height of the tile lattice, can be adjusted based on the desired height of the tile deformation

        // We move the lattice downwards to better align with the subquad vertices and achieve a more natural deformation.
        a += Vector3.down * 1.5f;
        b += Vector3.down * 1.5f;
        c += Vector3.down * 1.5f;
        d += Vector3.down * 1.5f;

        Vector3[] lattice = new Vector3[8];

        lattice[0] = t.InverseTransformPoint(a);
        lattice[1] = t.InverseTransformPoint(b);
        lattice[2] = t.InverseTransformPoint(c);
        lattice[3] = t.InverseTransformPoint(d);

        lattice[4] = t.InverseTransformPoint(a + Vector3.up * h);
        lattice[5] = t.InverseTransformPoint(b + Vector3.up * h);
        lattice[6] = t.InverseTransformPoint(c + Vector3.up * h);
        lattice[7] = t.InverseTransformPoint(d + Vector3.up * h);

        return lattice;
    }

    /// <summary>
    /// Instantiates a placeholder tile for the given corner tile types and returns the instantiated game object.
    /// Function goes over the corner tile types and groups them by type, creating a chain of tiles of the same type.
    /// It then merges the prefabs for the tiles in the chain and returns the instantiated game object.
    /// </summary>
    /// <param name="cornerTileTypes">The tile types of the corner tiles of the quad.</param>
    /// <returns>The instantiated game object.</returns>
    private GameObject InstantiatePlaceholderTile(TileType[] cornerTileTypes)
    {
        TileType currentTileType = TileType.WATER;
        int currentChainLength = 0;
        List<TileType> placeholderTileTypes = new List<TileType>();
        List<int> placeholderTileChainLengths = new List<int>();
        List<int> rotationDegrees = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            TileType cornerTileType = cornerTileTypes[i];
            if (i == 0)
            {
                // Initial tile type should be the one of the first corner tile.
                currentTileType = cornerTileType;
            }

            // If the current corner tile type is the same as the previous one, increment the chain length.
            if (cornerTileType == currentTileType)
            {
                currentChainLength++;
            }
            else
            {
                // Otherwise, the previous tile chain is complete, so we add it to the list of placeholder 
                // tile types with the appropriate rotation degrees.
                placeholderTileTypes.Add(currentTileType);
                placeholderTileChainLengths.Add(currentChainLength);
                rotationDegrees.Add(90 * (i - currentChainLength));
                currentTileType = cornerTileType;
                currentChainLength = 1;
            }

            // If it's not the last corner tile, continue to the next one. Only the last corner tile has additional special logic.
            if (i != 3)
            {
                continue;
            }

            // For the last corner tile, we need to check if the last tile is of the same type as the first one, and if so, we need to connect them.
            if (currentChainLength < 4 && placeholderTileTypes[0] == cornerTileType)
            {
                // The last tile is of the same type as the first one, so we need to connect them.
                placeholderTileChainLengths[0] += currentChainLength;
                rotationDegrees[0] = 90 * (i - currentChainLength + 1);
            }
            else
            {
                // The last tile is not of the same type as the first one, so we add it to the list of 
                // placeholder tile types with the appropriate rotation degrees.
                placeholderTileTypes.Add(currentTileType);
                placeholderTileChainLengths.Add(currentChainLength);
                rotationDegrees.Add(90 * (i - currentChainLength + 1));
            }
        }

        // If there are four different tile types, one in each corner, we need to instantiate the opposite corners tile.
        bool shouldInstantiateOppositeCornersTile = false;
        if (placeholderTileTypes.Count == 4)
        {
            for (int i = 0; i < placeholderTileTypes.Count; i++)
            {
                TileType placeholderTileType = placeholderTileTypes[i];
                if (placeholderTileType != TileType.LAND)
                {
                    continue;
                }

                if (!shouldInstantiateOppositeCornersTile)
                {
                    shouldInstantiateOppositeCornersTile = true;
                    continue;
                }
                else
                {
                    placeholderTileTypes.RemoveAt(i);
                    placeholderTileChainLengths.RemoveAt(i);
                    rotationDegrees.RemoveAt(i);
                    i--;
                    continue;
                }
            }
        }

        // Fetch all required placeholder tile prefabs based on the tile types and chain lengths.
        // We're going to use their meshes to create a single merged mesh.
        List<GameObject> placeholderTilePrefabs = new List<GameObject>();
        for (int i = 0; i < placeholderTileTypes.Count; i++)
        {
            TileType placeholderTileType = placeholderTileTypes[i];
            int placeholderTileChainLength = placeholderTileChainLengths[i];
            bool requireOppositeCornersTile = placeholderTileType == TileType.LAND && shouldInstantiateOppositeCornersTile;
            GameObject placeholderTilePrefab = GetPlaceholderTilePrefab(placeholderTileType, placeholderTileChainLength, requireOppositeCornersTile);
            placeholderTilePrefabs.Add(placeholderTilePrefab);
        }

        // Try to merge the placeholder tile prefabs into a single mesh while applying the appropriate rotation degrees to each prefab's mesh.
        // The result of a successful merge will be a new mesh that represents the merged geometry of all the placeholder tile prefabs and preserves 
        // the submesh and material relationships of the source prefabs.
        if (!TryMergePrefabsWithYRotations(placeholderTilePrefabs, rotationDegrees, out Mesh mergedMesh, out List<Material> mergedMeshMaterials))
        {
            Debug.LogError("Failed to merge placeholder tile prefabs.");
            return null;
        }

        // Finally, instantiate a new game object that will hold the merged placeholder tile mesh and return it.
        GameObject tileInstance = new GameObject("Placeholder Tile");
        tileInstance.transform.position = Vector3.zero;
        tileInstance.transform.rotation = Quaternion.identity;
        tileInstance.transform.localScale = Vector3.one;
        tileInstance.transform.SetParent(transform);
        tileInstance.AddComponent<MeshFilter>().sharedMesh = mergedMesh;
        tileInstance.AddComponent<MeshRenderer>().sharedMaterials = mergedMeshMaterials.ToArray();

        return tileInstance;
    }

    private GameObject GetPlaceholderTilePrefab(TileType placeholderTileType, int placeholderTileChainLength, bool requireOppositeCornersTile)
    {
        foreach (PlaceholderTileInfo placeholderTileInfo in _placeholderTileInfos)
        {
            if (placeholderTileInfo.TileType != placeholderTileType)
            {
                continue;
            }

            List<GameObject> tiles = null;
            switch (placeholderTileChainLength)
            {
                case 1:
                    tiles = placeholderTileInfo.SingleTiles;
                    break;
                case 2:
                    tiles = placeholderTileInfo.DoubleTiles;
                    break;
                case 3:
                    tiles = placeholderTileInfo.TripleTiles;
                    break;
                case 4:
                    tiles = placeholderTileInfo.FullTiles;
                    break;
                default:
                    throw new Exception($"Invalid placeholder tile chain length: {placeholderTileChainLength}");
            }

            if (requireOppositeCornersTile)
            {
                tiles = placeholderTileInfo.OppositeCornersTiles;
            }

            if (tiles == null || tiles.Count == 0)
            {
                return null;
            }

            return tiles[UnityEngine.Random.Range(0, tiles.Count)];
        }

        Debug.LogError($"Placeholder tile prefab not found for tile type: {placeholderTileType} and chain length: {placeholderTileChainLength}");
        return null;
    }

    /// <summary>
    /// Function tries to merge meshes from the specified prefabs, applying a rotation around the Y axis to each one.
    /// The merged mesh preserves submesh and material relationships and does not modify or destroy any of the source prefab meshes.
    /// Returns true if the merge was successful, false otherwise.
    /// </summary>
    /// <param name="prefabs">A list of prefabs that each contain a MeshFilter and MeshRenderer component.</param>
    /// <param name="yAxisRotations">A list of Y-axis rotations, in degrees, to apply to each prefab. The number of elements must match the number of prefabs.</param>
    /// <param name="mergedMesh">The merged mesh that represents the merged geometry of all provided prefabs.</param>
    /// <param name="mergedMeshMaterials">The materials of the merged mesh.</param>
    /// <returns>True if the merge was successful, false otherwise.</returns>
    private bool TryMergePrefabsWithYRotations(List<GameObject> prefabs, List<int> yAxisRotations, out Mesh mergedMesh, out List<Material> mergedMeshMaterials)
    {
        mergedMesh = null;
        mergedMeshMaterials = null;

        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning("No prefabs provided.");
            return false;
        }

        if (yAxisRotations == null || yAxisRotations.Count != prefabs.Count)
        {
            Debug.LogError("yAxisRotations list must match the length of the prefabs list.");
            return false;
        }

        // We group combine instances by material so that each material becomes a distinct submesh in the final mesh.
        // This ensures the merged mesh can still be rendered with multiple materials just like the source meshes.
        Dictionary<Material, List<CombineInstance>> materialToInstancesMap = new Dictionary<Material, List<CombineInstance>>();

        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
            {
                continue;
            }

            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshRenderer == null || meshFilter.sharedMesh == null)
            {
                continue;
            }

            Mesh sourceMesh = meshFilter.sharedMesh;
            Material[] sourceMaterials = meshRenderer.sharedMaterials;

            // The rotation matrix is applied to transform vertex positions without modifying the original shared mesh.
            // This avoids altering prefab assets and allows reuse of the same mesh data safely.
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0f, yAxisRotations[i], 0f));

            for (int submeshIndex = 0; submeshIndex < sourceMesh.subMeshCount; submeshIndex++)
            {
                if (submeshIndex >= sourceMaterials.Length)
                {
                    continue;
                }

                Material material = sourceMaterials[submeshIndex];

                // Lazily initialize a list for this material if it has not been encountered yet.
                if (!materialToInstancesMap.ContainsKey(material))
                {
                    materialToInstancesMap[material] = new List<CombineInstance>();
                }

                // Each CombineInstance references a submesh of the source prefab.
                // We use the rotation matrix instead of actual transform manipulation to minimize memory and processing overhead.
                CombineInstance combineInstance = new CombineInstance
                {
                    mesh = sourceMesh,
                    subMeshIndex = submeshIndex,
                    transform = rotationMatrix
                };

                materialToInstancesMap[material].Add(combineInstance);
            }
        }

        // Each material group will be combined into a single temporary mesh first.
        // This two-pass process (by material, then globally) ensures submesh boundaries remain correct.
        List<Material> finalMaterials = new List<Material>();
        List<CombineInstance> finalCombineInstances = new List<CombineInstance>();
        List<Mesh> temporaryMeshes = new List<Mesh>();

        foreach (KeyValuePair<Material, List<CombineInstance>> entry in materialToInstancesMap)
        {
            Material material = entry.Key;
            List<CombineInstance> combineInstancesForMaterial = entry.Value;

            // Combining all meshes using the same material reduces draw calls and maintains proper submesh material assignment.
            Mesh combinedSubmesh = new Mesh();
            combinedSubmesh.CombineMeshes(combineInstancesForMaterial.ToArray(), true, true);
            temporaryMeshes.Add(combinedSubmesh);

            // Each combined submesh becomes one submesh in the final merged mesh.
            CombineInstance submeshCombineInstance = new CombineInstance
            {
                mesh = combinedSubmesh,
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };

            finalCombineInstances.Add(submeshCombineInstance);
            finalMaterials.Add(material);
        }

        // The second combination step merges all submeshes into one final mesh while preserving submesh separation.
        mergedMesh = new Mesh();
        mergedMesh.name = "MergedPrefabsMesh";
        mergedMesh.CombineMeshes(finalCombineInstances.ToArray(), false, false);

        // Temporary meshes must be destroyed explicitly to prevent native memory leaks.
        // Unity does not automatically clean up Mesh objects created with 'new Mesh()'.
        foreach (Mesh temporaryMesh in temporaryMeshes)
        {
#if UNITY_EDITOR
            DestroyImmediate(temporaryMesh);
#else
            Destroy(temporaryMesh);
#endif
        }

        // The caller is responsible for assigning materials and using the returned merged mesh as needed.
        mergedMeshMaterials = finalMaterials;
        return true;
    }

    /// <summary>
    /// Applies the tile mesh transformations to the original vertices. It rotates and flips the original mesh to match the desired transformation and returns the transformed mesh.
    /// </summary>
    /// <param name="tileOriginalMesh">The original tile mesh.</param>
    /// <param name="tileMeshTransformationInfo">The tile mesh transformation info.</param>
    /// <param name="transformedMesh">The transformed mesh. The original mesh is not modified, a new mesh is created and returned.</param>
    private void ApplyMeshTransformation(Mesh tileOriginalMesh, TileMeshTransformationInfo tileMeshTransformationInfo, out Mesh transformedMesh)
    {
        // Create a copy of the original mesh.
        transformedMesh = MeshModificationUtils.CopyMesh(tileOriginalMesh);

        // If there's no transformation to apply, return the exact copy of the original mesh.
        if (Mathf.Approximately(tileMeshTransformationInfo.RotationDegrees, 0f) && !tileMeshTransformationInfo.FlipHorizontally && !tileMeshTransformationInfo.FlipVertically)
        {
            return;
        }

        // Get the original mesh data that will be used as a guide for applying transformations.
        Vector3[] originalVertices = tileOriginalMesh.vertices;
        Vector3[] originalNormals = tileOriginalMesh.normals;
        int[] originalTriangles = tileOriginalMesh.triangles;

        int vertexCount = originalVertices.Length;
        Vector3[] transformedVertices = new Vector3[vertexCount];
        Vector3[] transformedNormals = new Vector3[vertexCount];

        // Calculate the transformation matrix and the normal transformation matrix.
        Quaternion rotation = Quaternion.Euler(0f, tileMeshTransformationInfo.RotationDegrees, 0f);
        Vector3 scale = new Vector3(tileMeshTransformationInfo.FlipHorizontally ? -1f : 1f, 1f, tileMeshTransformationInfo.FlipVertically ? -1f : 1f);
        Matrix4x4 transformationMatrix = Matrix4x4.TRS(Vector3.zero, rotation, scale);
        Matrix4x4 normalTransformationMatrix = transformationMatrix.inverse.transpose;

        // Apply the transformations to the vertices and normals.
        for (int i = 0; i < vertexCount; i++)
        {
            transformedVertices[i] = transformationMatrix.MultiplyPoint3x4(originalVertices[i]);
            transformedNormals[i] = normalTransformationMatrix.MultiplyVector(originalNormals[i]).normalized;
        }
        transformedMesh.vertices = transformedVertices;
        transformedMesh.normals = transformedNormals;

        // Flip the triangles if the transformation matrix has a negative determinant.
        bool shouldFlipTriangles = transformationMatrix.determinant < 0f;
        for (int i = 0; i < tileOriginalMesh.subMeshCount; i++)
        {
            int[] originalSubmeshTriangles = tileOriginalMesh.GetTriangles(i);
            int[] transformedSubmeshTriangles = new int[originalSubmeshTriangles.Length];
            for (int j = 0; j < originalSubmeshTriangles.Length; j += 3)
            {
                if (shouldFlipTriangles)
                {
                    transformedSubmeshTriangles[j] = originalSubmeshTriangles[j + 2];
                    transformedSubmeshTriangles[j + 2] = originalSubmeshTriangles[j];
                    transformedSubmeshTriangles[j + 1] = originalSubmeshTriangles[j + 1];
                }
                else
                {
                    transformedSubmeshTriangles[j] = originalSubmeshTriangles[j];
                    transformedSubmeshTriangles[j + 1] = originalSubmeshTriangles[j + 1];
                    transformedSubmeshTriangles[j + 2] = originalSubmeshTriangles[j + 2];
                }
            }
            transformedMesh.SetTriangles(transformedSubmeshTriangles, i);
        }

        // Recalculate the bounds of the transformed mesh.
        transformedMesh.RecalculateBounds();
    }

#if UNITY_EDITOR
    public void SaveTileAsPrefab(GameObject tileGO, string name)
    {
        if (tileGO == null) return;

        // 1. Clone so we don't mess with scene object
        GameObject clone = Instantiate(tileGO);
        clone.name = name;

        // 2. Ensure mesh is unique (VERY IMPORTANT)
        MeshFilter mf = clone.GetComponent<MeshFilter>();
        if (mf != null)
        {
            mf.sharedMesh = Instantiate(mf.sharedMesh);
        }

        ExportMeshToOBJ(mf.sharedMesh, name); // Optional: export to OBJ for external use
    }
#endif

#if UNITY_EDITOR

    public void ExportMeshToOBJ(Mesh mesh, string name)
    {
        string path = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            name + ".obj"
        );

        using (StreamWriter sw = new StreamWriter(path))
        {
            foreach (Vector3 v in mesh.vertices)
                sw.WriteLine($"v {v.x} {v.y} {v.z}");

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                sw.WriteLine($"f {mesh.triangles[i]+1} {mesh.triangles[i+1]+1} {mesh.triangles[i+2]+1}");
            }
        }

        Debug.Log("Exported OBJ to Desktop: " + path);
    }
#endif

    private string GetShapeNameFromVertex(Vertex vertex)
    {
        int edgeCount = vertex.Edges.Count;

        switch (edgeCount)
        {
            case 3: return "Triangle";
            case 4: return "Quad";
            case 5: return "Pentagon";
            case 6: return "Hexagon";
            default: return "Unknown";
        }
    }




    public void SetRandomBuildingGeneration()
    {
        Transform gridManager = transform.Find("GridManager");

        if (gridManager != null)
        {
            cubeVertices.ForEach(x => x.IsActive = false);
            for (int i = 0; i < gridManager.childCount; i++)
            {
                Destroy(gridManager.GetChild(i).gameObject);
                Resources.UnloadUnusedAssets();
            }
        }

        // Transform blockSlot = transform.Find("BlockSlots"); //GameObject.Find("Grid").transform.Find("Slot");
        Transform blockSlots = transform.GetChild(0);

        if (blockSlots != null)
        {
            for (int i = 0; i < blockSlots.childCount; i++)
            {
                Destroy(blockSlots.GetChild(i).gameObject);
                Resources.UnloadUnusedAssets();
            }
        }

        for (int i = 0; i < grid.Floor - 1; i++)
        {
            foreach (Vertex vertex in grid.Vertices)
            {
                if (!vertex.IsBoundary)
                {
                    if (Random.value < 0.3f)
                    {
                        UpdateBlockModuleCollider(vertex, i * grid.Height, CursorOperation.LeftClickToAddBlock, null);
                    }
                }
            }

        }
    }

    // From the book "Real-Time Collision Detection" by Christer Ericson:
    // Test if quadrilateral (a, b, c, d) is convex
    // public int IsConvexQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    // {
    //     // Quad is nonconvex if Dot(Cross(bd, ba), Cross(bd, bc)) >= 0
    //     Vector3 bda = Vector3.Cross(d - b, a - b);
    //     Vector3 bdc = Vector3.Cross(d - b, c - b);
    //     if (Vector3.Dot(bda, bdc) >= 0.0f)
    //     {
    //         return 0;
    //     }
    //     // Quad is now convex iff Dot(Cross(ac, ad), Cross(ac, ab)) < 0
    //     Vector3 acd = Vector3.Cross(c - a, d - a);
    //     Vector3 acb = Vector3.Cross(c - a, b - a);
    //     return Vector3.Dot(acd, acb) < 0.0f ? 1 : 0;
    // }


    private void OnDrawGizmos()
    {
        if (vertices == null)
        {
            return;
        }

        // foreach (Vertex vertex in vertices)
        // {
        //     if (!vertex.IsBoundary)
        //     {
        //         Gizmos.DrawSphere(vertex.point, 0.5f);
        //     }
        // }

        // foreach (CubeVertex vertex in verticesAll)
        // {
        //     if (vertex.IsActive)
        //         Gizmos.color = Color.red;
        //     else
        //         Gizmos.color = Color.gray;
        //     Gizmos.DrawSphere(vertex.worldPoint, 0.1f);
        // }




        // foreach (var edge in edges)
        // {
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawLine(edge.Vertices.ToArray()[0].point, edge.Vertices.ToArray()[1].point);
        // }


        // for (int j = 0; j < subQuads.Count; j++)
        // {
        //     var quad = subQuads[j];

        //     Gizmos.color = Color.yellow;
        //     //Gizmos.DrawSphere(quad.Edges[0].Vertices.ToArray()[0].point, 0.5f);

        //     for (int i = 0; i < quad.Edges.Count; i++)
        //     {
        //         var edge = quad.Edges[i];
        //         var edge_next = quad.Edges[(i + 1) % quad.Edges.Count];
        //         Gizmos.color = Color.cyan;
        //         Gizmos.DrawLine(edge.Vertices.ToArray()[0].point, edge.Vertices.ToArray()[1].point);


        //         //Gizmos.color = Color.red;
        //         //Gizmos.DrawRay(new Ray(edge.Vertices.ToArray()[0].point, edge.Vertices.ToArray()[1].point- edge.Vertices.ToArray()[0].point));
        //         //GUI.color = Color.yellow;
        //         //Handles.Label(edge.Vertices.ToArray()[0].point, edge.Vertices.ToArray()[0].point.ToString());
        //         //Handles.Label(edge.Vertices.ToArray()[1].point, edge.Vertices.ToArray()[1].point.ToString());
        //     }
        // }




        // foreach (var tri in triangles)
        // {
        //     for (int i = 0; i < tri.Edges.Count; i++)
        //     {
        //         var edge = tri.Edges[i];
        //         Gizmos.color = Color.red;
        //         Gizmos.DrawLine(edge.Vertices.ToArray()[0].point, edge.Vertices.ToArray()[1].point);

        //     }
        // }


        // foreach (var quad in quads)
        // {
        //     for (int i = 0; i < quad.Edges.Count; i++)
        //     {
        //         var edge = quad.Edges[i];
        //         Gizmos.color = Color.red;
        //         Gizmos.DrawLine(edge.Vertices.ToArray()[0].point, edge.Vertices.ToArray()[1].point);

        //     }
        // }


        // for (int j = 0; j < cubes.Count; j++)
        // {
        //     Cube cube = cubes[j];

        //     GUI.color = Color.yellow;
        //     //Handles.Label(cube.centroid, cube.StateIndicator.ToString());
        //     for (int i = 0; i < cube.Edges.Count; i++)
        //     {
        //         Edge edge = cube.Edges[i];

        //         Gizmos.color = Color.cyan;
        //         Gizmos.DrawLine(edge.a3.worldPoint, edge.b3.worldPoint);
        //     }

        //     foreach (Vertex3 vertex in cube.Vertices)
        //     {
        //         if (vertex.IsActive)
        //             Gizmos.color = Color.red;
        //         else
        //             Gizmos.color = Color.gray;
        //         Gizmos.DrawSphere(vertex.worldPoint, 0.3f);
        //     }
        // }



    }


}
