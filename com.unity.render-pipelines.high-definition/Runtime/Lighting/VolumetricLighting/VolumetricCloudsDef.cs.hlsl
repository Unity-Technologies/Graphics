//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit / Render Pipeline / Generate Shader Includes ] instead
//

#ifndef VOLUMETRICCLOUDSDEF_CS_HLSL
#define VOLUMETRICCLOUDSDEF_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesClouds
// PackingRules = Exact
CBUFFER_START(ShaderVariablesClouds)
    float _MaxRayMarchingDistance;
    float _HighestCloudAltitude;
    float _LowestCloudAltitude;
    float _EarthRadius;
    float2 _CloudRangeSquared;
    int _NumPrimarySteps;
    int _NumLightSteps;
    float4 _CloudMapTiling;
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
    float4 _ScatteringTint;
    float _MultiScattering;
    float _ScatteringDirection;
    float _PowderEffectIntensity;
    float _NormalizationFactor;
    float _MaxCloudDistance;
    float _DensityMultiplier;
    float _ShapeFactor;
    float _ErosionFactor;
    float _ShapeScale;
    float _ErosionScale;
    float _CloudMapOffset;
    float _TemporalAccumulationFactor;
    float4 _FinalScreenSize;
    float4 _IntermediateScreenSize;
    float4 _TraceScreenSize;
    float2 _HistoryViewportSize;
    float2 _HistoryBufferSize;
    float2 _DepthMipOffset;
    int _AccumulationFrameIndex;
    int _SubPixelIndex;
    float4 _AmbientProbeCoeffs[7];
    float3 _SunRight;
    float _ShadowIntensity;
    float3 _SunUp;
    float _ShadowFallbackValue;
    int _ShadowCookieResolution;
    float2 _ShadowRegionSize;
    float _ShadowPlaneOffset;
    float _Padding1;
CBUFFER_END


#endif
