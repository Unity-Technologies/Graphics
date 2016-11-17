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
        public float angleScale; // Spot light

        public Vector3 forward;
        public float angleOffset;

        public Vector3 up;
        public float diffuseScale; // Spot light

        public Vector3 right;
        public float specularScale;
        
        public float shadowDimmer;
        // index are -1 if not used
        public int shadowIndex;
        public int IESIndex;
        public int cookieIndex;

        public GPULightType lightType;        
        // Area Light specific
        public Vector2 size;
        public bool twoSided;
    };

    [GenerateHLSL]
    public struct DirectionalLightData
    {
        public Vector3 direction;
        public float diffuseScale;

        public Vector3 color;
        public float specularScale;        

        // Sun disc size 
        public float cosAngle; // Distance to disk
        public float sinAngle; // Disk radius
        public int shadowIndex;
        public float unused;
    };


    // TODO: we may have to add various parameters here for shadow - was suppose to be coupled with a light loop
    // A point light is 6x PunctualShadowData
    [GenerateHLSL]
    public struct PunctualShadowData
    {
        // World to ShadowMap matrix
        // Include scale and bias for shadow atlas if any
        public Matrix4x4 worldToShadow;

        public GPULightType lightType;
        public float bias;
        public float quality;
        public float unused;
    };

    [GenerateHLSL]
    public struct DirectionalShadowData
    {
        // World to ShadowMap matrix
        // Include scale and bias for shadow atlas if any
        public Matrix4x4 worldToShadow;

        public float bias;
        public float quality;
        public Vector2 unused2;
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
        public float envIndex;

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
