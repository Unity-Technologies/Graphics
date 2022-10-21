using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class VolumeUtils
    {
        internal static Bounds ComputeBoundsWS(Transform transform, Vector3 size)
        {
            return ComputeBoundsWS(transform.position, transform.rotation, size);
        }

        internal static Bounds ComputeBoundsWS(Vector3 position, Quaternion rotation, Vector3 size)
        {
            // Unity Bounds class has guards that will break assignment of Positive/Negative infinity.
            // In our case, we want these assignments to force the first iteration of the loop to assign the first position as the min and max.
            // Just using temporary vector3s and assigning to the Bounds class at the end once we have valid bounds.
            Vector3 boundsMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 boundsMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (uint i = 0; i < 8u; ++i)
            {
                Vector3 positionOS = new Vector3(
                    (float)(i & 1u),
                    (float)((i >> 1) & 1u),
                    (float)((i >> 2) & 1u)
                );
                positionOS.x = positionOS.x * size.x - 0.5f * size.x;
                positionOS.y = positionOS.y * size.y - 0.5f * size.y;
                positionOS.z = positionOS.z * size.z - 0.5f * size.z;

                Vector3 positionWS = (rotation * positionOS) + position;

                boundsMin = Vector3.Min(boundsMin, positionWS);
                boundsMax = Vector3.Max(boundsMax, positionWS);
            }

            Bounds bounds = new Bounds();
            bounds.min = boundsMin;
            bounds.max = boundsMax;
            return bounds;
        }

        internal static Matrix4x4 ComputeProbeIndex3DToPositionWSMatrix(Transform transform, Vector3 size, int resolutionX, int resolutionY, int resolutionZ)
        {
            return ComputeProbeIndex3DToPositionWSMatrix(transform.position, transform.rotation, size, resolutionX, resolutionY, resolutionZ);
        }

        internal static Matrix4x4 ComputeProbeIndex3DToPositionWSMatrix(Vector3 position, Quaternion rotation, Vector3 size, int resolutionX, int resolutionY, int resolutionZ)
        {
            Vector3 scale = ComputeCellSizeWS(size, resolutionX, resolutionY, resolutionZ);

            // Handle half probe offset from bounds.
            Vector3 translation = (rotation * new Vector3(
                        0.5f * scale.x - size.x * 0.5f,
                        0.5f * scale.y - size.y * 0.5f,
                        0.5f * scale.z - size.z * 0.5f
                    )
                )
                + position;

            return Matrix4x4.TRS(translation, rotation, scale);
        }

        internal static Vector3 ComputeCellSizeWS(Vector3 size, int resolutionX, int resolutionY, int resolutionZ)
        {
            return new Vector3(
                size.x / resolutionX,
                size.y / resolutionY,
                size.z / resolutionZ
            );
        }

        internal static int ComputeProbeCount(int resolutionX, int resolutionY, int resolutionZ)
        {
            return resolutionX * resolutionY * resolutionZ;
        }
    }
}
