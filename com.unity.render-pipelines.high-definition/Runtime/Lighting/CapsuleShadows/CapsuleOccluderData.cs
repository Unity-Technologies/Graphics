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

    public enum CapsuleShadowPipeline
    {
        InLightLoop,
        PrePassFullResolution,
        PrePassHalfResolution,
    }

    [GenerateHLSL]
    public enum CapsuleIndirectShadowMethod
    {
        AmbientOcclusion,
        DirectionAtSurface,
        DirectionAtCapsule,
    }

    [GenerateHLSL]
    public enum CapsuleAmbientOcclusionMethod
    {
        ClosestSphere,
        LineAndClosestSphere,
    }

    [GenerateHLSL]
    public enum CapsuleShadowFlags
    {
        CountMask = 0x0000ffff,
        MethodShift = 16,
        MethodMask = 0x000f0000,
        ExtraShift = 20,
        ExtraMask = 0x00f00000,
        LightLoopBit = 0x01000000,
        HalfResBit = 0x02000000,
        FadeSelfShadowBit = 0x04000000,
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleOccluderData
    {
        public Vector3 centerRWS;
        public float radius;
        public Vector3 axisDirWS;
        public float offset;
        public Vector3 indirectDirWS; // for CapsuleIndirectShadowMethod.DirectionAtCapsule
        public uint lightLayers;
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
                indirectDirWS = Vector3.zero,
                lightLayers = (uint)occluder.lightLayersMask,
            };
        }
    }
}
