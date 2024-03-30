using Unity.Mathematics;

namespace UnityEngine.Rendering.HighDefinition
{
    // This buffer contains surface data that mostly don't change
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

        // Scale of the water mask
        public Vector2 _WaterMaskScale;
        // Offset of the water mask
        public Vector2 _WaterMaskOffset;
        // Remap range of the water mask
        public Vector2 _WaterMaskRemap;

        // Per group data
        public float2 _GroupOrientation;

        public Vector2 _DeformationRegionOffset;
        public Vector2 _DeformationRegionScale;

        // Per band data
        public Vector4 _Band0_ScaleOffset_AmplitudeMultiplier;
        public Vector4 _Band1_ScaleOffset_AmplitudeMultiplier;
        public Vector4 _Band2_ScaleOffset_AmplitudeMultiplier;
        public float2 _Band0_Fade;
        public float2 _Band1_Fade;
        public float2 _Band2_Fade;

        // Resolution at which the simulation is evaluated
        public uint _BandResolution;
        // Surface Index
        public int _SurfaceIndex;

        // Scale of the foam mask
        public Vector2 _SimulationFoamMaskScale;
        // Offset of the foam mask
        public Vector2 _SimulationFoamMaskOffset;

        // Foam Intensity
        public float _SimulationFoamIntensity;
        // Amount of surface foam
        public float _SimulationFoamAmount;
        // Foam region resolution
        public float _WaterFoamRegionResolution;
        // Foam Tiling
        public float _FoamTiling;

        // Size of the foam region
        public Vector2 _FoamRegionScale;
        // Center of the foam region
        public Vector2 _FoamRegionOffset;

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
        public float _PaddingW2;

        // Base color data
        public Vector4 _WaterAlbedo;

        public float _AmbientScattering;
        public float _HeightBasedScattering;
        public float _DisplacementScattering;
        public float _ScatteringWaveHeight;

        // Influence of current on foam scrolling
        public float _FoamCurrentInfluence;
        // Smoothness of the foam
        public float _FoamSmoothness;
        // Water smoothness
        public float _WaterSmoothness;
        // Controls the fade multiplier of the foam
        public float _FoamPersistenceMultiplier;

        // Forward vector of water surface
        public float2 _WaterForwardXZ;
        // Deformation region resolution
        public int _DeformationRegionResolution;
        // Offset applied to the caustics LOD
        public float _CausticsMaxLOD;

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

        // Current Map Influence
        public Vector2 _CurrentMapInfluence;

        // Scale & offset of the large
        public Vector4 _Group0CurrentRegionScaleOffset;
        // Scale & offset of the ripples
        public Vector4 _Group1CurrentRegionScaleOffset;

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

        // Below are the only data that needs to be changed every frame
        // Currently the whole buffer is reupload anyway, but this should be changed

        // Maximum horizontal deformation
        public float _MaxWaterDeformation;
        // Current simulation time
        public float _SimulationTime;
        // Delta-time since the last simulation step
        public float _DeltaTime;
        // Padding
        public float _PaddingW1;
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
