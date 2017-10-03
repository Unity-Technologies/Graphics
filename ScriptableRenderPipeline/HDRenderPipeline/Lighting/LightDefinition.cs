using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    [GenerateHLSL]
    public enum GPULightType
    {
        Directional,
        ProjectorBox,
        Spot,
        Point,
        ProjectorPyramid,

        // AreaLight
        Line, // Keep Line lights before Rectangle. This is needed because of a compiler bug (see LightLoop.hlsl)
        Rectangle,
        // Currently not supported in real time (just use for reference)
        // Sphere,
        // Disk,
    };

    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL]
    public struct DirectionalLightData
    {
        public Vector3 positionWS;
        public bool tileCookie;

        public Vector3 color;
        public int shadowIndex; // -1 if unused

        public Vector3 forward;
        public int cookieIndex; // -1 if unused

        public Vector3 right;   // Rescaled by (2 / shapeLenght)
        public float specularScale;

        public Vector3 up;      // Rescaled by (2 / shapeWidth)
        public float diffuseScale;
    };

    [GenerateHLSL]
    public struct LightData
    {
        public Vector3 positionWS;
        public float invSqrAttenuationRadius;

        public Vector3 color;
        public int shadowIndex; // -1 if unused

        public Vector3 forward;
        public int cookieIndex; // -1 if unused

        public Vector3 right;   // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by (2 / shapeLenght)
        public float specularScale;

        public Vector3 up;      // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by * (2 / shapeWidth)
        public float diffuseScale;

        public float angleScale;  // Spot light
        public float angleOffset; // Spot light
        public float shadowDimmer;
        public int unused0;

        public Vector2 size;      // Used by area, frustum projector and spot lights (x = cot(outerHalfAngle))
        public GPULightType lightType;
        public float minRoughness;  // This is use to give a small "area" to punctual light, as if we have a light with a radius.
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

    // Usage of StencilBits.Lighting on 2 bits.
    // We support both deferred and forward renderer.  Here is the current usage of this 2 bits:
    // 0. Everything except case below. This include any forward opaque object. No lighting in deferred lighting path.
    // 1. All deferred opaque object that require split lighting (i.e output both specular and diffuse in two different render target). Typically Subsurface scattering material.
    // 2. All deferred opaque object.
    // 3. unused
    [GenerateHLSL]
    // Caution: Value below are hardcoded in some shader (because properties doesn't support include). If order or value is change, please update corresponding ".shader"
    public enum StencilLightingUsage
    {
        NoLighting,
        SplitLighting,
        RegularLighting
    }
}
