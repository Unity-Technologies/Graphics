using UnityEngine;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    [GenerateHLSL]
    public enum GPULightType
    {
        Directional,
        Spot,
        Point,
        ProjectorOrtho,
        ProjectorPyramid,

        // AreaLight
        Rectangle,
        Line,
        // Currently not supported in real time (just use for reference)
        Sphere,
        Disk,
        Hemisphere,
        Cylinder
    };

    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL]
    public struct LightData
    {
        public Vector3 positionWS;
        public float invSqrAttenuationRadius;

        public Vector3 color;
        public float angleScale;  // Spot light

        public Vector3 forward;
        public float angleOffset; // Spot light

        public Vector3 up;
        public float diffuseScale;

        public Vector3 right;
        public float specularScale;

        public float shadowDimmer;
        // index are -1 if not used
        public int shadowIndex;
        public int IESIndex;
        public int cookieIndex;

        public GPULightType lightType;
        // Area Light specific
        public Vector2 size; // x = cot(outerHalfAngle) for spot lights
        public bool twoSided;
    };

    [GenerateHLSL]
    public struct DirectionalLightData
    {
        public Vector3 forward;
        public float   diffuseScale;

        public Vector3 up;
        public float   invScaleY;

        public Vector3 right;
        public float   invScaleX;

        public Vector3 positionWS;
        public bool    tileCookie;

        public Vector3 color;
        public float   specularScale;

        // Sun disc size
        public float cosAngle;  // Distance to the disk
        public float sinAngle;  // Disk radius
        public int shadowIndex; // -1 if unused
        public int cookieIndex; // -1 if unused
    };


    // TODO: we may have to add various parameters here for shadow - was suppose to be coupled with a light loop
    // A point light is 6x PunctualShadowData
    [GenerateHLSL]
    public struct ShadowData
    {
        // World to ShadowMap matrix
        // Include scale and bias for shadow atlas if any
        public Matrix4x4 worldToShadow;

        public float bias;
        public float quality;
        public float unused;
        public float unused2;
        public Vector4 invResolution;
    };

    [GenerateHLSL]
    public enum EnvShapeType
    {
        None,
        Box,
        Sphere,
        Sky
    };

    [GenerateHLSL]
    public enum EnvConstants
    {
        SpecCubeLodStep = 6
    }


    [GenerateHLSL]
    public struct EnvLightData
    {
        public Vector3 positionWS;
        public EnvShapeType envShapeType;

        public Vector3 forward;
        public int envIndex;

        public Vector3 up;
        public float blendDistance;     // blend transition outside the volume

        public Vector3 right;
        public int unused0;

        public Vector3 innerDistance;   // equivalent to volume scale
        public float unused1;

        public Vector3 offsetLS;
        public float unused2;
    };
} // namespace UnityEngine.Experimental.ScriptableRenderLoop
