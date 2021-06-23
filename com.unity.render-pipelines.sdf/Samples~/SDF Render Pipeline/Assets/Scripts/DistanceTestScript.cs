using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTestScript : MonoBehaviour
{
    public GameObject Ball;
    public GameObject Triangle;
    public Material VertexMarkerMaterial;

    Mesh triangleMesh;
    GameObject[] markers = new GameObject[3];

    void Start()
    {
        triangleMesh = Triangle.GetComponent<MeshFilter>().mesh;

        for(int i = 0; i < 3; ++i)
        {
            markers[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markers[i].name = "v" + (i+1).ToString();

            float scale = 0.1f;
            markers[i].transform.localScale = new Vector3(scale, scale, scale);
            markers[i].GetComponent<Renderer>().material = VertexMarkerMaterial;
            markers[i].GetComponent<Renderer>().material.SetColor(0, Color.red);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector4 ov1 = new Vector4(triangleMesh.vertices[0].x, triangleMesh.vertices[0].y, triangleMesh.vertices[0].z, 1.0f);
        Vector4 ov2 = new Vector4(triangleMesh.vertices[1].x, triangleMesh.vertices[1].y, triangleMesh.vertices[1].z, 1.0f);
        Vector4 ov3 = new Vector4(triangleMesh.vertices[2].x, triangleMesh.vertices[2].y, triangleMesh.vertices[2].z, 1.0f);

        Matrix4x4 objectToWorldMatrix = Triangle.transform.localToWorldMatrix;
        Vector4 tv1 = objectToWorldMatrix * ov1;
        Vector4 tv2 = objectToWorldMatrix * ov2;
        Vector4 tv3 = objectToWorldMatrix * ov3;

        Vector3 v1 = new Vector3(tv1.x, tv1.y, tv1.z);
        Vector3 v2 = new Vector3(tv2.x, tv2.y, tv2.z);
        Vector3 v3 = new Vector3(tv3.x, tv3.y, tv3.z);

        markers[0].transform.position = v1;
        markers[1].transform.position = v2;
        markers[2].transform.position = v3;

        float signedDistance = CalculateSignedDistance(v1, v2, v3, Ball.transform.position);
    }
    public static Vector3 GetNormalOfTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 AB = b - a;
        Vector3 AC = c - a;

        Vector3 normal = Vector3.Cross(AB, AC);
        normal.Normalize();

        return normal;
    }

    public static float GetAreaOfTriangle(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 AB = B - A;
        Vector3 BC = C - B;

        Vector3 vecNormal = GetNormalOfTriangle(A, B, C);

        // The cross product is equal to the area of a parallelogram
        // which is the base * height of one of the triangles that
        // makes up the parallelogram.
        Vector3 vecCross = Vector3.Cross(AB, BC);
        float fArea = Vector3.Dot(vecCross, vecNormal) / 2.0f;

        return fArea;
    }

    //--------------------------------------------------------------------------------------
    public static Vector3 GetTriangleBarycentricCoordinate(Vector3 A, Vector3 B, Vector3 C, Vector3 P)
    {
        float t = GetAreaOfTriangle(A, B, C);
        float t1 = GetAreaOfTriangle(P, B, C) / t;
        float t2 = GetAreaOfTriangle(A, P, C) / t;
        float t3 = GetAreaOfTriangle(P, A, B) / t;

        return new Vector3(t1, t2, t3);

    }

    float CalculateSignedDistance(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p)
    {
        float dist = float.MaxValue;

        Vector3 triangleNormal = GetNormalOfTriangle(v1, v2, v3);

        Vector3 b = GetTriangleBarycentricCoordinate(v1, v2, v3, p);
        Vector3 projectedPoint = (b.x * v1) + (b.y * v2) + (b.z * v3);
        dist = Vector3.Distance(p, projectedPoint);

        Vector3 v1ToP = p - v1;
        v1ToP.Normalize();

        if (Vector3.Dot(v1ToP, triangleNormal) < 0)
            dist = -dist;

        Debug.Log(dist);

        return dist;
    }
}
