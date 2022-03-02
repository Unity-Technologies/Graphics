//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATERSYSTEMDEF_CS_HLSL
#define WATERSYSTEMDEF_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.WaterSurfaceProfile
// PackingRules = Exact
struct WaterSurfaceProfile
{
    float3 waterAmbientProbe;
    float tipScatteringHeight;
    float bodyScatteringHeight;
    float maxRefractionDistance;
    uint lightLayers;
    int cameraUnderWater;
    float3 transparencyColor;
    float outScatteringCoefficient;
    float3 scatteringColor;
    float padding0;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWater)
    uint _BandResolution;
    float _MaxWaveHeight;
    float _SimulationTime;
    float _PaddingW0;
    float4 _DirectionDampener;
    float4 _BandPatchSize;
    float4 _WindSpeed;
    float4 _WindDirection;
    float _SwellCurrent;
    float _RipplesCurrent;
    float _Choppiness;
    float _DeltaTime;
    float _SimulationFoamSmoothness;
    float _JacobianDrag;
    float _SimulationFoamAmount;
    float _SSSMaskCoefficient;
    float _MaxWaveDisplacement;
    float _MaxRefractionDistance;
    float _WaterSmoothness;
    float _WaterMaxAmplitude;
    float2 _FoamOffsets;
    float _FoamTilling;
    float _WindFoamAttenuation;
    float4 _TransparencyColor;
    float4 _ScatteringColorTips;
    float _DisplacementScattering;
    int _WaterInitialFrame;
    int _SurfaceIndex;
    float _CausticsRegionSize;
    float4 _ScatteringLambertLighting;
    float4 _DeepFoamColor;
    float _OutScatteringCoefficient;
    float _FoamSmoothness;
    float _HeightBasedScattering;
    float _PaddingW1;
    float4 _FoamJacobianLambda;
    int _WaterRefSimRes;
    float _WaterSpectrumOffset;
    int _WaterSampleOffset;
    int _WaterBandCount;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterRendering
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterRendering)
    float2 _GridSize;
    float2 _WaterRotation;
    float4 _PatchOffset;
    float4 _WaterAmbientProbe;
    uint _WaterLODCount;
    uint _NumWaterPatches;
    float _PaddingWR0;
    float _CausticsIntensity;
    float2 _WaterMaskScale;
    float2 _WaterMaskOffset;
    float2 _FoamMaskScale;
    float2 _FoamMaskOffset;
    float2 _CausticsOffset;
    float _CausticsTiling;
    float _PaddingWR1;
    float _CausticsPlaneBlendDistance;
    int _WaterCausticsType;
    uint _WaterDecalLayer;
    int _InfiniteSurface;
    float _WaterMaxTessellationFactor;
    float _WaterTessellationFadeStart;
    float _WaterTessellationFadeRange;
    int _CameraInUnderwaterRegion;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesUnderWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesUnderWater)
    float4 _WaterRefractionColor;
    float4 _WaterScatteringColor;
    float _MaxViewDistanceMultiplier;
    float _OutScatteringCoeff;
    float _WaterTransitionSize;
    float _PaddingUW;
CBUFFER_END


#endif
