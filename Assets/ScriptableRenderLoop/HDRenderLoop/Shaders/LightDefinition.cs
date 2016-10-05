//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------

namespace UnityEngine.ScriptableRenderLoop
{
    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL]
    public struct PunctualLightData
    {
        public Vec3 positionWS;
        public float invSqrAttenuationRadius;

        public Vec3 color;
        public float useDistanceAttenuation;

        public Vec3 forward;
        public float diffuseScale;

        public Vec3 up;
        public float specularScale;

        public Vec3 right;
        public float shadowDimmer;

        public float angleScale;
        public float angleOffset;
        public Vec2 unused2;
    };

    [GenerateHLSL]
    public struct AreaLightData
    {
        public Vec3 positionWS;
    };

    [GenerateHLSL]
    public struct EnvLightData
    {
        public Vec3 positionWS;
    };

    [GenerateHLSL]
    public struct PlanarLightData
    {
        public Vec3 positionWS;
    };
} // namespace UnityEngine.ScriptableRenderLoop
