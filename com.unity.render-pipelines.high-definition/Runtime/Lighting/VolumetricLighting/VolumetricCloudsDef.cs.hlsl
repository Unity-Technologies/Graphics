//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
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
    float _ErosionOcclusion;
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
    float2 _ShapeNoiseOffset;
    int _AccumulationFrameIndex;
    int _SubPixelIndex;
    float4 _AmbientProbeTop;
    float4 _AmbientProbeBottom;
    float4 _SunRight;
    float4 _SunUp;
    float _ShadowIntensity;
    float _ShadowFallbackValue;
    int _ShadowCookieResolution;
    float _ShadowPlaneOffset;
    float2 _ShadowRegionSize;
    float2 _WorldSpaceShadowCenter;
    float4x4 _CameraViewProjection_NO;
    float4x4 _CameraInverseViewProjection_NO;
    float4x4 _CameraPrevViewProjection_NO;
    float _AltitudeDistortion;
    float _ErosionFactorCompensation;
    int _EnableFastToneMapping;
    float _Padding;
    float4 _DistanceBasedWeights[12];
CBUFFER_END


#endif
