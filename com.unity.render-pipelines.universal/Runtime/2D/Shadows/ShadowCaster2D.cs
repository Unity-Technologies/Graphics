using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowCaster2D : MonoBehaviour
{
    public float m_Radius = 1;
    public MeshFilter m_DebugMeshFilter;

    public enum CasterType
    {
        Capsule,
        Polygon
    }

    Mesh m_ShadowMesh;

    void CreateShadowPolygon(Vector3 position, float radius, float angle, int sides, ref Mesh mesh)
    {
        if (mesh == null)
            mesh = new Mesh();

        float angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
        if (sides < 3)
        {
            radius = 0.70710678118654752440084436210485f * radius;
            sides = 4;
        }

        if (sides == 4)
        {
            angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
        }

        Vector3[] vertices;
        Vector4[] tangents;
        int[] triangles;
        Color[] colors;

        int extraTriangles = sides;
        int extraVertices = sides;

        vertices = new Vector3[1 + sides + extraVertices];
        tangents = new Vector4[1 + sides + extraVertices];
        triangles = new int[3 * (sides + extraTriangles)];

        colors = new Color[1 + sides + extraVertices];

        int centerIndex = sides + extraVertices;
        int lastVertexIndex = 0;

        vertices[centerIndex] = position;
        tangents[centerIndex] = Vector4.zero;
        colors[centerIndex] = Color.black;
        float radiansPerSide = 2 * Mathf.PI / sides;
        Vector3 lastEndPoint = radius * new Vector3(Mathf.Cos(angleOffset), Mathf.Sin(angleOffset), 0); ;
        for (int i = 0; i < sides; i++)
        {
            float endAngle = (i + 1) * radiansPerSide;
            float nextEndAngle = (i + 2) * radiansPerSide;
            Vector3 endPoint = radius * new Vector3(Mathf.Cos(endAngle + angleOffset), Mathf.Sin(endAngle + angleOffset), 0); ;
            Vector3 nextEndPoint = radius * new Vector3(Mathf.Cos(nextEndAngle + angleOffset), Mathf.Sin(nextEndAngle + angleOffset), 0); ;

            Vector3 curCross = -Vector3.Normalize(Vector3.Cross((endPoint - lastEndPoint), Vector3.forward));
            Vector3 nextCross = -Vector3.Normalize(Vector3.Cross((nextEndPoint - endPoint), Vector3.forward));

            // Create triangle
            int vertexIndex;
            
            vertexIndex = (i + 1) % (sides);
            vertices[vertexIndex] = endPoint;
            tangents[vertexIndex] = nextCross;
            colors[vertexIndex] = Color.black;
            Debug.DrawLine(endPoint, endPoint + 0.2f * nextCross, Color.blue, 60);

            int triangleIndex = 3 * i;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = lastVertexIndex;
            triangles[triangleIndex + 2] = centerIndex;

            // Create extra shadow triangle
            int extraVertexIndex = vertexIndex + sides;
            vertices[extraVertexIndex] = endPoint;
            tangents[extraVertexIndex] = curCross;
            colors[extraVertexIndex] = Color.black;
            Debug.DrawLine(endPoint, endPoint + 0.2f *curCross, Color.red, 60);

            int extraTriangleIndex = 3 * (i + sides);
            triangles[extraTriangleIndex] = vertexIndex;
            triangles[extraTriangleIndex + 1] = lastVertexIndex;
            triangles[extraTriangleIndex + 2] = extraVertexIndex;

            lastEndPoint = endPoint;
            lastVertexIndex = vertexIndex;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.tangents = tangents;
        mesh.colors = colors;
    }


    private void Start()
    {
        CreateShadowPolygon(Vector3.zero, m_Radius, 0, 6, ref m_ShadowMesh);
        m_DebugMeshFilter.sharedMesh = m_ShadowMesh;
    }


}
