using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleShadowOccluder
    {
        public Vector3 centerRWS;
        public float radius;
        public Vector3 axisDirWS;
        public float offset;
        public Vector3 indirectDirWS;   // for CapsuleIndirectShadowMethod.DirectionAtCapsule
        public uint layerMask;
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
        public uint casterTypeAndLayerMask;
        public float shadowRange;
        public float maxCosTheta;
        public float lightRange;        // point/spot light only

        public Vector3 directionWS;     // directional light, or spot axis
        public float spotCosTheta;      // spot light only

        public Vector3 positionRWS;     // point/spot light only
        public float radiusWS;          // point/spot light only

        internal CapsuleShadowCaster(CapsuleShadowCasterType _casterType, uint _layerMask, float _shadowRange, float _maxCosTheta)
        {
            casterTypeAndLayerMask = (uint)_casterType | (_layerMask << 8);
            shadowRange = _shadowRange;
            maxCosTheta = _maxCosTheta;
            lightRange = 0.0f;
            directionWS = Vector3.zero;
            spotCosTheta = 1.0f;
            positionRWS = Vector3.zero;
            radiusWS = 0.0f;
        }

        internal CapsuleShadowCasterType GetCasterType() { return (CapsuleShadowCasterType)(casterTypeAndLayerMask & 0xff); }
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct CapsuleShadowVolume
    {
        public uint bits;   // [31:16]=occluderIndex, [15:8]=casterIndex, [7:0]=casterType
    }

    internal static class CapsuleOccluderExt
    {
        internal static CapsuleShadowOccluder GetPackedData(this CapsuleOccluder occluder, Vector3 originWS)
        {
            Matrix4x4 localToWorld = occluder.CapsuleToWorld;

            float offset = Mathf.Max(0.0f, 0.5f * occluder.m_Height - occluder.m_Radius);

            Vector3 centerRWS = localToWorld.MultiplyPoint3x4(Vector3.zero) - originWS;
            Vector3 axisDirWS = localToWorld.MultiplyVector(Vector3.forward).normalized;
            float radiusWS = localToWorld.MultiplyVector(occluder.m_Radius * Vector3.right).magnitude;

            return new CapsuleShadowOccluder {
                centerRWS = centerRWS,
                radius = radiusWS,
                axisDirWS = axisDirWS,
                offset = offset,
                layerMask = (uint)occluder.m_LightLayerMask,
            };
        }
    }
}
