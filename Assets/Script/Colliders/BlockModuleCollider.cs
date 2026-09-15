using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockModuleCollider : MonoBehaviour
{
    private List<CubeVertex> primalGridFaceVertices; // aka Cube/Block vertices
    private float cubeHeight;
    private Grid grid;
    private Material cursor_material;
    private Material cursor_material_dark;

    public void Start()
    {
        InitializeGridProperties(); // Initialize the grid properties (e.g. cubeHeight, grid) to be used in the BlockModuleCollider class and avoid using GridManager.Instance all the time.
    }

    public void InitializeGridProperties()
    {
        cubeHeight = GridManager.instance.height;
        grid = GridManager.instance.grid;
    }

    // Aka the Dual Grid Element (Block Part) construction method: 
    // Constructs and returns a list of dual grid elements representing the data structure of a placed tile/mesh module in the dual grid.
    // For example, a square block/cube module will have 4 dual grid elements, a triangle block/cube module will have 3 dual grid elements, etc.
    public List<DualGridElement> AssembleDualGridElements(Vertex targetVertex, float targetY)
    {
        // Get the vertices of the primal grid cube (3D subquad face points) that correspond to the given target vertex and Y (height)
        primalGridFaceVertices = grid.GetCubeVertices(targetVertex, targetY);

        List<DualGridElement> dualGridElements = new List<DualGridElement>();
        foreach (CubeVertex primalGridFaceVertex in primalGridFaceVertices)
        {
            List<Vertex> neighboringPrimalVertices = new List<Vertex>(); // contains 2 primal vertices neighboring the current primal grid face vertex
            List<Vector3> dualFaceVertices = grid.GetQuarterSubQuad(primalGridFaceVertex, out neighboringPrimalVertices); // contains 4 vertices of the dual face surrounding the primal vertex.

            DualGridElementNeighbor[] dualGridElementNeighbors = new DualGridElementNeighbor[2];
            for (int i = 0; i < dualGridElementNeighbors.Length; i++)
            {
                dualGridElementNeighbors[i] = new DualGridElementNeighbor();
            }
            DualGridElementNeighbor firstNeighbor = dualGridElementNeighbors[0];
            firstNeighbor.selfPoint = dualFaceVertices[1]; // A point along the edge of the dual face
            firstNeighbor.sideDirection = neighboringPrimalVertices[0];

            DualGridElementNeighbor secondNeighbor = dualGridElementNeighbors[1];
            secondNeighbor.selfPoint = dualFaceVertices[3]; // Another point along the edge of the dual face
            secondNeighbor.sideDirection = neighboringPrimalVertices[1];

            // Assemble the dual grid element that combines the dual face vertices, neighboring vertices, and primal vertex details
            DualGridElement currentDualGridElement = new DualGridElement();
            currentDualGridElement.neighbours = dualGridElementNeighbors;
            currentDualGridElement.faceCentroid = dualFaceVertices[2]; // The centroid of the dual face
            currentDualGridElement.targetVertex = targetVertex;
            currentDualGridElement.targetY = targetY;

            dualGridElements.Add(currentDualGridElement);
        }
        List<DualGridElement> sortedDualGridElements = SortDualGridElementsClockwise(dualGridElements); // Sort the dual grid elements in a clockwise manner for proper mesh construction
        return sortedDualGridElements;
    }

    public List<DualGridElement> SortDualGridElementsClockwise(List<DualGridElement> targetDualGridElements)
    {
        Vector3 quadFaceCentroid = new Vector3();
        foreach (DualGridElement targetDualGridElement in targetDualGridElements)
        {
            quadFaceCentroid += targetDualGridElement.faceCentroid;
        }
        Vector3 quadCentroidPoint = quadFaceCentroid / targetDualGridElements.Count;

        // Sort the blockParts based on the angle to the centroid
        targetDualGridElements.Sort((be1, be2) =>
        {
            // The initial approach of adding 360 is meant to handle negative angles 
            // and ensure all angles are within a positive range before taking the modulus 
            // to keep the values between 0 and 360 degrees.
            float xDiffCentroidPoint1 = be1.faceCentroid.x - quadCentroidPoint.x;
            float zDiffCentroidPoint1 = be1.faceCentroid.z - quadCentroidPoint.z;
            double angle1 = (Mathf.Rad2Deg * Math.Atan2(xDiffCentroidPoint1, zDiffCentroidPoint1) + 360) % 360; // Normalization: The % 360 is to ensure the angle is between 0 and 360. The + 360 is to ensure the angle is positive.
            float xDiffCentroidPoint2 = be2.faceCentroid.x - quadCentroidPoint.x;
            float zDiffCentroidPoint2 = be2.faceCentroid.z - quadCentroidPoint.z;
            double angle2 = (Mathf.Rad2Deg * Math.Atan2(xDiffCentroidPoint2, zDiffCentroidPoint2) + 360) % 360; // Math.Atan2(y, x) computes the angle between the positive x-axis and the line from the point (x, y) to the origin (0, 0). Whereas Math.Atan2(x, y) calculates the angle between the positive y-axis and the point (y, x).
            return angle1.CompareTo(angle2); // returns -1 if angle1 is less than angle2, 0 if they are equal, and 1 if angle1 is greater than angle2.
        });

        return targetDualGridElements;
    }

    //_______________________________________________________________________________________________________________________________________________________________________________________
    // DUAL GRID ELEMENT (BLOCK) COLLIDER CREATION:

    public void CreateBlockModuleCollider(List<DualGridElement> targetBlockParts)
    {
        List<Vector3> middleBlockPoints, cornerBlockPoints;
        SeparateBlockPartPoints(targetBlockParts, out middleBlockPoints, out cornerBlockPoints);

        List<Vector3> upperBlockPoints = CalculateVerticalOffsets(cornerBlockPoints.Concat(middleBlockPoints).ToList(), cubeHeight / 2);
        List<Vector3> lowerBlockPoints = CalculateVerticalOffsets(cornerBlockPoints.Concat(middleBlockPoints).ToList(), -cubeHeight / 2);

        CreateBlockSideColliders(targetBlockParts, upperBlockPoints, lowerBlockPoints);
        CreateBlockTopBottomColliders(targetBlockParts[0], upperBlockPoints, lowerBlockPoints);
    }

    private void CreateBlockSideColliders(List<DualGridElement> targetBlockParts, List<Vector3> upperPoints, List<Vector3> lowerPoints)
    {
        int numberOfParts = targetBlockParts.Count; // number of parts (corners) in the block module (e.g. 4 parts for a square block module, 3 parts for a triangle block module, etc.)

        // Create side colliders for the block module
        for (int i = 0; i < numberOfParts; i++)
        {
            List<Vector3> sideVertices = FormSideVertices(lowerPoints, upperPoints, i, numberOfParts);

            Mesh mesh = new Mesh
            {
                vertices = sideVertices.ToArray(),
                triangles = new int[] { 0, 1, 3, 1, 4, 3, 1, 2, 4, 2, 5, 4 }
            };
            mesh.RecalculateBounds();

            GameObject blockSideColliderObject = new GameObject("BlockColliderSide_" + i, typeof(MeshCollider), typeof(BlockColliderSides), typeof(BlockColliderSequences));
            blockSideColliderObject.transform.parent = transform;
            blockSideColliderObject.GetComponent<MeshCollider>().sharedMesh = mesh;
            // "sideDirection" here is needed to obtain the correct vertex of the block part (dual grid element) that is 
            // on the side of the block module when we click to create a block on the side of the block module.
            // If we comment out this line, the block module itself along with its collider will not be created 
            // on the selected side of the block module:
            blockSideColliderObject.GetComponent<BlockColliderSides>().SideDirectionVertex = targetBlockParts[i].neighbours[1].sideDirection;

            blockSideColliderObject.GetComponent<BlockColliderSides>().TargetVertex = targetBlockParts[i].targetVertex;
            blockSideColliderObject.GetComponent<BlockColliderSides>().TargetY = targetBlockParts[i].targetY;
            blockSideColliderObject.GetComponent<BlockColliderSequences>().Queue = i;
            blockSideColliderObject.layer = LayerMask.NameToLayer("BlockModuleCollider_Sides");
        }
    }

    private void CreateBlockTopBottomColliders(DualGridElement targetBlockPart, List<Vector3> upperPoints, List<Vector3> lowerPoints)
    {
        CreateTopOrBottomCollider(upperPoints, "Top", 1, targetBlockPart, 10);
        CreateTopOrBottomCollider(lowerPoints, "Bottom", -1, targetBlockPart, 11);
    }

    public void CreateTopOrBottomCollider(List<Vector3> topBottomVertices, string name, int direction, DualGridElement targetBlockPart, int queue)
    {
        int numberOfParts = topBottomVertices.Count / 2;

        // create a mesh for the top or bottom collider face of the block (dual grid element) module
        Mesh mesh = new Mesh
        {
            vertices = topBottomVertices.ToArray(),
            triangles = GenerateTriangles(numberOfParts)
        };
        mesh.RecalculateBounds();

        // Creates an upper or lower grid collider for the grid 
        GameObject blockTopBottomColliderObject = new GameObject(name + "_" + numberOfParts, typeof(MeshCollider), typeof(BlockColliderTopBottom), typeof(BlockColliderSequences));
        blockTopBottomColliderObject.transform.parent = transform;

        blockTopBottomColliderObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        blockTopBottomColliderObject.GetComponent<BlockColliderTopBottom>().TopOrBottomDirection = direction;
        blockTopBottomColliderObject.GetComponent<BlockColliderTopBottom>().TargetVertex = targetBlockPart.targetVertex;
        blockTopBottomColliderObject.GetComponent<BlockColliderTopBottom>().TargetY = targetBlockPart.targetY;
        blockTopBottomColliderObject.GetComponent<BlockColliderSequences>().Queue = queue;
        blockTopBottomColliderObject.layer = LayerMask.NameToLayer("BlockModuleCollider_TopBottom");
    }


    //_______________________________________________________________________________________________________________________________________________________________________________________
    // CURSOR CREATION:

    public void CreateCursor(List<DualGridElement> targetCubeParts, Material material, Material material_dark, Transform parent, GameObject currentCollider)
    {
        List<Vector3> upperBlockPoints;
        List<Vector3> lowerBlockPoints;
        int colliderFace = -1; // -1 means no collider face is currently being hovered over

        this.cursor_material = material;
        this.cursor_material_dark = material_dark;

        // Creates cursor mesh for the side collider faces
        CreateSideCursorFaces(targetCubeParts, parent, currentCollider, out upperBlockPoints, out lowerBlockPoints, ref colliderFace);

        // Creates cursor mesh for the top and bottom collider faces
        CreateTopBottomCursorFaces(upperBlockPoints, "Top", 1, targetCubeParts[0], parent, 10, colliderFace);
        CreateTopBottomCursorFaces(lowerBlockPoints, "Bottom", -1, targetCubeParts[0], parent, 11, colliderFace);
    }

    public void CreateSideCursorFaces(List<DualGridElement> targetCubeParts, Transform parent, GameObject currentCollider, out List<Vector3> upperBlockPoints, out List<Vector3> lowerBlockPoints, ref int colliderFace)
    {
        // Update colliderFace if a valid currentCollider is provided
        if (currentCollider != null && currentCollider.layer != LayerMask.NameToLayer("GroundCollider"))
        {
            colliderFace = currentCollider.GetComponent<BlockColliderSequences>().Queue;
        }

        // Separate middle and corner points
        List<Vector3> middleBlockPoints, cornerBlockPoints;
        SeparateBlockPartPoints(targetCubeParts, out middleBlockPoints, out cornerBlockPoints);

        // Calculate upper and lower points
        upperBlockPoints = CalculateVerticalOffsets(cornerBlockPoints.Concat(middleBlockPoints).ToList(), cubeHeight / 2); // cubeHeight + 0.5f);
        lowerBlockPoints = CalculateVerticalOffsets(cornerBlockPoints.Concat(middleBlockPoints).ToList(), 0); //-cubeHeight / 2);

        int numberOfParts = targetCubeParts.Count;

        // Create cursor mesh for the side collider faces
        for (int i = 0; i < numberOfParts; i++)
        {
            List<Vector3> sideCursorFaceVertices = FormSideVertices(lowerBlockPoints, upperBlockPoints, i, numberOfParts);

            Mesh mesh = new Mesh
            {
                vertices = sideCursorFaceVertices.ToArray(),
                triangles = new int[] { 0, 1, 3, 1, 4, 3, 1, 2, 4, 2, 5, 4 }
            };
            mesh.RecalculateBounds();

            GameObject cursorSideColliderObject = new GameObject("Cursor_Side_" + i, typeof(MeshFilter), typeof(MeshRenderer), typeof(BlockColliderSequences));
            cursorSideColliderObject.transform.parent = parent;
            cursorSideColliderObject.GetComponent<MeshFilter>().mesh = mesh;

            SetMaterialBasedOnColliderFaceHoverState(cursorSideColliderObject, colliderFace, i);

            cursorSideColliderObject.GetComponent<BlockColliderSequences>().Queue = i;
            cursorSideColliderObject.layer = LayerMask.NameToLayer("Cursor");
        }
    }

    private void SeparateBlockPartPoints(List<DualGridElement> blockParts, out List<Vector3> middleBlockPoints, out List<Vector3> cornerBlockPoints)
    {
        middleBlockPoints = new List<Vector3>(); // aka edge midpoints
        cornerBlockPoints = new List<Vector3>();

        foreach (DualGridElement blockPart in blockParts)
        {
            middleBlockPoints.Add(blockPart.neighbours[1].selfPoint);
            cornerBlockPoints.Add(blockPart.faceCentroid);
        }
    }

    private List<Vector3> FormSideVertices(List<Vector3> lowerBlockPoints, List<Vector3> upperBlockPoints, int index, int numberOfParts)
    {
        return new List<Vector3>
        {
            lowerBlockPoints[index],
            lowerBlockPoints[index + numberOfParts],
            lowerBlockPoints[(index + 1) % numberOfParts],
            upperBlockPoints[index],
            upperBlockPoints[index + numberOfParts],
            upperBlockPoints[(index + 1) % numberOfParts]
        };
    }

    public void CreateTopBottomCursorFaces(List<Vector3> vertices, string name, int direction, DualGridElement blockPart, Transform parent, int queue, int colliderFace)
    {
        int numberOfParts = vertices.Count / 2;

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = GenerateTriangles(numberOfParts)
        };
        mesh.RecalculateBounds();

        GameObject cursorTopBottomFaceObject = new GameObject("Cursor_" + name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BlockColliderSequences));
        cursorTopBottomFaceObject.transform.parent = parent;
        cursorTopBottomFaceObject.GetComponent<MeshFilter>().mesh = mesh;

        SetMaterialBasedOnColliderFaceHoverState(cursorTopBottomFaceObject, colliderFace, queue);

        cursorTopBottomFaceObject.GetComponent<BlockColliderSequences>().Queue = queue;
        cursorTopBottomFaceObject.layer = LayerMask.NameToLayer("Cursor");
    }

    private int[] GenerateTriangles(int numberOfParts)
    {
        List<int> triangles = new List<int>();

        for (int i = 0; i < numberOfParts; i++)
        {
            triangles.Add(i);
            triangles.Add(i + numberOfParts);
            triangles.Add((i - 1 + numberOfParts) % numberOfParts + numberOfParts);
        }

        for (int i = 0; i < numberOfParts - 2; i++)
        {
            triangles.Add(numberOfParts);
            triangles.Add(numberOfParts + i + 1);
            triangles.Add(numberOfParts + i + 2);
        }

        return triangles.ToArray();
    }

    public List<Vector3> CalculateVerticalOffsets(List<Vector3> blockOutlinePoints, float y)
    {
        Vector3 heightOffset = Vector3.up * y;
        List<Vector3> verticalBlockOutlinePoints = new List<Vector3>();

        foreach (Vector3 blockOutlinePoint in blockOutlinePoints)
        {
            verticalBlockOutlinePoints.Add(blockOutlinePoint + heightOffset);
        }
        return verticalBlockOutlinePoints;
    }

    private void SetMaterialBasedOnColliderFaceHoverState(GameObject gridPartElement, int colliderFace, int faceIndex)
    {
        MeshRenderer meshRenderer = gridPartElement.GetComponent<MeshRenderer>();
        if (colliderFace == faceIndex && colliderFace != -1) // if the current collider face is the same as the current cursor face (aka if we are currently hovering over the collider face with a cursor)
        {
            // The MeshRenderer component is responsible for rendering a mesh in the scene using a material.
            // It works in conjunction with the MeshFilter component, which holds the mesh data.
            meshRenderer.material = cursor_material_dark;
        }
        else
        {
            meshRenderer.material = cursor_material;
        }
    }
}

public class DualGridElementNeighbor
{
    public Vector3 selfPoint; // vertex point of the target block/cube part neighbour (aka edgePoint)
    public Vertex sideDirection; // direction of the neighbour (aka correspondingPrimalVertex)
}

public class DualGridElement // Represents the data structure of a target block/cube part in the grid (e.g. sides, top, bottom faces obtained from the cube's/block's corner and middle vertices)
{
    public DualGridElementNeighbor[] neighbours = new DualGridElementNeighbor[2];
    public Vector3 faceCentroid;
    public Vertex targetVertex;
    public float targetY;
}


// Collider classes for the block module
public class BlockColliderSides : MonoBehaviour
{
    public Vertex SideDirectionVertex;
    public Vertex TargetVertex;
    public float TargetY;
}
public class BlockColliderTopBottom : MonoBehaviour
{
    public int TopOrBottomDirection; //up: 1, down: -1
    public Vertex TargetVertex;
    public float TargetY;
}

public class BlockColliderSequences : MonoBehaviour
{
    public int Queue;
}
