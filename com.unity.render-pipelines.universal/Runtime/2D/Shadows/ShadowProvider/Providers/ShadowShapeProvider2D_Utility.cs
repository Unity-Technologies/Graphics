using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal static class ShadowShapeProvider2DUtility
{
    static public float GetTrimEdgeFromBounds(Bounds bounds, float trimMultipler)
    {
        Vector3 size = bounds.size;

        // Pick the smaller side
        float trimEdge = trimMultipler * (size.x < size.y ? size.x : size.y);

        // Clean up the trim value to one significant digit
        float multiplier = Mathf.Pow(10, -Mathf.Floor(Mathf.Log10(trimEdge)));
        trimEdge = Mathf.Floor(trimEdge * multiplier) / multiplier;

        return trimEdge;
    }
}
