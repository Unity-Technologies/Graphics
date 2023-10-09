//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATERSYSTEMDEF_CS_HLSL
#define WATERSYSTEMDEF_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.WaterMaskDebugMode:  static fields
//
#define WATERMASKDEBUGMODE_RED_CHANNEL (0)
#define WATERMASKDEBUGMODE_GREEN_CHANNEL (1)
#define WATERMASKDEBUGMODE_BLUE_CHANNEL (2)

//
// UnityEngine.Rendering.HighDefinition.WaterDebugMode:  static fields
//
#define WATERDEBUGMODE_NONE (0)
#define WATERDEBUGMODE_WATER_MASK (1)
#define WATERDEBUGMODE_SIMULATION_FOAM_MASK (2)
#define WATERDEBUGMODE_CURRENT (3)
#define WATERDEBUGMODE_DEFORMATION (4)
#define WATERDEBUGMODE_FOAM (5)

//
// UnityEngine.Rendering.HighDefinition.WaterFoamDebugMode:  static fields
//
#define WATERFOAMDEBUGMODE_SURFACE_FOAM (0)
#define WATERFOAMDEBUGMODE_DEEP_FOAM (1)

//
// UnityEngine.Rendering.HighDefinition.WaterAtlasSize:  static fields
//
#define WATERATLASSIZE_ATLAS_SIZE64 (64)
#define WATERATLASSIZE_ATLAS_SIZE128 (128)
#define WATERATLASSIZE_ATLAS_SIZE256 (256)
#define WATERATLASSIZE_ATLAS_SIZE512 (512)
#define WATERATLASSIZE_ATLAS_SIZE1024 (1024)
#define WATERATLASSIZE_ATLAS_SIZE2048 (2048)

//
// UnityEngine.Rendering.HighDefinition.WaterCurrentDebugMode:  static fields
//
#define WATERCURRENTDEBUGMODE_LARGE (0)
#define WATERCURRENTDEBUGMODE_RIPPLES (1)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterDebug
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterDebug)
    int _WaterDebugMode;
    int _WaterMaskDebugMode;
    int _WaterCurrentDebugMode;
    float _CurrentDebugMultiplier;
    int _WaterFoamDebugMode;
    int _PaddingWDbg0;
    int _PaddingWDbg1;
    int _PaddingWDbg2;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.WaterGeneratorData
// PackingRules = Exact
struct WaterGeneratorData
{
    float3 position;
    float rotation;
    float2 regionSize;
    int type;
    int padding0;
    float2 padding1;
    float deepFoamDimmer;
    float surfaceFoamDimmer;
    float4 scaleOffset;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWater)
    float4 _PatchOrientation;
    float4 _PatchWindSpeed;
    float4 _PatchDirectionDampener;
    int4 _PatchGroup;
    float2 _WaterMaskScale;
    float2 _WaterMaskOffset;
    float2 _WaterMaskRemap;
    float2 _GroupOrientation;
    float2 _WaterDeformationCenter;
    float2 _WaterDeformationExtent;
    float4 _Band0_ScaleOffset_AmplitudeMultiplier;
    float4 _Band1_ScaleOffset_AmplitudeMultiplier;
    float4 _Band2_ScaleOffset_AmplitudeMultiplier;
    float2 _Band0_Fade;
    float2 _Band1_Fade;
    float2 _Band2_Fade;
    uint _BandResolution;
    int _SurfaceIndex;
    float2 _SimulationFoamMaskScale;
    float2 _SimulationFoamMaskOffset;
    float _SimulationFoamIntensity;
    float _SimulationFoamAmount;
    float _WaterFoamRegionResolution;
    float _FoamTiling;
    float2 _FoamRegionScale;
    float2 _FoamRegionOffset;
    float4 _WaterUpDirection;
    float4 _TransparencyColor;
    float _MaxRefractionDistance;
    float _OutScatteringCoefficient;
    float _CausticsRegionSize;
    int _CausticsBandIndex;
    float4 _ScatteringColorTips;
    float _AmbientScattering;
    float _HeightBasedScattering;
    float _DisplacementScattering;
    float _ScatteringWaveHeight;
    float _FoamSmoothness;
    float _WaterSmoothness;
    float _FoamPersistenceMultiplier;
    int _WaterDeformationResolution;
    float _MaxWaveDisplacement;
    float _MaxWaveHeight;
    float _SimulationTime;
    float _DeltaTime;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterRendering
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterRendering)
    float4x4 _WaterSurfaceTransform;
    float4x4 _WaterSurfaceTransform_Inverse;
    float4 _PatchOffset;
    float2 _GridSize;
    float2 _GridOffset;
    float2 _RegionExtent;
    float2 _CurrentMapInfluence;
    uint _MaxLOD;
    float _MaxWaterDeformation;
    float _CausticsMaxLOD;
    float _CausticsTilingFactor;
    float _CausticsIntensity;
    float _CausticsShadowIntensity;
    float _CausticsPlaneBlendDistance;
    int _PaddingWR1;
    float4 _Group0CurrentRegionScaleOffset;
    float4 _Group1CurrentRegionScaleOffset;
    uint _WaterRenderingLayer;
    float _WaterMaxTessellationFactor;
    float _WaterTessellationFadeStart;
    float _WaterTessellationFadeRange;
    float4 _WaterAmbientProbe;
    float4x4 _WaterCustomTransform_Inverse;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.WaterSurfaceProfile
// PackingRules = Exact
struct WaterSurfaceProfile
{
    float bodyScatteringHeight;
    float maxRefractionDistance;
    uint renderingLayers;
    int cameraUnderWater;
    float3 transparencyColor;
    float outScatteringCoefficient;
    float3 scatteringColor;
    float envPerceptualRoughness;
    float smoothnessFadeStart;
    float smoothnessFadeDistance;
    float roughnessEndValue;
    float colorPyramidScale;
    float4 foamColor;
    float3 upDirection;
    int colorPyramidMipOffset;
    int disableIOR;
    float tipScatteringHeight;
    float underWaterAmbientProbeContribution;
    float absorptionDistanceMultiplier;
};

// Generated from UnityEngine.Rendering.HighDefinition.WaterDeformerData
// PackingRules = Exact
struct WaterDeformerData
{
    float2 regionSize;
    int type;
    float amplitude;
    float3 position;
    float rotation;
    float2 blendRegion;
    float2 breakingRange;
    float bowWaveElevation;
    float waveLength;
    int waveRepetition;
    float waveSpeed;
    float waveOffset;
    int cubicBlend;
    float deepFoamDimmer;
    float surfaceFoamDimmer;
    float2 deepFoamRange;
    float2 padding3;
    float4 scaleOffset;
};

// Generated from UnityEngine.Rendering.HighDefinition.WaterSectorData
// PackingRules = Exact
struct WaterSectorData
{
    float4 dir0;
    float4 dir1;
};


#endif
