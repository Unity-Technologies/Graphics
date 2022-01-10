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
    float4 _BandPatchSize;
    float4 _WindSpeed;
    float2 _WindDirection;
    float _Choppiness;
    float _DeltaTime;
    float _SimulationFoamSmoothness;
    float _SimulationFoamIntensity;
    float _SimulationFoamAmount;
    float _SSSMaskCoefficient;
    float _DispersionAmount;
    float _ScatteringBlur;
    float _MaxRefractionDistance;
    float _WaterSmoothness;
    float2 _FoamOffsets;
    float _FoamTilling;
    float _WindFoamAttenuation;
    float4 _TransparencyColor;
    float4 _ScatteringColorTips;
    float _DisplacementScattering;
    float _ScatteringIntensity;
    int _SurfaceIndex;
    float _CausticsRegionSize;
    float4 _ScatteringLambertLighting;
    float4 _DeepFoamColor;
    float _OutScatteringCoefficient;
    float _FoamSmoothness;
    float _HeightBasedScattering;
    float _PaddingW0;
    float4 _FoamJacobianLambda;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterRendering
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterRendering)
    float2 _GridSize;
    float2 _WaterRotation;
    float4 _PatchOffset;
    float4 _WaterAmbientProbe;
    uint _GridRenderingResolution;
    uint _TesselationMasks;
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
CBUFFER_END


#endif
