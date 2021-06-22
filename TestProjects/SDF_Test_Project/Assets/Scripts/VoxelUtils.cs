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

        dimensions.x = System.Math.Max(1, Mathf.RoundToInt(xLength * voxelSizeInverse));
        dimensions.y = System.Math.Max(1, Mathf.RoundToInt(yLength * voxelSizeInverse));
        dimensions.z = System.Math.Max(1, Mathf.RoundToInt(zLength * voxelSizeInverse));
    }

    static float Dot2(Vector3 v)
    {
        return Vector3.Dot(v, v);
    }

    public static float DistanceFromPointToTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 p)
    {
        float dist = float.MaxValue;

        Vector3 v21 = v2 - v1; Vector3 p1 = p - v1;
        Vector3 v32 = v3 - v2; Vector3 p2 = p - v2;
        Vector3 v13 = v1 - v3; Vector3 p3 = p - v3;

        Vector3 normal = Vector3.Cross(v21, v13);

        float a = Mathf.Sign(Vector3.Dot(Vector3.Cross(v21, normal), p1));
        float b = Mathf.Sign(Vector3.Dot(Vector3.Cross(v32, normal), p2));
        float c = Mathf.Sign(Vector3.Dot(Vector3.Cross(v13, normal), p3));
        float insideOutsideTest = a + b + c;

        if(insideOutsideTest < 2.0f)
        {
            float d1 = Mathf.Clamp01(Vector3.Dot(v21, p1) / Dot2(v21));
            float d2 = Mathf.Clamp01(Vector3.Dot(v32, p2) / Dot2(v32));
            float d3 = Mathf.Clamp01(Vector3.Dot(v13, p3) / Dot2(v13));

            a = Dot2(v21 * d1 - p1);
            b = Dot2(v32 * d2 - p2);
            c = Dot2(v13 * d3 - p3);

            dist = Mathf.Min(Mathf.Min(a,b),c);
        }
        else
        {
            dist = (Vector3.Dot(normal, p1) * Vector3.Dot(normal, p1)) / Dot2(normal);
        }

        float distSqrt = Mathf.Sqrt(dist);
        if (dist < 0.0f)
            distSqrt = -distSqrt;

        return distSqrt;
    }
}
