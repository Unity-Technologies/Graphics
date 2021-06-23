using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelFieldDeminsions
{
    public int x;
    public int y;
    public int z;
}

public class VoxelUtils
{
    public static void ComputeVoxelFieldDimensions(float voxelSize, UnityEngine.Bounds bounds, out VoxelFieldDeminsions dimensions)
    {
        dimensions = new VoxelFieldDeminsions();

        float voxelSizeInverse = 1.0f / voxelSize;

        float xLength = bounds.max.x - bounds.min.x;
        float yLength = bounds.max.y - bounds.min.y;
        float zLength = bounds.max.z - bounds.min.z;

        dimensions.x = System.Math.Max(1, Mathf.RoundToInt(xLength * voxelSizeInverse)) + 1;
        dimensions.y = System.Math.Max(1, Mathf.RoundToInt(yLength * voxelSizeInverse)) + 1;
        dimensions.z = System.Math.Max(1, Mathf.RoundToInt(zLength * voxelSizeInverse)) + 1;
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
    
    public static Vector3 GetTriangleBarycentricCoordinate(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        Vector3 coordinate;

        Vector3 normal = GetNormalOfTriangle(a, b, c);

        // The area of a triangle is 
        float areaABC = Vector3.Dot(normal, Vector3.Cross((b - a), (c - a)));
        float areaPBC = Vector3.Dot(normal, Vector3.Cross((b - p), (c - p)));
        float areaPCA = Vector3.Dot(normal, Vector3.Cross((c - p), (a - p)));

        coordinate.x = areaPBC / areaABC ;
        coordinate.y = areaPCA / areaABC ;
        coordinate.z = 1.0f - coordinate.x - coordinate.y ;

        return coordinate;
    }

    public static float DistanceFromPointToTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p, out Vector3 closestPointOnTriangle)
    {
        float dist = float.MaxValue;

        Vector3 triangleNormal = GetNormalOfTriangle(v1, v2, v3);

        Vector3 b = GetTriangleBarycentricCoordinate(v1, v2, v3, p);
        closestPointOnTriangle = (b.x * v1) + (b.y * v2) + (b.z * v3);
        dist = Vector3.Distance(p, closestPointOnTriangle);

        Vector3 closestPointToP = p - closestPointOnTriangle;
        closestPointToP.Normalize();

        if (Vector3.Dot(closestPointToP, triangleNormal) < 0)
            dist = -dist;

        return dist;
    }
}
