//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit / Render Pipeline / Generate Shader Includes ] instead
//

#ifndef VOLUMETRICCLOUDSMANAGER_CS_HLSL
#define VOLUMETRICCLOUDSMANAGER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesClouds
// PackingRules = Exact
CBUFFER_START(ShaderVariablesClouds)
    float _CloudDomeSize;
    float _HighestCloudAltitude;
    float _LowestCloudAltitude;
    float _EarthRadius;
    int _NumPrimarySteps;
    int _NumLightSteps;
    float2 _Padding0;
    float2 _WindDirection;
    float2 _WindVector;
    float _GlobalWindSpeed;
    float _LargeWindSpeed;
    float _MediumWindSpeed;
    float _SmallWindSpeed;
    int _ExposureSunColor;
    float3 _SunLightColor;
    float3 _SunDirection;
    int _PhysicallyBasedSun;
    float _MultiScattering;
    float _ScatteringDirection;
    float _PowderEffectIntensity;
    float _Padding1;
    float _MaxCloudDistance;
    float _DensityMultiplier;
    float _DensityAmplifier;
    float _ErosionFactor;
    float _CloudMapOffset;
    float _TemporalAccumulationFactor;
    int _AccumulationFrameIndex;
    int _SubPixelIndex;
    float4 _FinalScreenSize;
    float4 _IntermediateScreenSize;
    float4 _TraceScreenSize;
    float2 _HistoryBufferSize;
    float2 _HistoryDepthBufferSize;
    float4 _AmbientProbeCoeffs[7];
CBUFFER_END


#endif
