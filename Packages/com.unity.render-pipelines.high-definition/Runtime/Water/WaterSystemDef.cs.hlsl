//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATERSYSTEMDEF_CS_HLSL
#define WATERSYSTEMDEF_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.WaterCurrentDebugMode:  static fields
//
#define WATERCURRENTDEBUGMODE_LARGE (0)
#define WATERCURRENTDEBUGMODE_RIPPLES (1)

//
// UnityEngine.Rendering.HighDefinition.WaterDeformationAtlasSize:  static fields
//
#define WATERDEFORMATIONATLASSIZE_ATLAS_SIZE256 (256)
#define WATERDEFORMATIONATLASSIZE_ATLAS_SIZE512 (512)
#define WATERDEFORMATIONATLASSIZE_ATLAS_SIZE1024 (1024)
#define WATERDEFORMATIONATLASSIZE_ATLAS_SIZE2048 (2048)

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
#define WATERDEBUGMODE_FOAM_MASK (2)
#define WATERDEBUGMODE_CURRENT (3)
#define WATERDEBUGMODE_DEFORMATION (4)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterDebug
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterDebug)
    int _WaterDebugMode;
    int _WaterMaskDebugMode;
    int _WaterCurrentDebugMode;
    float _CurrentDebugMultiplier;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWater)
    uint _BandResolution;
    float _MaxWaveHeight;
    float _SimulationTime;
    float _ScatteringWaveHeight;
    int4 _PatchGroup;
    float4 _PatchSize;
    float4 _PatchOrientation;
    float4 _PatchWindSpeed;
    float4 _PatchDirectionDampener;
    float4 _PatchAmplitudeMultiplier;
    float4 _PatchCurrentSpeed;
    float4 _PatchFadeStart;
    float4 _PatchFadeDistance;
    float4 _PatchFadeValue;
    float2 _GroupOrientation;
    float2 _PaddingW0;
    float _SimulationFoamSmoothness;
    float _JacobianDrag;
    float _SimulationFoamAmount;
    float _PaddingW1;
    float _Choppiness;
    float _DeltaTime;
    float _MaxWaveDisplacement;
    float _MaxRefractionDistance;
    float2 _FoamOffsets;
    float _FoamTilling;
    float _WindFoamAttenuation;
    float4 _TransparencyColor;
    float4 _ScatteringColorTips;
    float _DisplacementScattering;
    int _WaterInitialFrame;
    int _SurfaceIndex;
    float _CausticsRegionSize;
    float4 _WaterUpDirection;
    float4 _DeepFoamColor;
    float _OutScatteringCoefficient;
    float _FoamSmoothness;
    float _HeightBasedScattering;
    float _WaterSmoothness;
    float4 _FoamJacobianLambda;
    int _WaterRefSimRes;
    float _WaterSpectrumOffset;
    int _WaterSampleOffset;
    int _WaterBandCount;
    float2 _WaterMaskRemap;
    float _AmbientScattering;
    int _CausticsBandIndex;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.WaterSectorData
// PackingRules = Exact
struct WaterSectorData
{
    float4 dir0;
    float4 dir1;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesUnderWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesUnderWater)
    float4 _WaterRefractionColor;
    float4 _WaterScatteringColor;
    float _MaxViewDistanceMultiplier;
    float _OutScatteringCoeff;
    float _WaterTransitionSize;
    float _UnderWaterAmbientProbeContribution;
    uint _WaterLineTileCountX;
    uint _WaterLineTileCountY;
    float _PaddingUW1;
    float _PaddingUW2;
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
    float3 upDirection;
    int colorPyramidMipOffset;
    float2 padding;
    float tipScatteringHeight;
    float underWaterAmbientProbeContribution;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterDeformation
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterDeformation)
    float2 _WaterDeformationCenter;
    float2 _WaterDeformationExtent;
    float2 _PaddingWD0;
    int _PaddingWD1;
    int _WaterDeformationResolution;
CBUFFER_END

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
    float waveLength;
    int cubicBlend;
    float bowWaveElevation;
    float peakLocation;
    int waveRepetition;
    float waveSpeed;
    float waveOffset;
    float padding0;
    float padding1;
    float padding2;
    float4 scaleOffset;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterRendering
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterRendering)
    float4 _PatchOffset;
    float2 _GridSize;
    uint _WaterLODCount;
    uint _NumWaterPatches;
    float _FoamIntensity;
    float _CausticsIntensity;
    float2 _CurrentMapInfluence;
    float4 _Group0CurrentRegionScaleOffset;
    float4 _Group1CurrentRegionScaleOffset;
    float2 _WaterMaskScale;
    float2 _WaterMaskOffset;
    float2 _FoamMaskScale;
    float2 _FoamMaskOffset;
    float _CausticsPlaneBlendDistance;
    int _WaterCausticsEnabled;
    uint _WaterRenderingLayer;
    int _WaterProceduralGeometry;
    float _WaterMaxTessellationFactor;
    float _WaterTessellationFadeStart;
    float _WaterTessellationFadeRange;
    int _CameraInUnderwaterRegion;
    float2 _RegionCenter;
    float2 _RegionExtent;
    float4 _WaterAmbientProbe;
    float4x4 _WaterSurfaceTransform;
    float4x4 _WaterSurfaceTransform_Inverse;
    float4x4 _WaterCustomMeshTransform;
    float4x4 _WaterCustomMeshTransform_Inverse;
CBUFFER_END


#endif
