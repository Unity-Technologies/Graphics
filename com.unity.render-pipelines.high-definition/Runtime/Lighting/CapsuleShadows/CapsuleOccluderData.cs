using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum CapsuleShadowPipeline
    {
        InLightLoop,
        PrePassFullResolution,
        PrePassHalfResolution,
    }

    public enum CapsuleShadowTextureFormat
    {
        U8,
        U16,
    }

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
        SplitDepthRangeBit = 0x08000000,
        DirectEnabledBit = 0x10000000,
        IndirectEnabledBit = 0x20000000,
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleOccluderData
    {
        public Vector3 centerRWS;
        public float radius;
        public Vector3 axisDirWS;
        public float offset;
        public Vector3 indirectDirWS; // for CapsuleIndirectShadowMethod.DirectionAtCapsule
        public uint layerMask;
    }

    [GenerateHLSL]
    internal enum CapsuleShadowCasterType
    {
        None,               // read by all lights in the light loop
        Directional,        // with solid angle
        Point,              // with spherical size
        // TODO: spot (use spot cone for culling)
        AmbientOcclusion,
        // TODO: other indirect types
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleShadowCaster
    {
        public uint casterType;
        public uint sliceIndex;
        public float shadowRange;
        public float tanTheta;          // directional light only

        public Vector3 directionWS;     // directional light, or spot axis
        public float cosTheta;          // directional light, or maxCosTheta for point/spot

        public Vector3 positionRWS;     // point/spot light
        public float radiusWS;          // point/spot light

        internal bool isDirectional { get { return casterType == (uint)CapsuleShadowCasterType.Directional; } }

        internal bool isIndirect {  get {  return casterType == (uint)CapsuleShadowCasterType.AmbientOcclusion; } }
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
                layerMask = (uint)occluder.lightLayersMask,
            };
        }
    }
}
