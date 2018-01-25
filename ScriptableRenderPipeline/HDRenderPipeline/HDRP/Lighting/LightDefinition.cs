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

        public Vector2 fadeDistanceScaleAndBias; // Use with ShadowMask feature
        public float unused0;
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
        
        // Proxy properties
        public float capturePositionWSX;
        public float capturePositionWSY;
        public float capturePositionWSZ;
        public EnvShapeType influenceShapeType;

        // Box: extents = box extents
        // Sphere: extents.x = sphere radius
        public float proxyExtentsX;
        public float proxyExtentsY;
        public float proxyExtentsZ;
        // User can chose if they use This is use in case we want to force infinite projection distance (i.e no projection);
        public float minProjectionDistance;

        public float proxyPositionWSX;
        public float proxyPositionWSY;
        public float proxyPositionWSZ;
        public float proxyForwardX;
        public float proxyForwardY;
        public float proxyForwardZ;
        public float proxyUpX;
        public float proxyUpY;
        public float proxyUpZ;
        public float proxyRightX;
        public float proxyRightY;
        public float proxyRightZ;

        // Influence properties
        public float influencePositionWSX;
        public float influencePositionWSY;
        public float influencePositionWSZ;
        public float influenceForwardX;
        public float influenceForwardY;
        public float influenceForwardZ;
        public float influenceUpX;
        public float influenceUpY;
        public float influenceUpZ;
        public float influenceRightX;
        public float influenceRightY;
        public float influenceRightZ;

        public float influenceExtentsX;
        public float influenceExtentsY;
        public float influenceExtentsZ;
        public float unused00;

        public float blendDistancePositiveX;
        public float blendDistancePositiveY;
        public float blendDistancePositiveZ;
        public float blendDistanceNegativeX;
        public float blendDistanceNegativeY;
        public float blendDistanceNegativeZ;
        public float blendNormalDistancePositiveX;
        public float blendNormalDistancePositiveY;
        public float blendNormalDistancePositiveZ;
        public float blendNormalDistanceNegativeX;
        public float blendNormalDistanceNegativeY;
        public float blendNormalDistanceNegativeZ;

        public float boxSideFadePositiveX;
        public float boxSideFadePositiveY;
        public float boxSideFadePositiveZ;
        public float boxSideFadeNegativeX;
        public float boxSideFadeNegativeY;
        public float boxSideFadeNegativeZ;
        public float dimmer;
        public float unused01;

        public float sampleDirectionDiscardWSX;
        public float sampleDirectionDiscardWSY;
        public float sampleDirectionDiscardWSZ;
        // Sampling properties
        public int envIndex;

        public Vector3 capturePositionWS
        {
            get { return new Vector3(capturePositionWSX, capturePositionWSY, capturePositionWSZ); }
            set
            {
                capturePositionWSX = value.x;
                capturePositionWSY = value.y;
                capturePositionWSZ = value.z;
            }
        }
        public Vector3 proxyExtents
        {
            get { return new Vector3(proxyExtentsX, proxyExtentsY, proxyExtentsZ); }
            set
            {
                proxyExtentsX = value.x;
                proxyExtentsY = value.y;
                proxyExtentsZ = value.z;
            }
        }
        public Vector3 proxyPositionWS
        {
            get { return new Vector3(proxyPositionWSX, proxyPositionWSY, proxyPositionWSZ); }
            set
            {
                proxyPositionWSX = value.x;
                proxyPositionWSY = value.y;
                proxyPositionWSZ = value.z;
            }
        }
        public Vector3 proxyForward
        {
            get { return new Vector3(proxyForwardX, proxyForwardY, proxyForwardZ); }
            set
            {
                proxyForwardX = value.x;
                proxyForwardY = value.y;
                proxyForwardZ = value.z;
            }
        }
        public Vector3 proxyUp
        {
            get { return new Vector3(proxyUpX, proxyUpY, proxyUpZ); }
            set
            {
                proxyUpX = value.x;
                proxyUpY = value.y;
                proxyUpZ = value.z;
            }
        }
        public Vector3 proxyRight
        {
            get { return new Vector3(proxyRightX, proxyRightY, proxyRightZ); }
            set
            {
                proxyRightX = value.x;
                proxyRightY = value.y;
                proxyRightZ = value.z;
            }
        }

        public Vector3 influenceExtents
        {
            get { return new Vector3(influenceExtentsX, influenceExtentsY, influenceExtentsZ); }
            set
            {
                influenceExtentsX = value.x;
                influenceExtentsY = value.y;
                influenceExtentsZ = value.z;
            }
        }
        public Vector3 influencePositionWS
        {
            get { return new Vector3(influencePositionWSX, influencePositionWSY, influencePositionWSZ); }
            set
            {
                influencePositionWSX = value.x;
                influencePositionWSY = value.y;
                influencePositionWSZ = value.z;
            }
        }
        public Vector3 influenceForward
        {
            get { return new Vector3(influenceForwardX, influenceForwardY, influenceForwardZ); }
            set
            {
                influenceForwardX = value.x;
                influenceForwardY = value.y;
                influenceForwardZ = value.z;
            }
        }
        public Vector3 influenceUp
        {
            get { return new Vector3(influenceUpX, influenceUpY, influenceUpZ); }
            set
            {
                influenceUpX = value.x;
                influenceUpY = value.y;
                influenceUpZ = value.z;
            }
        }
        public Vector3 influenceRight
        {
            get { return new Vector3(influenceRightX, influenceRightY, influenceRightZ); }
            set
            {
                influenceRightX = value.x;
                influenceRightY = value.y;
                influenceRightZ = value.z;
            }
        }

        public Vector3 blendDistancePositive
        {
            get { return new Vector3(blendDistancePositiveX, blendDistancePositiveY, blendDistancePositiveZ); }
            set
            {
                blendDistancePositiveX = value.x;
                blendDistancePositiveY = value.y;
                blendDistancePositiveZ = value.z;
            }
        }
        public Vector3 blendDistanceNegative
        {
            get { return new Vector3(blendDistanceNegativeX, blendDistanceNegativeY, blendDistanceNegativeZ); }
            set
            {
                blendDistanceNegativeX = value.x;
                blendDistanceNegativeY = value.y;
                blendDistanceNegativeZ = value.z;
            }
        }
        public Vector3 blendNormalDistancePositive
        {
            get { return new Vector3(blendNormalDistancePositiveX, blendNormalDistancePositiveY, blendNormalDistancePositiveZ); }
            set
            {
                blendNormalDistancePositiveX = value.x;
                blendNormalDistancePositiveY = value.y;
                blendNormalDistancePositiveZ = value.z;
            }
        }
        public Vector3 blendNormalDistanceNegative
        {
            get { return new Vector3(blendNormalDistanceNegativeX, blendNormalDistanceNegativeY, blendNormalDistanceNegativeZ); }
            set
            {
                blendNormalDistanceNegativeX = value.x;
                blendNormalDistanceNegativeY = value.y;
                blendNormalDistanceNegativeZ = value.z;
            }
        }
        public Vector3 boxSideFadePositive
        {
            get { return new Vector3(boxSideFadePositiveX, boxSideFadePositiveY, boxSideFadePositiveZ); }
            set
            {
                boxSideFadePositiveX = value.x;
                boxSideFadePositiveY = value.y;
                boxSideFadePositiveZ = value.z;
            }
        }
        public Vector3 boxSideFadeNegative
        {
            get { return new Vector3(boxSideFadeNegativeX, boxSideFadeNegativeY, boxSideFadeNegativeZ); }
            set
            {
                boxSideFadeNegativeX = value.x;
                boxSideFadeNegativeY = value.y;
                boxSideFadeNegativeZ = value.z;
            }
        }

        public Vector3 sampleDirectionDiscardWS
        {
            get { return new Vector3(sampleDirectionDiscardWSX, sampleDirectionDiscardWSY, sampleDirectionDiscardWSZ); }
            set
            {
                sampleDirectionDiscardWSX = value.x;
                sampleDirectionDiscardWSY = value.y;
                sampleDirectionDiscardWSZ = value.z;
            }
        }
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
