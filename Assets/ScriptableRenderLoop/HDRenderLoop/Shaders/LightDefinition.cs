using UnityEngine;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.ScriptableRenderLoop
{
    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL]
    public struct PunctualLightData
    {
        public Vector3 positionWS;
        public float invSqrAttenuationRadius;

        public Vector3 color;
        public float useDistanceAttenuation;

        public Vector3 forward;
        public float diffuseScale;

        public Vector3 up;
        public float specularScale;

        public Vector3 right;
        public float shadowDimmer;

        public float angleScale;
        public float angleOffset;
        public Vector2 unused2;
    };

    [GenerateHLSL]
    public struct AreaLightData
    {
        public Vector3 positionWS;
    };

    [GenerateHLSL]
    public enum EnvShapeType
    {
        None, 
        Box, 
        Sphere
    };

    [GenerateHLSL]
    public struct EnvLightData
    {
        public Vector3 positionWS;
        public EnvShapeType shapeType;

        public Matrix4x4 worldToLocal; // No scale        

        public Vector3 innerDistance;
        public int sliceIndex;

        public Vector3 capturePointWS;
        public float blendDistance;
    };

    [GenerateHLSL]
    public struct PlanarLightData
    {
        public Vector3 positionWS;
    };
} // namespace UnityEngine.ScriptableRenderLoop
