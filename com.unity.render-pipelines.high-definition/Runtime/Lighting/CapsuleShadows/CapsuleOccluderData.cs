using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum CapsuleShadowPipeline
    {
        InLightLoop,
        AfterDepthPrePass,
    }

    public enum CapsuleShadowResolution
    {
        Full,
        Half,
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
    public enum CapsuleShadowUpscaleMethod
    {
        SingleGather4,
        DoubleGather4,
        QuadGather4,
    }

    [GenerateHLSL]
    public enum CapsuleShadowFlags
    {
        CountMask = 0x00000fff,
        MethodShift = 12,
        MethodMask = 0x0000f000,

        ExtraShift = 16,
        ExtraMask = 0x000f0000,

        DirectEnabledBit = 0x00010000,
        IndirectEnabledBit = 0x00020000,
        FadeSelfShadowBit = 0x00040000,
        LightLoopBit = 0x00080000,
        SplitDepthRangeBit = 0x00100000,
        HalfResBit = 0x00200000,
        NeedsUpscaleBit = 0x00400000,
        NeedsTileCheckBit = 0x00800000,
        UpscaleShift = 24,
        UpscaleMask = 0x03000000,
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleOccluderData
    {
        public Vector3 centerRWS;
        public float radius;
        public Vector3 axisDirWS;
        public float offset;
        public Vector3 indirectDirWS;   // for CapsuleIndirectShadowMethod.DirectionAtCapsule
        public uint packedData;         // [23:16]=layerMask, [15:8]=casterType, [7:0]=casterIndex
    }

    [GenerateHLSL]
    internal enum CapsuleShadowCasterType
    {
        Directional,        // with solid angle
        Point,              // with spherical size
        Spot,               // with spherical size
        // TODO: spot (use spot cone for culling)
        Indirect,
        // TODO: other indirect types
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleShadowCaster
    {
        public uint casterType;
        public float shadowRange;
        public float maxCosTheta;
        public float lightRange;        // point/spot light only

        public Vector3 directionWS;     // directional light, or spot axis
        public float spotCosTheta;      // spot light only

        public Vector3 positionRWS;     // point/spot light only
        public float radiusWS;          // point/spot light only

        public const int maxCapsuleShadowCasterCount = 8;

        public const int capsuleShadowIndirectIndexTileCount = 0;
        public const int capsuleShadowIndirectIndexShadowCount = 3;
        public const int capsuleShadowIndirectUintCount = 4;

        internal CapsuleShadowCaster(CapsuleShadowCasterType _casterType, float _shadowRange, float _maxCosTheta)
        {
            casterType = (uint)_casterType;
            shadowRange = _shadowRange;
            maxCosTheta = _maxCosTheta;
            lightRange = 0.0f;
            directionWS = Vector3.zero;
            spotCosTheta = 1.0f;
            positionRWS = Vector3.zero;
            radiusWS = 0.0f;
        }

        internal CapsuleShadowCasterType GetCasterType() { return (CapsuleShadowCasterType)casterType; }
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
                packedData = (uint)occluder.lightLayersMask << 16,
            };
        }
    }
}
