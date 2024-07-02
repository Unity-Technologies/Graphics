using Unity.Mathematics;

namespace UnityEngine.Rendering.HighDefinition
{
    // This buffer contains surface data that mostly don't change
    // Note: be careful not to use generic names to not conflict with user defined variables
    // eg. _FoamSmoothness should not be used
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWaterPerSurface
    {
        // Transform of the water surface
        public Matrix4x4 _WaterSurfaceTransform;
        public Matrix4x4 _WaterSurfaceTransform_Inverse;

        // Per band data
        public Vector4 _PatchOrientation;
        public Vector4 _PatchWindSpeed;
        public Vector4 _PatchDirectionDampener;
        public int4 _PatchGroup;

        // Per group data
        public float2 _GroupOrientation;

        // Resolution at which the simulation is evaluated
        public uint _BandResolution;
        // Surface Index
        public int _SurfaceIndex;

        // Per band data
        public Vector4 _Band0_ScaleOffset_AmplitudeMultiplier;
        public Vector4 _Band1_ScaleOffset_AmplitudeMultiplier;
        public Vector4 _Band2_ScaleOffset_AmplitudeMultiplier;
        public float2 _Band0_Fade;
        public float2 _Band1_Fade;
        public float2 _Band2_Fade;

        // Deformation region resolution
        public int _DeformationRegionResolution;
        // Foam region resolution
        public float _WaterFoamRegionResolution;

        // Foam Intensity
        public float _SimulationFoamIntensity;
        // Amount of surface foam
        public float _SimulationFoamAmount;
        // Foam Tiling
        public float _WaterFoamTiling;
        // Resolution of the decal atlas
        public float _DecalAtlasScale;

        // Size of the decal region
        public Vector2 _DecalRegionScale;
        // Center of the decal region
        public Vector2 _DecalRegionOffset;

        // Up direction of the water surface
        public float4 _WaterUpDirection;

        // Extinction coefficient
        public float4 _WaterExtinction;

        // Maximum refraction distance
        public float _MaxRefractionDistance;
        // Caustics data
        public float _CausticsRegionSize;
        // Caustic band index
        public int _CausticsBandIndex;
        // Offset applied to the caustics LOD
        public float _CausticsMaxLOD;

        // Base color data
        public Vector4 _WaterAlbedo;

        public float _AmbientScattering;
        public float _HeightBasedScattering;
        public float _DisplacementScattering;
        public float _ScatteringWaveHeight;

        // Influence of current on foam scrolling
        public float _FoamCurrentInfluence;
        // Smoothness of the foam
        public float _WaterFoamSmoothness;
        // Water smoothness
        public float _WaterSmoothness;
        // Controls the fade multiplier of the foam
        public float _FoamPersistenceMultiplier;

        // Tiling of the caustics texture
        public float _CausticsTilingFactor;
        // Intensity of the water caustics
        public float _CausticsIntensity;
        // Intensity of the water caustics in sun shadow
        public float _CausticsShadowIntensity;
        // Blend distance
        public float _CausticsPlaneBlendDistance;

        // Maximal horizontal displacement
        public float _MaxWaveDisplacement;
        // Maximal wave height of the current setup
        public float _MaxWaveHeight;
        public Vector2 _PaddingW2;

        // Which rendering layers should affect this surface - for decals
        public uint _WaterRenderingLayer;
        // Max tessellation factor
        public float _WaterMaxTessellationFactor;
        // Distance at which the fade of the tessellation starts
        public float _WaterTessellationFadeStart;
        // Size of the range of the tessellation
        public float _WaterTessellationFadeRange;

        // This matrix is used for caustics in case of a custom mesh
        public Matrix4x4 _WaterCustomTransform_Inverse;

        // Those are not used in decal mode

        public Vector2 _WaterMaskScale;
        public Vector2 _WaterMaskOffset;
        public Vector2 _WaterMaskRemap;
        public Vector2 _CurrentMapInfluence;

        public Vector2 _SimulationFoamMaskScale;
        public Vector2 _SimulationFoamMaskOffset;

        public Vector4 _Group0CurrentRegionScaleOffset;
        public Vector4 _Group1CurrentRegionScaleOffset;

        // Below are the only data that need to be changed every frame
        // Currently the whole buffer is reupload anyway, but this should be changed

        // Maximum vertical deformation
        public float _MaxWaterDeformation;
        // Current simulation time
        public float _SimulationTime;
        // Delta-time since the last simulation step
        public float _DeltaTime;
        // Padding
        public float _PaddingW3;
    }

    // This buffer contains surface data that vary per camera
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWaterPerCamera
    {
        // Offset of the patch w/r to the origin. w is used to scale the low res water mesh
        public float2 _PatchOffset;
        // Horizontal size of the grid in the horizontal plane
        public float2 _GridSize;

        // Size of the quad in world space (to cull non-infinite instanced quads)
        public float2 _RegionExtent;

        // Low res grid multiplier
        public float _GridSizeMultiplier;
        // Maximum LOD
        public uint _MaxLOD;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWaterDebug
    {
        public int _WaterDebugMode;
        public int _WaterMaskDebugMode;
        public int _WaterCurrentDebugMode;
        public float _CurrentDebugMultiplier;

        public int _WaterFoamDebugMode;
        public int _PaddingWDbg0;
        public int _PaddingWDbg1;
        public int _PaddingWDbg2;
    }
}
