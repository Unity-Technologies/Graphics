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
    public enum CapsuleShadowFlags
    {
        CountMask = 0x0000ffff,
        MethodShift = 16,
        MethodMask = 0x000f0000,

        ExtraShift = 20,
        ExtraMask = 0x00f00000,

        DirectEnabledBit = 0x00100000,
        IndirectEnabledBit = 0x00200000,
        FadeSelfShadowBit = 0x00400000,
        LightLoopBit = 0x00800000,
        SplitDepthRangeBit = 0x01000000,
        HalfResBit = 0x02000000,
        NeedsTileCheckBit = 0x04000000,
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
