using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    public enum CapsuleShadowMethod
    {
        FlattenThenClosestSphere,
        ClosestSphere,
        Ellipsoid
    }

    [GenerateHLSL]
    public enum CapsuleIndirectShadowMethod
    {
        AmbientOcclusion,
        Directional,
    }

    [GenerateHLSL]
    public enum CapsuleAmbientOcclusionMethod
    {
        ClosestSphere,
        LineAndClosestSphere,
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleOccluderData
    {
        public Vector3 centerRWS;
        public float radius;
        public Vector3 axisDirWS;
        public float offset;
        public uint lightLayers;
        public float pad0;
        public float pad1;
        public float pad2;
    }

    internal static class CapsuleOccluderExt
    {
        public static CapsuleOccluderData GetOccluderData(this CapsuleOccluder occluder, Vector3 originWS)
        {
            Matrix4x4 localToWorld = occluder.capsuleToWorld;

            float offset = Mathf.Max(0.0f, 0.5f * occluder.height - occluder.radius);

            Vector3 centerRWS = localToWorld.MultiplyPoint3x4(Vector3.zero) - originWS;
            Vector3 axisDirWS = localToWorld.MultiplyVector(Vector3.forward).normalized;
            float radiusWS = localToWorld.MultiplyVector(occluder.radius * Vector3.right).magnitude;

            return new CapsuleOccluderData { 
                centerRWS = centerRWS,
                radius = radiusWS,
                axisDirWS = axisDirWS,
                offset = offset,
                lightLayers = (uint)occluder.lightLayersMask,
            };
        }
    }
}
