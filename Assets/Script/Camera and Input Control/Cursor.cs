using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum CursorOperation
{
    LeftClickToAddBlock,
    RightClickToRemoveBlock
}

public class Cursor : MonoBehaviour
{
    public GameObject currentCollidingGameObject;

    private Vertex previousTargetVertex;
    private float previousY;

    public Grid grid;

    public Material cursor_material;
    public Material cursor_material_dark;
    public BlockModuleCollider blockModuleCollider; // Block Module (aka Building Block Element)

    private List<DualGridElement> targetDualGridElements = new List<DualGridElement>();

    List<CubeVertex> cubeVertices = new List<CubeVertex>();


    // Start is called before the first frame update
    public void Start()
    {
        grid = GridManager.instance.grid;
        blockModuleCollider.InitializeGridProperties();
        previousTargetVertex = GetCursorTargetVertex(out previousY);
    }

    // Update is called once per frame
    public void Update()
    {
        targetDualGridElements.Clear();

        // In this section we make sure to avoid updating the grid element if the cursor is not on a valid target vertex due to the following reasons:
        // 1. The target vertex is null.
        // 2. The target vertex is a boundary vertex.
        // 3. The target vertex is not a boundary vertex but one of its cube vertices is a boundary vertex.
        float currentTargetY;
        Vertex currentTargetVertex = GetCursorTargetVertex(out currentTargetY);
        if (currentTargetVertex == null) return;

        cubeVertices = grid.GetCubeVertices(currentTargetVertex, currentTargetY);
        foreach (CubeVertex cubeVertex in cubeVertices)
        {
            if (cubeVertex.IsBoundary) return;
        }

        UpdateCursor(currentTargetVertex, currentTargetY);
        UpdateBlockModuleOnClick(currentTargetVertex, currentTargetY);
    }

    public Vertex GetCursorTargetVertex(out float y)
    {
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Create a RaycastHit variable to store information about what was hit by the ray.
        RaycastHit floorHit;

        // The length of the ray from the camera into the scene.
        float camRayLength = 10000f;

        // A layer mask so that a ray can be cast just at gameobjects on all the three collider layers.
        // Create a layer mask for the three collider layers.
        int floorMask = 1 << LayerMask.NameToLayer("GroundCollider");
        floorMask += 1 << LayerMask.NameToLayer("BlockModuleCollider_TopBottom");
        floorMask += 1 << LayerMask.NameToLayer("BlockModuleCollider_Sides");

        // Perform the raycast and if it hits something on the all three collider layers...
        // By creating a combined floorMask, you can efficiently check for collisions, 
        // raycasts, or other physics interactions that involve multiple layers simultaneously.
        if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
        {
            // This will only hit objects on the layers: "GridGroundCollider", "GridBuildingCubeCollider_TopBottom", "GridBuildingCubeCollider_Sides"
            currentCollidingGameObject = floorHit.transform.gameObject;
            if (currentCollidingGameObject.layer == LayerMask.NameToLayer("GroundCollider"))
            {
                Vector3 mousePosition = floorHit.point;
                SubQuad subQuad = currentCollidingGameObject.GetComponent<GroundColliderSubQuad>().SubQuad;
                Vertex targetVertex = GetSubQuadCornerTargetPoint(subQuad, mousePosition);

                y = 0f;
                return targetVertex;
            }

            if (currentCollidingGameObject.layer == LayerMask.NameToLayer("BlockModuleCollider_TopBottom")) // TB means Top and Bottom (upper and lower sides of the building)
            {
                Vertex targetVertex = currentCollidingGameObject.GetComponent<BlockColliderTopBottom>().TargetVertex;
                float targetY = currentCollidingGameObject.GetComponent<BlockColliderTopBottom>().TargetY;

                y = targetY;
                return targetVertex;
            }

            if (currentCollidingGameObject.layer == LayerMask.NameToLayer("BlockModuleCollider_Sides"))
            {
                Vertex targetVertex = currentCollidingGameObject.GetComponent<BlockColliderSides>().TargetVertex;
                float targetY = currentCollidingGameObject.GetComponent<BlockColliderSides>().TargetY;

                y = targetY;
                return targetVertex;
            }
        }
        y = 0f;
        return null;
    }

    public Vertex GetSubQuadCornerTargetPoint(SubQuad subQuad, Vector3 mousePos)
    {
        var (a, b, c, d, ab, bc, cd, da, centroid) = grid.CalculateSubQuadPoints(subQuad);

        Vector3[] quarterRegionA = new Vector3[4] { a, ab, centroid, da };
        Vector3[] quarterRegionB = new Vector3[4] { b, bc, centroid, ab };
        Vector3[] quarterRegionC = new Vector3[4] { c, cd, centroid, bc };
        Vector3[] quarterRegionD = new Vector3[4] { d, da, centroid, cd };

        if (IsMousePointInSubQuad(mousePos, quarterRegionA))
            return subQuad.Vertices[0];
        if (IsMousePointInSubQuad(mousePos, quarterRegionB))
            return subQuad.Vertices[1];
        if (IsMousePointInSubQuad(mousePos, quarterRegionC))
            return subQuad.Vertices[2];
        if (IsMousePointInSubQuad(mousePos, quarterRegionD))
            return subQuad.Vertices[3];

        return null;
    }

    public bool IsMousePointInSubQuad(Vector3 mousePos, Vector3[] subquadRegion)
    {
        bool pointInSubQuad = false;
        for (int i = 0, j = subquadRegion.Length - 1; i < subquadRegion.Length; j = i++)
        {
            if (((subquadRegion[i].z > mousePos.z) != (subquadRegion[j].z > mousePos.z)) &&
                (mousePos.x < (subquadRegion[j].x - subquadRegion[i].x) * (mousePos.z - subquadRegion[i].z) / (subquadRegion[j].z - subquadRegion[i].z) + subquadRegion[i].x))
                pointInSubQuad = !pointInSubQuad;
        }
        return pointInSubQuad;
    }


    public void UpdateCursor(Vertex targetVertex, float targetY)
    {
        // If the current target vertex (mouse pointer) is different from the previous target vertex,
        // we update the cube vertices since we (our mouse pointer) picked a new vertex in the grid.
        // We update the cube vertices according to the new target vertex and the y value (mouse pointer).
        if (previousTargetVertex != targetVertex || previousY != targetY)
        {
            previousTargetVertex = targetVertex;
            cubeVertices = grid.GetCubeVertices(targetVertex, targetY);
        }

        // When the mouse moves to a new vertex in the grid, we update the dark cursor 
        // to avoid the accumulation of dark cursors (dark cursors are created when the mouse moves to a new vertex).
        if (transform.childCount != 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        // If the target vertex is not null (i.e. we are mouse pointing at something (subquad) in the grid), 
        // we create a new grid element and cursor based on the target vertex and the y value.
        if (targetVertex != null)
        {
            // Creates/Assembles a structure data for the grid building cube element in the grid.
            targetDualGridElements = blockModuleCollider.AssembleDualGridElements(targetVertex, targetY);
            // Creates a cursor mesh on the current hovered target vertex in the grid (subquad).
            // We give Cursor transform as the parent of the cursor mesh we create.
            blockModuleCollider.CreateCursor(targetDualGridElements, cursor_material, cursor_material_dark, transform, currentCollidingGameObject);
        }
    }

    public void UpdateBlockModuleOnClick(Vertex targetVertex, float targetY)
    {
        GridManager.instance.SetTargetVertex(targetVertex, targetY);
        if (Input.GetMouseButtonDown(0)) // Add a block module on the grid
        {
            GridManager.instance.UpdateBlockModuleCollider(targetVertex, targetY, CursorOperation.LeftClickToAddBlock, currentCollidingGameObject);
        }
        // if (Input.GetMouseButtonDown(1)) // Remove a block module from the grid
        // {
        //     GridManager.instance.UpdateBlockModuleCollider(targetVertex, targetY, CursorOperation.RightClickToRemoveBlock, currentCollidingGameObject);
        // }
    }

    // private void OnDrawGizmos()
    // {
    //foreach (var vertex in vertex3)
    //{
    //    Gizmos.color = Color.red;

    //    Gizmos.DrawSphere(vertex.SubQuad.Vertices[0].point, 1f);
    //    Gizmos.DrawSphere(vertex.SubQuad.Vertices[1].point, 1f);
    //    Gizmos.DrawSphere(vertex.SubQuad.Vertices[2].point, 1f);
    //    Gizmos.DrawSphere(vertex.SubQuad.Vertices[3].point, 1f);
    //}


    //for (int i = 0; i < buildingElements.Count; i++)
    //{
    //    BuildingElement buildingElement = buildingElements[i];
    //    var a = grid.GetVertex3(buildingElement.Neighbour[0].Direction, 0);
    //    var b = grid.GetVertex3(buildingElement.Neighbour[1].Direction, 0);

    //    GUI.color = Color.yellow;
    //    Handles.Label(a[0].worldPoint, i.ToString());

    //}
    // }

}
