using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelUtils
{
    public static void ComputeVoxelFieldDimensions(float voxelSize, UnityEngine.Bounds bounds, out int voxelCountX, out int voxelCountY, out int voxelCountZ)
    {
        float voxelSizeInverse = 1.0f / voxelSize;

        float xLength = bounds.max.x - bounds.min.x;
        float yLength = bounds.max.y - bounds.min.y;
        float zLength = bounds.max.z - bounds.min.z;

        voxelCountX = System.Math.Max(1, Mathf.RoundToInt(xLength * voxelSizeInverse)) + 1;
        voxelCountY = System.Math.Max(1, Mathf.RoundToInt(yLength * voxelSizeInverse)) + 1;
        voxelCountZ = System.Math.Max(1, Mathf.RoundToInt(zLength * voxelSizeInverse)) + 1;
    }

    public static Vector3 GetNormalOfTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 AB = b - a;
        Vector3 AC = c - a;

        AB.Normalize();
        AC.Normalize();

        Vector3 normal = Vector3.Cross(AB, AC);
        normal.Normalize();

        return normal;
    }

    public static float GetAreaOfTriangle(Vector3 A, Vector3 B, Vector3 C)
    {
        Vector3 AB = B - A;
        Vector3 AC = C - A;

        // The cross product is equal to the area of a parallelogram
        // which is the base * height of one of the triangles that
        // makes up the parallelogram.
        Vector3 vecCross = Vector3.Cross(AB, AC);
        float fArea = vecCross.magnitude / 2.0f;

        return fArea;
    }
    
    class Plane
    {
        public Vector3 normal;
        public float distFromOrigin;
    };

    public static Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 p)
    {
        Vector3 fromStartToEnd = lineEnd - lineStart;
        float t = Vector3.Dot(p - lineStart, fromStartToEnd) / Vector3.Dot(fromStartToEnd, fromStartToEnd);
        t = Mathf.Max(t, 0.0000f);
        t = Mathf.Min(t, 1.0f);

        return lineStart + (fromStartToEnd * t);
    }

    public static bool IsPointInTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p)
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

    public static float DistanceFromPointToTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p, bool smoothNormals, out Vector3 closestPointOnTriangle, out Vector3 normal)
    {
        Vector3 triangleNormal = GetNormalOfTriangle(v1, v2, v3);
        Plane trianglePlane = new Plane();
        trianglePlane.normal = triangleNormal;
        trianglePlane.distFromOrigin = Vector3.Dot(trianglePlane.normal, v1);

        Plane planePoint = new Plane();
        planePoint.normal = triangleNormal;
        planePoint.distFromOrigin = Vector3.Dot(planePoint.normal, p);

        float pointDist = planePoint.distFromOrigin - trianglePlane.distFromOrigin;
        closestPointOnTriangle = p - (triangleNormal * pointDist);

        float dist = float.MaxValue;

        if (IsPointInTriangle(v1, v2, v3, closestPointOnTriangle))
        {
            dist = Vector3.Magnitude(closestPointOnTriangle - p);
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
            if (ed0 < ed1 && ed0 < ed2)
            {
                closestPointOnTriangle = e0;
                dist = Vector3.Magnitude(e0 - p);
            }
            else if (ed1 < ed0 && ed1 < ed2)
            {
                closestPointOnTriangle = e1;
                dist = dist = Vector3.Magnitude(e1 - p);
            }
            else if (ed2 < ed0 && ed2 < ed1)
            {
                closestPointOnTriangle = e2;
                dist = dist = Vector3.Magnitude(e2 - p);
            }
            else
            {
                // We are cloest to a vertex.
                float vd0 = Vector3.SqrMagnitude(v1 - p);
                float vd1 = Vector3.SqrMagnitude(v2 - p);
                float vd2 = Vector3.SqrMagnitude(v3 - p);

                if (vd0 < vd1 && vd0 < vd2)
                {
                    closestPointOnTriangle = v1;
                    dist = Vector3.Magnitude(v1 - p);
                }
                else if (vd1 < vd0 && vd1 < vd2)
                {
                    closestPointOnTriangle = v2;
                    dist = Vector3.Magnitude(v2 - p);
                }
                else
                {
                    closestPointOnTriangle = v3;
                    dist = Vector3.Magnitude(v3 - p);
                }
            }
        }

        // Flip the sign of the distance based on if we are
        // inside or outside the mesh.
        Vector3 closestPointToPointNormal = p - closestPointOnTriangle;
        closestPointToPointNormal.Normalize();

        if (Vector3.Dot(closestPointToPointNormal, triangleNormal) < 0)
            dist = -dist;

        if(smoothNormals)
            normal = closestPointToPointNormal;
        else
            normal = triangleNormal;

        return dist;
    }
}
