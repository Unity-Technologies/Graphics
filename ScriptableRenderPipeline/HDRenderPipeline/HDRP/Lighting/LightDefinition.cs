using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    // Caution: Order is important and is use for optimization in light loop
    [GenerateHLSL]
    public enum GPULightType
    {
        Directional,
        Point,
        Spot,
        ProjectorPyramid,
        ProjectorBox,

        // AreaLight
        Line, // Keep Line lights before Rectangle. This is needed because of a compiler bug (see LightLoop.hlsl)
        Rectangle,
        // Currently not supported in real time (just use for reference)
        // Sphere,
        // Disk,
    };

    // This is use to distinguish between reflection and refraction probe in LightLoop
    [GenerateHLSL]
    public enum GPUImageBasedLightingType
    {
        Reflection,
        Refraction
    };

    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL]
    public struct DirectionalLightData
    {
        public Vector3 positionWS;
        public int tileCookie; // TODO: make it a bool

        public Vector3 color;
        public int shadowIndex; // -1 if unused

        public Vector3 forward;
        public int cookieIndex; // -1 if unused

        public Vector3 right;   // Rescaled by (2 / shapeWidth)
        public float specularScale;

        public Vector3 up;      // Rescaled by (2 / shapeHeight)
        public float diffuseScale;

        public float volumetricDimmer;
        public int dynamicShadowCasterOnly; // Use with ShadowMask feature // TODO: make it a bool

        public Vector4 shadowMaskSelector; // Use with ShadowMask feature
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

        public Vector3 right;   // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by (2 / shapeWidth)
        public float specularScale;

        public Vector3 up;      // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by (2 / shapeHeight)
        public float diffuseScale;

        public float angleScale;  // Spot light
        public float angleOffset; // Spot light
        public float shadowDimmer;
        public int dynamicShadowCasterOnly; // Use with ShadowMask feature // TODO: make it a bool

        public Vector4 shadowMaskSelector; // Use with ShadowMask feature

        public Vector2 size;        // Used by area (X = length or width, Y = height) and box projector lights (X = range (depth))
        public GPULightType lightType;
        public float minRoughness;  // This is use to give a small "area" to punctual light, as if we have a light with a radius.

        public float volumetricDimmer; // TODO: improve the cache locality
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


    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection
    [GenerateHLSL]
    public struct EnvLightData
    {
        // Packing order depends on chronological access to avoid cache misses
        // Caution: The struct need to be align on byte16 (not strictly needed for structured buffer but if we do array later better).

        // Proxy properties
        public Vector3 capturePositionWS;
        public EnvShapeType influenceShapeType;

        // Box: extents = box extents
        // Sphere: extents.x = sphere radius
        public Vector3 proxyExtents;
        // User can chose if they use This is use in case we want to force infinite projection distance (i.e no projection);
        public float minProjectionDistance;

        public Vector3 proxyPositionWS;
        public Vector3 proxyForward;
        public Vector3 proxyUp;
        public Vector3 proxyRight;

        // Influence properties
        public Vector3 influencePositionWS;
        public Vector3 influenceForward;
        public Vector3 influenceUp;
        public Vector3 influenceRight;

        public Vector3 influenceExtents;
        public float unused00;

        public Vector3 blendDistancePositive;
        public Vector3 blendDistanceNegative;
        public Vector3 blendNormalDistancePositive;
        public Vector3 blendNormalDistanceNegative;

        public Vector3 boxSideFadePositive;
        public Vector3 boxSideFadeNegative;
        public float weight;
        public float multiplier;

        // Sampling properties
        public int envIndex;
    };

    [GenerateHLSL]
    public enum EnvCacheType
    {
        Texture2D,
        Cubemap
    }

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
