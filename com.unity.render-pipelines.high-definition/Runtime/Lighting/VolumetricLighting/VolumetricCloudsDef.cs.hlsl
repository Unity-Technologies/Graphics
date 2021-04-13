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
    float _LargeWindSpeed;
    float _MediumWindSpeed;
    float _SmallWindSpeed;
    int _ExposureSunColor;
    float4 _SunLightColor;
    float4 _SunDirection;
    int _PhysicallyBasedSun;
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
    float _TemporalAccumulationFactor;
    float4 _ScatteringTint;
    float4 _FinalScreenSize;
    float4 _IntermediateScreenSize;
    float4 _TraceScreenSize;
    float2 _HistoryViewportSize;
    float2 _HistoryBufferSize;
    float2 _DepthMipOffset;
    int _AccumulationFrameIndex;
    int _SubPixelIndex;
    float4 _AmbientProbeCoeffs[7];
    float4 _SunRight;
    float4 _SunUp;
    float _ShadowIntensity;
    float _ShadowFallbackValue;
    int _ShadowCookieResolution;
    float _ShadowPlaneOffset;
    float2 _ShadowRegionSize;
    float _Padding0;
    float _Padding1;
CBUFFER_END


#endif
