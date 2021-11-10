//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATERSYSTEMDEF_CS_HLSL
#define WATERSYSTEMDEF_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWater
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWater)
    uint _BandResolution;
    float _MaxWaveHeight;
    float _SimulationTime;
    float _DirectionDampener;
    float4 _WaveAmplitude;
    float4 _BandPatchSize;
    float4 _BandPatchUVScale;
    float4 _WindSpeed;
    float2 _WindDirection;
    float _Choppiness;
    float _DeltaTime;
    float _SurfaceFoamIntensity;
    float _SurfaceFoamAmount;
    float _DeepFoamAmount;
    float _SSSMaskCoefficient;
    float _CloudTexturedAmount;
    float _RefractionNormalWeight;
    float _MaxRefractionDistance;
    float _WaterSmoothness;
    float2 _FoamOffsets;
    float _FoamTilling;
    float _WindFoamAttenuation;
    float3 _ScatteringColorTips;
    float _FoamSmoothness;
    float _Refraction;
    float _RefractionLow;
    float _MaxAbsorptionDistance;
    float _ScatteringBlur;
    float3 _TransparencyColor;
    float _OutScatteringCoefficient;
    float _DisplacementScattering;
    float _ScatteringIntensity;
    float _BodyScatteringWeight;
    float _TipScatteringWeight;
    float4 _ScatteringLambertLighting;
    float3 _DeepFoamColor;
    float _HeightBasedScattering;
    float4 _FoamJacobianLambda;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterRendering
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterRendering)
    float2 _GridSize;
    float2 _WaterRotation;
    float3 _PatchOffset;
    uint _GridRenderingResolution;
    float3 _WaterAmbientProbe;
    uint _TesselationMasks;
    float2 _WaterMaskScale;
    float2 _WaterMaskOffset;
    float2 _FoamMaskScale;
    float2 _FoamMaskOffset;
    float2 _CausticsOffset;
    float _CausticsTiling;
    float _CausticsPlaneOffset;
    float2 _PaddingWR0;
    float _EarthRadius;
    float _CausticsIntensity;
CBUFFER_END


#endif
