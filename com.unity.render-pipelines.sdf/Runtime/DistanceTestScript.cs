using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTestScript : MonoBehaviour
{
    public GameObject Ball;
    public GameObject ProjectedPoint;
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

        Vector3 projectPointPosition;
        float signedDistance = CalculateSignedDistance(v1, v3, v2, Ball.transform.position, out projectPointPosition);
        ProjectedPoint.transform.position = projectPointPosition;
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

    public static Vector3 GetTriangleBarycentricCoordinate(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        Vector3 coordinate;

        Vector3 normal = GetNormalOfTriangle(a, b, c);

        // The area of a triangle is 
        float areaABC = Vector3.Dot(normal, Vector3.Cross((b - a), (c - a)));
        float areaPBC = Vector3.Dot(normal, Vector3.Cross((b - p), (c - p)));
        float areaPCA = Vector3.Dot(normal, Vector3.Cross((c - p), (a - p)));

        coordinate.x = areaPBC / areaABC;
        coordinate.y = areaPCA / areaABC;
        coordinate.z = 1.0f - (coordinate.x - coordinate.y);

        return coordinate;
    }

    class Plane
    {
        public Vector3 normal;
        public float distFromOrigin;
    };

    Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 p)
    {
        Vector3 fromStartToEnd = lineEnd - lineStart;
        float t = Vector3.Dot(p - lineStart, fromStartToEnd) / Vector3.Dot(fromStartToEnd, fromStartToEnd);
        t = Mathf.Max(t, 0.0f);
        t = Mathf.Min(t, 1.0f);

        return lineStart + fromStartToEnd * t;
    }

    bool IsPointInTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p)
    {
        Vector3 a = v1 - p;
        Vector3 b = v2 - p;
        Vector3 c = v3 - p;

        Vector3 normPBC = Vector3.Cross(b, c);
        Vector3 normPCA = Vector3.Cross(c, a);
        Vector3 normPAB = Vector3.Cross(a, b);

        if (Vector3.Dot(normPBC, normPCA) < 0.0f)
        {
            return false;
        }
        else if (Vector3.Dot(normPBC, normPAB) < 0.0f)
        {
            return false;
        }

        return true;
    }

    float CalculateSignedDistance(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p, out Vector3 projectedPoint)
    {
        Vector3 normal = GetNormalOfTriangle(v1, v2, v3);
        Plane trianglePlane = new Plane();
        trianglePlane.normal = normal;
        trianglePlane.distFromOrigin = Vector3.Dot(trianglePlane.normal, v1);

        Plane planePoint = new Plane();
        planePoint.normal = normal;
        planePoint.distFromOrigin = Vector3.Dot(planePoint.normal, p);
        projectedPoint = p - (normal * planePoint.distFromOrigin);


        if(IsPointInTriangle(v1, v2, v3, projectedPoint))
        {
            return Vector3.Distance(p, projectedPoint);
        }
        else
        {
            Vector3 e0 = GetClosestPointOnLine(v1, v2, p);
            Vector3 e1 = GetClosestPointOnLine(v2, v3, p);
            Vector3 e2 = GetClosestPointOnLine(v3, v1, p);

            float ed0 = Vector3.SqrMagnitude(e0 - p);
            float ed1 = Vector3.SqrMagnitude(e1 - p);
            float ed2 = Vector3.SqrMagnitude(e2 - p);

            // Get the closest point on an edge
            if(ed0 < ed1 && ed0 < ed2)
            {
                projectedPoint = e0;
                return Vector3.Distance(e0, p);
            }
            else if (ed1 < ed0 && ed1 < ed2)
            {
                projectedPoint = e1;
                return Vector3.Distance(e1, p);
            }
            else if (ed2 < ed0 && ed2 < ed1)
            {
                projectedPoint = e2;
                return Vector3.Distance(e2, p);
            }

            // We are cloest to a vertex.
            float vd0 = Vector3.SqrMagnitude(v1 - p);
            float vd1 = Vector3.SqrMagnitude(v2 - p);
            float vd2 = Vector3.SqrMagnitude(v3 - p);

            if (vd0 < vd1 && vd0 < vd2)
            {
                projectedPoint = v1;
                return Vector3.Distance(v1, p);
            }
            else if (vd1 < vd0 && vd1 < vd2)
            {
                projectedPoint = v2;
                return Vector3.Distance(v2, p);
            }
            else
            {
                projectedPoint = v3;
                return Vector3.Distance(v3, p);
            }
        }
    }
}
