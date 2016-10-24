using UnityEngine;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [GenerateHLSL]
    // Power of two value as they are flag
    public enum LightFlags
    {
        HasShadow = (1 << 0),
        HasCookie = (1 << 1),
        HasIES = (1 << 2)
    }

    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL]
    public struct PunctualLightData
    {
        public Vector3 positionWS;
        public float invSqrAttenuationRadius;

        public Vector3 color;
        public float useDistanceAttenuation;

        public Vector3 forward;
        public float angleScale;

        public Vector3 up;
        public float angleOffset;

        public Vector3 right;
        public LightFlags flags;        

        public float diffuseScale;
        public float specularScale;
        public float shadowDimmer;
        public int ShadowIndex;

        public int IESIndex;
        public int CookieIndex;
        public Vector2 unused;
    };

    [GenerateHLSL]
    public enum AreaShapeType
    {
        Rectangle,
        Line,
        // Currently not supported in real time (just use for reference)
        Sphere,
        Disk,
        Hemisphere,
        Cylinder
    };

    [GenerateHLSL]
    public struct AreaLightData
    {
        public Vector3 positionWS;
        public float invSqrAttenuationRadius;

        public Vector3 color;
        public AreaShapeType shapeType;

        public Vector3 forward;
        public float diffuseScale;

        public Vector3 up;
        public float specularScale;

        public Vector3 right;
        public float shadowDimmer;

        public Vector2 size;
        public float twoSided;
        public float unused;
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
        public EnvShapeType envShapeType;

        public Vector3 forward;
        public float unused2;

        public Vector3 up;
        public float blendDistance;     // blend transition outside the volume

        public Vector3 right;
        public int sliceIndex;

        public Vector3 innerDistance;   // equivalent to volume scale
        public float unused0;

        public Vector3 offsetLS;
        public float unused1;
    };

} // namespace UnityEngine.Experimental.ScriptableRenderLoop
