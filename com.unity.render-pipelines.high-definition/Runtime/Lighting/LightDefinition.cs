namespace UnityEngine.Rendering.HighDefinition
{
    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    // Caution: Order is important and is use for optimization in light loop
    [GenerateHLSL]
    enum GPULightType
    {
        Directional,
        Point,
        Spot,
        ProjectorPyramid,
        ProjectorBox,

        // AreaLight
        Tube, // Keep Line lights before Rectangle. This is needed because of a compiler bug (see LightLoop.hlsl)
        Rectangle,
        // Currently not supported in real time (just use for reference)
        Disc,
        // Sphere,
    };

    static class GPULightTypeExtension
    {
        public static bool IsAreaLight(this GPULightType lightType)
        {
            return lightType == GPULightType.Rectangle || lightType == GPULightType.Tube;
        }

        public static bool IsSpot(this GPULightType lightType)
        {
            return lightType == GPULightType.Spot || lightType == GPULightType.ProjectorBox || lightType == GPULightType.ProjectorPyramid;
        }
    }

    // This is use to distinguish between reflection and refraction probe in LightLoop
    [GenerateHLSL]
    enum GPUImageBasedLightingType
    {
        Reflection,
        Refraction
    };

    /// <summary>
    /// Cookie Mode
    /// </summary>
    [GenerateHLSL]
    public enum CookieMode
    {
        /// <summary>No cookie at all.</summary>
        None,
        /// <summary>Cookie texture with clamped sampling mode.</summary>
        Clamp,
        /// <summary>Cookie texture with repeat sampling mode.</summary>
        Repeat,
    }

    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL(PackingRules.Exact, false)]
    struct DirectionalLightData
    {
        // Packing order depends on chronological access to avoid cache misses
        // Make sure to respect the 16-byte alignment
        public Vector3 positionRWS;
        public uint    lightLayers;

        public float   lightDimmer;
        public float   volumetricLightDimmer;   // Replaces 'lightDimer'

        public Vector3 forward;
        public CookieMode cookieMode;

        public Vector4 cookieScaleOffset;

        public Vector3 right;                   // Rescaled by (2 / shapeWidth)
        public int     shadowIndex;             // -1 if unused (TODO: 16 bit)

        public Vector3 up;                      // Rescaled by (2 / shapeHeight)
        public int     contactShadowIndex;      // -1 if unused (TODO: 16 bit)

        public Vector3 color;
        public int     contactShadowMask;       // 0 if unused (TODO: 16 bit)

        public Vector3 shadowTint;              // Use to tint shadow color
        public float   shadowDimmer;

        public float   volumetricShadowDimmer;  // Replaces 'shadowDimmer'
        public int     nonLightMappedOnly;      // Used with ShadowMask (TODO: use a bitfield)
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public float   minRoughness;            // Hack
        public int     screenSpaceShadowIndex;  // -1 if unused (TODO: 16 bit)

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector4 shadowMaskSelector;      // Used with ShadowMask feature

        public float   diffuseDimmer;
        public float   specularDimmer;
        public float   penumbraTint;
        public float   isRayTracedContactShadow;

        public float   distanceFromCamera;      // -1 -> no sky interaction
        public float   angularDiameter;         // Units: radians
        public float   flareFalloff;
        public float   __unused__;

        public Vector3 flareTint;
        public float   flareSize;               // Units: radians

        public Vector3 surfaceTint;

        public Vector4 surfaceTextureScaleOffset;     // -1 if unused (TODO: 16 bit)
    };

    [GenerateHLSL(PackingRules.Exact, false)]
    struct LightData
    {
        // Packing order depends on chronological access to avoid cache misses
        // Make sure to respect the 16-byte alignment
        public Vector3 positionRWS;
        public uint    lightLayers;

        public float   lightDimmer;
        public float   volumetricLightDimmer;   // Replaces 'lightDimer'
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public float   angleScale;              // Spot light
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public float   angleOffset;             // Spot light

        public Vector3 forward;
        public float   iesCut;                  // Spot light

        public GPULightType lightType;          // TODO: move this up?

        public Vector3 right;                   // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by (2 / shapeWidth)
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public float   range;

        public Vector3 up;                      // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by (2 / shapeHeight)
        public float   rangeAttenuationScale;

        public Vector3 color;
        public float   rangeAttenuationBias;

        public CookieMode cookieMode;

        public int     shadowIndex;             // -1 if unused (TODO: 16 bit)

        public Vector4 cookieScaleOffset;       // coordinates of the cookie texture in the atlas
        public int     contactShadowMask;       // negative if unused (TODO: 16 bit)

        public Vector3 shadowTint;              // Use to tint shadow color
        public float   shadowDimmer;

        public float   volumetricShadowDimmer;  // Replaces 'shadowDimmer'
        public int     nonLightMappedOnly;      // Used with ShadowMask feature (TODO: use a bitfield)
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public float   minRoughness;            // This is use to give a small "area" to punctual light, as if we have a light with a radius.
        // TODO: Instead of doing this, we should pack the ray traced shadow index into the tile cookie for instance
        public int     screenSpaceShadowIndex;  // -1 if unused (TODO: 16 bit)

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector4 shadowMaskSelector;      // Used with ShadowMask feature

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector4 size;                    // Used by area (X = length or width, Y = height, Z = CosBarnDoorAngle, W = BarnDoorLength) and punctual lights (X = radius)

        public float   diffuseDimmer;
        public float   specularDimmer;
        public float   isRayTracedContactShadow;
        public float   penumbraTint;

        public Vector3 padding;
        public float   boxLightSafeExtent;
    };


    [GenerateHLSL]
    enum EnvShapeType
    {
        None,
        Box,
        Sphere,
        Sky
    };

    [GenerateHLSL]
    enum EnvConstants
    {
        ConvolutionMipCount = 7,
    }


    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection
    [GenerateHLSL(PackingRules.Exact, false)]
    struct EnvLightData
    {
        // Packing order depends on chronological access to avoid cache misses
        public uint lightLayers;
        // Proxy properties
        public Vector3 capturePositionRWS;

        public EnvShapeType influenceShapeType;
        // Box: extents = box extents
        // Sphere: extents.x = sphere radius
        public Vector3 proxyExtents;

        // User can chose if they use This is use in case we want to force infinite projection distance (i.e no projection);
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public float minProjectionDistance;
        public Vector3 proxyPositionRWS;

        public Vector3 proxyForward;
        public Vector3 proxyUp;
        public Vector3 proxyRight;
        // Influence properties
        public Vector3 influencePositionRWS;

        public Vector3 influenceForward;
        public Vector3 influenceUp;
        public Vector3 influenceRight;
        public Vector3 influenceExtents;

        public Vector3 blendDistancePositive;
        public Vector3 blendDistanceNegative;
        public Vector3 blendNormalDistancePositive;
        public Vector3 blendNormalDistanceNegative;

        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector3 boxSideFadePositive;
        [SurfaceDataAttributes(precision = FieldPrecision.Real)]
        public Vector3 boxSideFadeNegative;
        public float weight;
        public float multiplier;

        public float rangeCompressionFactorCompensation;
        // Only used for planar reflections to drop all mips below mip0
        public float roughReflections;
        // Only used for reflection probes to avoid using the proxy for distance based roughness.
        public float distanceBasedRoughness;
        // Sampling properties
        public int envIndex;
    };

    [GenerateHLSL]
    enum EnvCacheType
    {
        Texture2D,
        Cubemap
    }
}
