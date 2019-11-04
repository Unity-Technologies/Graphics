using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(MeshFilter))]
public class GridMesh : MonoBehaviour
{
    [SerializeField] private int m_Rows = 1;
    [SerializeField] private int m_Cols = 1;
    [SerializeField] private Vector2 m_CellSize = Vector2.one;
    [SerializeField] private float m_Thickness = 0.1f;

    [System.NonSerialized]
    Mesh            mesh;

    private void OnEnable()
    {
        mesh = new Mesh();
        GetComponent< MeshFilter >().sharedMesh = mesh;
        GenerateMesh(mesh, m_Rows, m_Cols, m_CellSize, m_Thickness);
    }

    static void GenerateMesh(Mesh destination, int rows, int cols, Vector2 cellSize, float thickness)
    {
        // Grid generation
        //
        // Consider the grid with one more row and column.
        // Let's name 'outer cells' the cells that lies in that extra row or column, and 'inner cells' the others.
        //
        // Then all inner cells have 3 quads: Left, Top left and Top.
        // Most outer cells have only 2 quads:
        //  - bottom cells: Top left and Top
        //  - right cells: Top Left and Left
        //  - the bottom right outer cell: only Top Left
        //
        // So you have:
        //  - 4 vertice per cell
        //  - 6 triangles per inner cell
        //  - 4 triangles per outer cell (except the bottom right one, which has 2 triangles)

        const int verticePerCell = 4;
        const int trianglesPerInnerCell = 6;
        const int trianglesPerRightOuterCell = 4;
        const int trianglesPerBottomOuterCell = 4;
        const int trianglesPerBottomRightOuterCell = 2;

        var verticeCount = verticePerCell * ((rows + 1) * (cols + 1));
        var triangleCount = trianglesPerInnerCell * (rows * cols)
                            + trianglesPerRightOuterCell * rows
                            + trianglesPerBottomOuterCell * cols
                            + trianglesPerBottomRightOuterCell;

        destination.Clear();
        var vertices = new Vector3[verticeCount];
        var normals = new Vector3[verticeCount];
        var triangles = new int[triangleCount * 3];

        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.forward;

        //
        // Vertices
        //

        // Quads per cells
        for (int r = 0; r < rows + 1; r++)
        {
            for (int c = 0; c < cols + 1; c++)
            {
                var cellIndex = r * (cols + 1) + c;
                var cellPosition = new Vector3(c * cellSize.y, r * cellSize.x, 0);

                var verticeStart = cellIndex * verticePerCell;
                vertices[verticeStart + 0] = cellPosition + new Vector3(0, 0, 0);
                vertices[verticeStart + 1] = cellPosition + new Vector3(thickness, 0, 0);
                vertices[verticeStart + 2] = cellPosition + new Vector3(thickness, thickness, 0);
                vertices[verticeStart + 3] = cellPosition + new Vector3(0, thickness, 0);
            }
        }

        //
        // Triangles
        //

        // Inner cells
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cellPosition = new Vector3(c * cellSize.y, r * cellSize.x, 0);

                var verticeStart = (r * (cols + 1) + c) * verticePerCell;
                var triangleStart = (r * cols + c) * trianglesPerInnerCell * 3;
                triangles[triangleStart + 0 + 0] = verticeStart + 0;
                triangles[triangleStart + 0 + 1] = verticeStart + 2;
                triangles[triangleStart + 0 + 2] = verticeStart + 1;
                triangles[triangleStart + 3 + 0] = verticeStart + 0;
                triangles[triangleStart + 3 + 1] = verticeStart + 3;
                triangles[triangleStart + 3 + 2] = verticeStart + 2;

                var verticeStartBottom = ((r + 1) * (cols + 1) + c) * verticePerCell;
                triangles[triangleStart + 6 + 0] = verticeStart + 3;
                triangles[triangleStart + 6 + 1] = verticeStartBottom + 1;
                triangles[triangleStart + 6 + 2] = verticeStart + 2;
                triangles[triangleStart + 9 + 0] = verticeStart + 3;
                triangles[triangleStart + 9 + 1] = verticeStartBottom + 0;
                triangles[triangleStart + 9 + 2] = verticeStartBottom + 1;

                var verticeStartRight = (r * (cols + 1) + c + 1) * verticePerCell;
                triangles[triangleStart + 12 + 0] = verticeStart + 1;
                triangles[triangleStart + 12 + 1] = verticeStartRight + 3;
                triangles[triangleStart + 12 + 2] = verticeStartRight + 0;
                triangles[triangleStart + 15 + 0] = verticeStart + 1;
                triangles[triangleStart + 15 + 1] = verticeStart + 2;
                triangles[triangleStart + 15 + 2] = verticeStartRight + 3;
            }
        }

        var triangleOuterBottomStart = rows * cols * trianglesPerInnerCell * 3;
        // Outer bottom cells
        for (int c = 0; c < cols; c++)
        {
            var r = rows;
            var cellPosition = new Vector3(c * cellSize.y, r * cellSize.x, 0);

            var verticeStart = (r * (cols + 1) + c) * verticePerCell;
            var triangleStart = triangleOuterBottomStart + c * trianglesPerBottomOuterCell * 3;

            triangles[triangleStart + 0 + 0] = verticeStart + 0;
            triangles[triangleStart + 0 + 1] = verticeStart + 2;
            triangles[triangleStart + 0 + 2] = verticeStart + 1;
            triangles[triangleStart + 3 + 0] = verticeStart + 0;
            triangles[triangleStart + 3 + 1] = verticeStart + 3;
            triangles[triangleStart + 3 + 2] = verticeStart + 2;

            var verticeStartRight = (r * (cols + 1) + c + 1) * verticePerCell;
            triangles[triangleStart + 6 + 0] = verticeStart + 1;
            triangles[triangleStart + 6 + 1] = verticeStartRight + 3;
            triangles[triangleStart + 6 + 2] = verticeStartRight + 0;
            triangles[triangleStart + 9 + 0] = verticeStart + 1;
            triangles[triangleStart + 9 + 1] = verticeStart + 2;
            triangles[triangleStart + 9 + 2] = verticeStartRight + 3;
        }

        var triangleOuterBottomRightStart = triangleOuterBottomStart + trianglesPerBottomOuterCell * 3 * cols;
        // Bottom right cell
        {
            var verticeStart = (rows * (cols + 1) + cols) * verticePerCell;
            var triangleStart = triangleOuterBottomRightStart;

            triangles[triangleStart + 0 + 0] = verticeStart + 0;
            triangles[triangleStart + 0 + 1] = verticeStart + 2;
            triangles[triangleStart + 0 + 2] = verticeStart + 1;
            triangles[triangleStart + 3 + 0] = verticeStart + 0;
            triangles[triangleStart + 3 + 1] = verticeStart + 3;
            triangles[triangleStart + 3 + 2] = verticeStart + 2;
        }

        var triangleOuterRightStart = triangleOuterBottomRightStart + trianglesPerBottomRightOuterCell * 3;
        // Outer right cells
        for (int r = 0; r < rows; r++)
        {
            var c = cols;
            var cellPosition = new Vector3(c * cellSize.y, r * cellSize.x, 0);

            var verticeStart = (r * (cols + 1) + c) * verticePerCell;
            var triangleStart = triangleOuterRightStart + r * trianglesPerRightOuterCell * 3;

            triangles[triangleStart + 0 + 0] = verticeStart + 0;
            triangles[triangleStart + 0 + 1] = verticeStart + 2;
            triangles[triangleStart + 0 + 2] = verticeStart + 1;
            triangles[triangleStart + 3 + 0] = verticeStart + 0;
            triangles[triangleStart + 3 + 1] = verticeStart + 3;
            triangles[triangleStart + 3 + 2] = verticeStart + 2;

            var verticeStartBottom = ((r + 1) * (cols + 1) + c) * verticePerCell;
            triangles[triangleStart + 6 + 0] = verticeStart + 3;
            triangles[triangleStart + 6 + 1] = verticeStartBottom + 1;
            triangles[triangleStart + 6 + 2] = verticeStart + 2;
            triangles[triangleStart + 9 + 0] = verticeStart + 3;
            triangles[triangleStart + 9 + 1] = verticeStartBottom + 0;
            triangles[triangleStart + 9 + 2] = verticeStartBottom + 1;
        }

        destination.vertices = vertices;
        destination.triangles = triangles;

        destination.Optimize();
        destination.RecalculateNormals();
        destination.RecalculateTangents();
        destination.RecalculateBounds();
    }

    void OnValidate()
    {
        if (Application.isPlaying || mesh == null)
            return;

        m_Rows = Mathf.Max(1, m_Rows);
        m_Cols = Mathf.Max(1, m_Cols);
        m_CellSize = Vector2.Max(Vector2.zero, m_CellSize);
        m_Thickness = Mathf.Min(Mathf.Max(0, m_Thickness), Mathf.Min(m_CellSize.x, m_CellSize.y));

        // var meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

        // var mesh = meshFilter.sharedMesh != null ? Instantiate(meshFilter.sharedMesh) : new Mesh();
        GenerateMesh(mesh, m_Rows, m_Cols, m_CellSize, m_Thickness);
        // meshFilter.mesh = mesh;
    }
}
