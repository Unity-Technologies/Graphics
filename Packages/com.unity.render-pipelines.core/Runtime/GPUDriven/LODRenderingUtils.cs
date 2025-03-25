using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Rendering
{
    // Based on C++ LODGroupManager code.
    internal static class LODRenderingUtils
    {
        public static float CalculateFOVHalfAngle(float fieldOfView)
        {
            return Mathf.Tan(Mathf.Deg2Rad * fieldOfView * 0.5f);
        }

        public static float CalculateScreenRelativeMetricNoBias(LODParameters lodParams)
        {
            if (lodParams.isOrthographic)
            {
                return 2.0F * lodParams.orthoSize;
            }

            // Half angle at 90 degrees is 1.0 (So we skip halfAngle / 1.0 calculation)
            float halfAngle = CalculateFOVHalfAngle(lodParams.fieldOfView);
            return 2.0f * halfAngle;
        }

        public static float CalculateMeshLodConstant(LODParameters lodParams, float screenRelativeMetric, float meshLodThreshold)
        {
            return meshLodThreshold * screenRelativeMetric / lodParams.cameraPixelHeight;
        }

        public static float CalculatePerspectiveDistance(Vector3 objPosition, Vector3 camPosition, float sqrScreenRelativeMetric)
        {
            return Mathf.Sqrt(CalculateSqrPerspectiveDistance(objPosition, camPosition, sqrScreenRelativeMetric));
        }

        public static float CalculateSqrPerspectiveDistance(Vector3 objPosition, Vector3 camPosition, float sqrScreenRelativeMetric)
        {
            return (objPosition - camPosition).sqrMagnitude * sqrScreenRelativeMetric;
        }

        public static Vector3 GetWorldReferencePoint(this LODGroup lodGroup)
        {
            return lodGroup.transform.TransformPoint(lodGroup.localReferencePoint);
        }

        public static float GetWorldSpaceScale(this LODGroup lodGroup)
        {
            Vector3 scale = lodGroup.transform.lossyScale;
            float largestAxis = Mathf.Abs(scale.x);
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.y));
            largestAxis = Mathf.Max(largestAxis, Mathf.Abs(scale.z));
            return largestAxis;
        }

        public static float GetWorldSpaceSize(this LODGroup lodGroup)
        {
            return lodGroup.GetWorldSpaceScale() * lodGroup.size;
        }

        public static float CalculateLODDistance(float relativeScreenHeight, float size)
        {
            return size / relativeScreenHeight;
        }
    }
}
