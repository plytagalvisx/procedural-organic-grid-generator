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
            }

        }
    }

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

    private void OnDrawGizmos()
    {
        if (vertices == null)
        {
            return;
        }
    }


}
