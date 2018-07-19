using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering
{
    public static class HDReflectionProbeEditorUtility
    {
        public static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            var t = probe.transform.position;
            return Matrix4x4.TRS(t, GetLocalSpaceRotation(probe), Vector3.one);
        }

        public static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            var supportsRotation = (SupportedRenderingFeatures.active.reflectionProbeSupportFlags & SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation) != 0;
            return supportsRotation
                ? probe.transform.rotation
                : Quaternion.identity;
        }

        // Ensures that probe's AABB encapsulates probe's position
        // Returns true, if center or size was modified
        public static bool ValidateAABB(ReflectionProbe p, ref Vector3 center, ref Vector3 size)
        {
            var localSpace = GetLocalSpace(p);
            var localTransformPosition = localSpace.inverse.MultiplyPoint3x4(p.transform.position);

            var b = new Bounds(center, size);

            if (b.Contains(localTransformPosition))
                return false;

            b.Encapsulate(localTransformPosition);

            center = b.center;
            size = b.size;
            return true;
        }

        public static float CalculateSphereMaxBlendDistance(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor o)
        {
            return p.influenceSphereRadius.floatValue;
        }

        public static Vector3 CalculateBoxMaxBlendDistance(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor o)
        {
            return p.boxSize.vector3Value * 0.5f;
        }
    }
}
