using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using JetBrains.Annotations;
using SystemRandom = System.Random;
using Random = UnityEngine.Random;
using System;

public class Grid
{
    public List<Vertex> Vertices = new List<Vertex>();
    public List<CubeVertex> CubeVertices = new List<CubeVertex>();

    public List<Edge> Edges = new List<Edge>();
    public List<Triangle> Triangles = new List<Triangle>();
    public List<Quad> Quads = new List<Quad>();
    public List<SubQuad> SubQuads = new List<SubQuad>();
    public List<Cube> Cubes = new List<Cube>();

    public LineRenderer LineRenderer;

    public int Floor;
    public float Height;

    public int ringCount;
    public float ringRadius;
    public int relaxationIterations = 500;

    public Grid(int ringCount, float ring_radius, int floor, float height)
    {
        this.Floor = floor;
        this.Height = height;
        this.ringCount = ringCount;
        this.ringRadius = ring_radius;

        GenerateHexGridVertices(ringCount, ring_radius);
        TriangulateHexGridVertices(ringCount, ring_radius);
        MergeAndSubdivideTrianglesIntoQuads();
        RelaxLaplacian(); // This method relaxes the vertices of the subquads to create a square-like grid.
        GenerateCubesOnGrid(); // This method generates the cube/block grid based on the subquads. Using cubes/blocks allows us to represent the 3D structure of the grid.
    }

    public Vector3 LinearInterpolate(Vector3 start, Vector3 end, float t)
    {
        return start + (end - start) * t; // returns interpolated vertex position between start and end points
    }

    private Vector3 GetHexPointByRing(float radius, int point_i)
    {
        float angle = -Mathf.PI / 2f + Mathf.PI / 3f * point_i;
        float x = radius * Mathf.Cos(angle);
        float z = radius * Mathf.Sin(angle);
        return new Vector3(x, 0, z);
    }

    public void GenerateHexGridVertices(int ring_count, float ring_radius)
    {
        Vertices.Add(new Vertex(Vector3.zero, 0));

        for (int ring_i = 1; ring_i <= ring_count - 1; ring_i++)
        {
            float r = ring_radius * ring_i;
            for (int point_i = 0; point_i < 6; point_i++) // Main hex angle points.
            {
                Vector3 start_point = GetHexPointByRing(r, point_i);
                Vector3 end_point = GetHexPointByRing(r, point_i + 1);

                // Add the start point
                if (Vertices.Contains(new Vertex(start_point, ring_i))) continue;
                Vertices.Add(new Vertex(start_point, ring_i));

                // Add interpolated points
                float side_step = 1f / ring_i; // interpolate between start and end point
                for (int side_point_i = 1; side_point_i < ring_i; side_point_i++) // start from 1 to skip ring 1 because we don't need to interpolate the first ring
                {
                    Vector3 interpolated_point = LinearInterpolate(start_point, end_point, side_step * side_point_i);

                    if (interpolated_point == start_point) continue;
                    if (interpolated_point == end_point) continue;

                    Vertices.Add(new Vertex(interpolated_point, ring_i));
                }
            }
        }
    }

    private Edge AddEdge(Vertex pointA, Vertex pointB, bool checkBoundaryRing = false, int ringCount = 6)
    {
        Edge edge = new Edge(pointA, pointB);
        Edge existingEdge = LookupEdge(edge); // check if the new edge already exists in the (general) list of Edges

        if (existingEdge == null) // if the edge doesn't exist in the list of edges, then add it
        {
            edge = new Edge(pointA, pointB);
            if (checkBoundaryRing && pointB.RingIndex == ringCount - 1) // second point might be on the boundary ring of the hex grid
            {
                edge.isBoundaryRing = true;
            }
            Edges.Add(edge);
            pointA.Edges.Add(edge);
            pointB.Edges.Add(edge);
            return edge;
        }
        return existingEdge;
    }

    public void TriangulateHexGridVertices(int ring_count, float ring_radius)
    {
        for (int ring_i = 1; ring_i < ring_count - 1; ring_i++) // (ring_count - 1) because we don't want to triangulate the last ring
        {
            List<Vertex> ringVertices = Vertices.FindAll(vertex => vertex.RingIndex == ring_i);

            foreach (Vertex firstHexPoint in ringVertices) // acts like a 'center' point (origin) for the hexagon ring
            {
                for (int point_i = 0; point_i < 6; point_i++)
                {
                    // Triangle has points a, b, c in that order
                    Vector3 pos_b = GetHexPointByRing(ring_radius, point_i);
                    Vertex secondHexPoint = LookupVertexByPoint(firstHexPoint.point + pos_b);
                    Vector3 pos_c = GetHexPointByRing(ring_radius, point_i + 1);
                    Vertex thirdHexPoint = LookupVertexByPoint(firstHexPoint.point + pos_c);

                    Edge ab_edge = AddEdge(firstHexPoint, secondHexPoint);
                    Edge bc_edge = AddEdge(secondHexPoint, thirdHexPoint, true, ring_count);
                    Edge ca_edge = AddEdge(thirdHexPoint, firstHexPoint);

                    List<Edge> newTriangleEdges = new List<Edge> { ab_edge, bc_edge, ca_edge };
                    Triangle newTriangle = new Triangle(newTriangleEdges, firstHexPoint, secondHexPoint, thirdHexPoint);
                    // check if the new triangle already exists in the (general) list of Triangles
                    bool isExistingTriangle = Triangles.Any(existingTriangle => newTriangle.Edges.All(existingTriangle.Edges.Contains)); // checks if all edges of the newTriangle are contained within the existingTriangle.
                    if (isExistingTriangle) continue;

                    ab_edge.Triangles.Add(newTriangle);
                    bc_edge.Triangles.Add(newTriangle);
                    ca_edge.Triangles.Add(newTriangle);
                    Triangles.Add(newTriangle);
                }
            }
        }
    }

    private void Shuffle<T>(List<T> list, SystemRandom rnd)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private Quad RemoveEdge(Triangle triangle, Edge targetEdge, List<Triangle> adjacentTriangles)
    {
        Edges.Remove(targetEdge);
        foreach (Vertex vertex in targetEdge.Vertices)
        {
            vertex.Edges.Remove(targetEdge);
        }

        List<Edge> quadEdges = new List<Edge>();
        foreach (Triangle adjacentTriangle in adjacentTriangles)
        {
            adjacentTriangle.Edges.Remove(targetEdge);
            adjacentTriangle.isMergedIntoQuad = true;
            quadEdges.AddRange(adjacentTriangle.Edges);
        }
        Vertex a = triangle.Vertices[0];
        Vertex b = triangle.Vertices[1];
        Vertex c = triangle.Vertices[2];

        Vertex d = quadEdges.SelectMany(e => e.Vertices).First(v => v != a && v != b && v != c);

        quadEdges = quadEdges.Distinct().ToList(); // Remove duplicate edges that may have been added from both triangles

        Quad quad = new Quad(quadEdges, a, b, c, d); // create a four sided quad from the two triangles

        return quad;
    }


    // Create quads by merging neighboring triangles
    // the easiest way is to randomly pick an edge then
    // use its neighbouring triangles to quickly determine if there are 
    // two triangles that share it (the selected edge). When all possible
    // quads have been made into faces, any remaining unused
    // triangles will be turned into faces too.
    // Build faces (quads or leftover triangles-as-quads):
    public void MergeAndSubdivideTrianglesIntoQuads()
    {
        SystemRandom rnd = new SystemRandom(1234);
        Random.InitState(1234);

        Shuffle(Triangles, rnd); // Shuffle the list of triangles to randomize the order of triangles to be merged into quads (to avoid bias)

        foreach (Triangle triangle in Triangles)
        {
            if (triangle.isMergedIntoQuad) continue; // Skip triangles that are already merged into a quad

            bool isQuadFormed = false; // Tracks if we successfully formed a quad from the current triangle
            for (int attempt = 0; attempt < 3 && !isQuadFormed; attempt++)
            {
                Edge targetEdge = triangle.Edges[Random.Range(0, 3)];
                if (targetEdge.isBoundaryRing) continue; // Skip boundary edges

                List<Triangle> targetEdgeAdjacentTriangles = targetEdge.Triangles; // neighboring triangles of the target edge
                bool canMergeTrianglesIntoQuad = targetEdgeAdjacentTriangles.All(t => !t.isMergedIntoQuad);

                if (canMergeTrianglesIntoQuad)
                {
                    Quad quad = RemoveEdge(triangle, targetEdge, targetEdgeAdjacentTriangles);
                    Quads.Add(quad);
                    SubdivideQuad(quad);
                    isQuadFormed = true;
                }
            }

            // Handle triangles that couldn't form a quad, ensuring no missing edges (this should cleanup the left overs)
            if (!isQuadFormed)
            {
                List<Edge> boundaryEdges = new List<Edge>(triangle.Edges);

                Vertex a = triangle.Vertices[0];
                Vertex b = triangle.Vertices[1];
                Vertex c = triangle.Vertices[2];

                Quad quad = new Quad(boundaryEdges, a, b, c); // if not merged into a quad, then create a 3-sided quad from the triangle
                Quads.Add(quad);
                SubdivideQuad(quad);
                triangle.isMergedIntoQuad = true;
            }
        }
    }

    // Subdivide all the Quad & Triangle faces of a topology
    // into a new topology that will only consist of quads (4-sided faces).
    // Subdivide all faces into subquads:
    private void SubdivideQuad(Quad quad) // Makes sure that all 3-sided (including 4-sided) quads are subdivided into 4-sided subquads so that there are no 3-sided quads left.
    {
        Vector3 quadCentroidPoint = quad.GetCentroid();
        Vertex quadCentroidVertex = new Vertex(quadCentroidPoint, 0);
        quad.Subdivide(this, quadCentroidVertex, ringCount);  // "this" is the Grid object

        // We sort the vertices of the quad in a clockwise order around the centroid to maintain the geometric integrity of the quad. Ensures vertices are processed in a predictable order. Prevents the creation of non-convex shapes or overlapping edges.
        List<Vertex> sortedQuadVertices = VertexClockwiseSort(quad.Edges);
        List<Edge> sortedQuadEdges = OrderEdgesByVertices(quad.Edges, sortedQuadVertices);

        for (int i = 0; i < sortedQuadEdges.Count; i++)
        {
            Vertex[] subQuadVertices = new Vertex[4];

            Edge currentEdge = sortedQuadEdges[i];
            Edge previousEdge = sortedQuadEdges[(i - 1 + sortedQuadEdges.Count) % sortedQuadEdges.Count];

            Vertex currentEdgeCenterVertex = LookupVertexByPoint(currentEdge.GetCenter());
            Vertex previousEdgeCenterVertex = LookupVertexByPoint(previousEdge.GetCenter());

            // We assign vertices in a specific order to maintain the geometric integrity 
            // of the quad and ensure that the resulting sub-quads are correctly formed.
            subQuadVertices[0] = sortedQuadVertices[i];
            subQuadVertices[1] = currentEdgeCenterVertex;
            subQuadVertices[2] = quadCentroidVertex;
            subQuadVertices[3] = previousEdgeCenterVertex;

            SubQuad subQuad = CreateSubQuad(subQuadVertices);
            SubQuads.Add(subQuad);
        }
    }

    // This is called Laplacian smoothing, which is a common technique used in computer graphics to smooth out meshes and create more organic-looking shapes.
    // We move each vertex in the subquad toward the average of neighbours to create a more relaxed and natural-looking grid. This process is repeated multiple times to achieve the desired level of smoothness.
    // Before: perfect geometric grid.
    // After: organic/slightly distorted grid.
    // Outer boundary stays fixed so that the shape is preserved
    // Moves points → breaks symmetry → organic look
    private void RelaxLaplacian() // Gives a more organic look to the grid by moving the vertices towards the average position of their neighbors.
    {
        float t = 0.5f; // interpolation factor. 0.5 is the midpoint of the edge between a and b points.
        for (int i = 0; i < 50; i++)
        {
            foreach (SubQuad subQuad in SubQuads)
            {
                // Move each vertex towards the average position of its neighbors to create a square-like grid
                foreach (Vertex vertex in subQuad.Vertices)
                {
                    if (vertex.RingIndex == ringCount - 1) continue; // Skip boundary vertices

                    Vector3 force = Vector3.zero;
                    foreach (Edge edge in vertex.Edges)
                    {
                        Vertex otherVertex = (edge.a == vertex) ? edge.b : edge.a;
                        force += otherVertex.point;
                    }
                    force /= vertex.Edges.Count; // Average position of neighboring vertices

                    vertex.point = LinearInterpolate(vertex.point, force, t); // Move the vertex towards the average position of its neighbors.
                }
            }
        }

        foreach (Vertex vertex in Vertices)
        {
            if (vertex.RingIndex == ringCount - 1)
            {
                vertex.IsBoundary = true;
            }
        }
    }

    // While our primary (2D) grid is an irregular organic hexagon grid, the (3D) cube/block grid can serve 
    // as a useful extension for representing additional 3D data, enhancing visualizations, 
    // and supporting complex calculations or simulations. By integrating cubes/blocks, you can add a 
    // vertical dimension to your hexagon grid, enabling richer and more detailed modeling of your environment.
    public void GenerateCubesOnGrid()
    {
        foreach (SubQuad subQuad in SubQuads)
        {
            for (int floor_level = 0; floor_level < Floor; floor_level++)
            {
                Cube cube = new Cube(subQuad, floor_level, Height);
                Cubes.Add(cube);

                cube.Vertices.ForEach(cube_v => cube_v.SubQuad = subQuad); // assign a subquad reference for each cube/block vertex
                CubeVertices.AddRange(cube.Vertices);
            }
        }

        foreach (CubeVertex cubeVertex in CubeVertices) // Flags the vertices of the cubes/blocks that are on the boundary
        {
            if (cubeVertex.Y == Floor * Height) // if the vertex is on the lower/bottom floor level (bottom/lower face of the cube/block)
            {
                cubeVertex.IsBoundary = true;
            }
        }
    }

    public Vertex LookupVertexByPoint(Vector3 inputPoint)
    {
        foreach (Vertex v in Vertices)
        {
            if (v.point == inputPoint)
                return v;
        }
        return null;
        // return Vertices.Find(x => x.point == point);
    }

    public Edge LookupEdge(Edge inputEdge)
    {
        foreach (Edge e in Edges)
        {
            if (e.Vertices.Contains(inputEdge.Vertices[0]) &&
                e.Vertices.Contains(inputEdge.Vertices[1]))
                return e;
        }
        return null;
        // return Edges.Find(e => e.Vertices.All(edge.Vertices.Contains));
    }


    [CanBeNull]
    public List<CubeVertex> GetCubeVertices(Vertex targetVertex, float y)
    {
        Vector3 height_offset = Vector3.up * y;
        Vector3 targetCubePoint = targetVertex.point + height_offset;

        List<CubeVertex> cubeVertices = new List<CubeVertex>();
        foreach (CubeVertex existingCubeVertex in CubeVertices)
        {
            if (existingCubeVertex.Vertex == targetVertex
                && existingCubeVertex.worldPoint == targetCubePoint)
            {
                cubeVertices.Add(existingCubeVertex);
            }
        }
        return cubeVertices;
    }

    [CanBeNull]
    public List<CubeVertex> GetCubeVertices(Vector3 targetCubePoint)
    {
        List<CubeVertex> cubeVertices = new List<CubeVertex>();
        foreach (CubeVertex existingCubeVertex in CubeVertices)
        {
            if (existingCubeVertex.Vertex == LookupVertexByPoint(targetCubePoint)
                && existingCubeVertex.worldPoint == targetCubePoint)
            {
                cubeVertices.Add(existingCubeVertex);
            }
        }
        return cubeVertices;
    }

    public List<Vector3> GetQuarterSubQuad(CubeVertex cubeVertices, out List<Vertex> neighbours)
    {
        SubQuad subQuad = cubeVertices.SubQuad;
        var (a, b, c, d, ab, bc, cd, da, centroid) = CalculateSubQuadPoints(subQuad, cubeVertices.Y);

        List<Vertex> directionNeighbours = new List<Vertex>();
        Vector3[] quarters = new Vector3[4];

        if (cubeVertices.Vertex == subQuad.Vertices[0])
        {
            quarters = new Vector3[4] { a, ab, centroid, da };
            directionNeighbours.Add(subQuad.Vertices[1]);
            directionNeighbours.Add(subQuad.Vertices[3]);
        }
        else if (cubeVertices.Vertex == subQuad.Vertices[1])
        {
            quarters = new Vector3[4] { b, bc, centroid, ab };
            directionNeighbours.Add(subQuad.Vertices[2]);
            directionNeighbours.Add(subQuad.Vertices[0]);
        }
        else if (cubeVertices.Vertex == subQuad.Vertices[2])
        {
            quarters = new Vector3[4] { c, cd, centroid, bc };
            directionNeighbours.Add(subQuad.Vertices[3]);
            directionNeighbours.Add(subQuad.Vertices[1]);
        }
        else if (cubeVertices.Vertex == subQuad.Vertices[3])
        {
            quarters = new Vector3[4] { d, da, centroid, cd };
            directionNeighbours.Add(subQuad.Vertices[0]);
            directionNeighbours.Add(subQuad.Vertices[2]);
        }

        neighbours = directionNeighbours; // returns the two adjacent neighbours of the cube subquad vertex
        return quarters.ToList();
    }

    public (Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 ab, Vector3 bc, Vector3 cd, Vector3 da, Vector3 centroid) CalculateSubQuadPoints(SubQuad subQuad, float y = 0)
    {
        Vector3 height_offset = Vector3.up * y;
        Vector3 a = subQuad.Vertices[0].point + height_offset;
        Vector3 b = subQuad.Vertices[1].point + height_offset;
        Vector3 c = subQuad.Vertices[2].point + height_offset;
        Vector3 d = subQuad.Vertices[3].point + height_offset;

        Vector3 ab = (a + b) / 2; // midpoint between a and b
        Vector3 bc = (b + c) / 2; // midpoint between b and c
        Vector3 cd = (c + d) / 2; // midpoint between c and d
        Vector3 da = (d + a) / 2; // midpoint between d and a
        Vector3 centroid = (a + b + c + d) / 4;

        return (a, b, c, d, ab, bc, cd, da, centroid);
    }

    public SubQuad CreateSubQuad(Vertex[] vertices)
    {
        List<Edge> subQuadEdges = new List<Edge>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Vertex a = vertices[i];
            Vertex b = vertices[(i + 1) % vertices.Length];
            Edge subQuadEdge = LookupEdge(new Edge(a, b));
            subQuadEdges.Add(subQuadEdge);
        }
        return new SubQuad(subQuadEdges);
    }

    public List<Edge> OrderEdgesByVertices(List<Edge> quadEdges, List<Vertex> sortedQuadVertices)
    {
        List<Edge> sortedQuadEdges = new List<Edge>();
        for (int i = 0; i < sortedQuadVertices.Count; i++)
        {
            Vertex currentSortedVertex = sortedQuadVertices[i];
            Vertex nextSortedVertex = sortedQuadVertices[(i + 1) % sortedQuadVertices.Count];

            foreach (Edge quadEdge in quadEdges)
            {
                Vector3 a = quadEdge.a.point;
                Vector3 b = quadEdge.b.point;

                if (currentSortedVertex.point == a && nextSortedVertex.point == b ||
                    currentSortedVertex.point == b && nextSortedVertex.point == a)
                {
                    sortedQuadEdges.Add(quadEdge);
                }
            }
        }
        return sortedQuadEdges;
    }

    // This method ensures that the vertices are sorted correctly and efficiently 
    // around the centroid, maintaining the geometric integrity of the quad.
    public List<Vertex> VertexClockwiseSort(List<Edge> quadEdges)
    {
        List<Vertex> quadVertices = new List<Vertex>();

        foreach (Edge quadEdge in quadEdges)
        {
            foreach (Vertex quadVertex in quadEdge.Vertices)
            {
                if (!quadVertices.Contains(quadVertex))
                {
                    quadVertices.Add(quadVertex);
                }
            }
        }

        Vector3 quadCentroid = new Vector3();
        foreach (Vertex quadVertex in quadVertices)
        {
            quadCentroid += quadVertex.point;
        }
        quadCentroid /= (float)quadVertices.Count;

        Vertex quadCentroidVertex = LookupVertexByPoint(quadCentroid);
        // Sort the vertices based on the angle to the centroid
        quadVertices.Sort((v1, v2) =>
        {
            // The initial approach of adding 360 is meant to handle negative angles 
            // and ensure all angles are within a positive range before taking the modulus 
            // to keep the values between 0 and 360 degrees.
            float xDiffVertex1 = v1.point.x - quadCentroidVertex.point.x;
            float zDiffVertex1 = v1.point.z - quadCentroidVertex.point.z;
            double angleA = (Mathf.Rad2Deg * Math.Atan2(xDiffVertex1, zDiffVertex1) + 360) % 360; // Normalization: The % 360 is to ensure the angle is between 0 and 360. The + 360 is to ensure the angle is positive.
            float xDiffVertex2 = v2.point.x - quadCentroidVertex.point.x;
            float zDiffVertex2 = v2.point.z - quadCentroidVertex.point.z;
            double angleB = (Mathf.Rad2Deg * Math.Atan2(xDiffVertex2, zDiffVertex2) + 360) % 360; // Math.Atan2(y, x) computes the angle between the positive x-axis and the line from the point (x, y) to the origin (0, 0). Whereas Math.Atan2(x, y) calculates the angle between the positive y-axis and the point (y, x).
            return angleA.CompareTo(angleB); // returns -1 if angleA is less than angleB, 0 if they are equal, and 1 if angleA is greater than angleB.
        });

        return quadVertices;
    }

    public void CreateGridLinesForTheSubQuad(SubQuad subQuad, LineRenderer lineRenderer)
    {
        lineRenderer.useWorldSpace = true;
        List<Vertex> subQuadVertices = subQuad.Vertices;
        lineRenderer.positionCount = subQuadVertices.Count; // 4 vertices for a subquad
        for (int i = 0; i < subQuadVertices.Count; i++)
        {
            lineRenderer.SetPosition(i, subQuadVertices[i].point); // sets the line positions to the vertices of the subquad
        }
    }
}