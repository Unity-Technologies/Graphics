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
    float padding0;
    float3 transparencyColor;
    float outScatteringCoefficient;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWater)
    uint _BandResolution;
    float _MaxWaveHeight;
    float _SimulationTime;
    float _DirectionDampener;
    float4 _WaveAmplitude;
    float4 _WaveDisplacement;
    float4 _BandPatchSize;
    float4 _WindSpeed;
    float2 _WindDirection;
    float _Choppiness;
    float _DeltaTime;
    float _SimulationFoamSmoothness;
    float _JacobianDrag;
    float _SimulationFoamAmount;
    float _SSSMaskCoefficient;
    float _MaxWaveDisplacement;
    float _ScatteringBlur;
    float _MaxRefractionDistance;
    float _WaterSmoothness;
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
    float _WindSpeedMultiplier;
    float4 _FoamJacobianLambda;
    float2 _PaddinwW0;
    int _PaddinwW1;
    int _WaterSampleOffset;
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
    float _EarthRadius;
    float _CausticsIntensity;
    float2 _WaterMaskScale;
    float2 _WaterMaskOffset;
    float2 _FoamMaskScale;
    float2 _FoamMaskOffset;
    float2 _CausticsOffset;
    float _CausticsTiling;
    float _CausticsPlaneOffset;
    float _CausticsPlaneBlendDistance;
    int _WaterCausticsType;
    uint _WaterDecalLayer;
    int _InfiniteSurface;
    float _WaterMaxTessellationFactor;
    float _WaterTessellationFadeStart;
    float _WaterTessellationFadeRange;
    int _PaddingWR1;
CBUFFER_END


#endif
