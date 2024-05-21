//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef VOLUMETRICCLOUDSDEF_CS_HLSL
#define VOLUMETRICCLOUDSDEF_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesClouds
// PackingRules = Exact
CBUFFER_START(ShaderVariablesClouds)
    float _HighestCloudAltitude;
    float _LowestCloudAltitude;
    float _CloudNearPlane;
    float _CameraSpace;
    int _NumPrimarySteps;
    int _NumLightSteps;
    float2 _ShadowRegionSize;
    float2 _WindDirection;
    float2 _WindVector;
    float2 _ShapeNoiseOffset;
    float _VerticalShapeWindDisplacement;
    float _VerticalErosionWindDisplacement;
    float _VerticalShapeNoiseOffset;
    float _LargeWindSpeed;
    float _MediumWindSpeed;
    float _SmallWindSpeed;
    float4 _SunLightColor;
    float4 _SunDirection;
    float4 _CloudMapTiling;
    float _MultiScattering;
    float _PowderEffectIntensity;
    float _NormalizationFactor;
    float _DensityMultiplier;
    float _ShapeFactor;
    float _ShapeScale;
    float _MicroErosionFactor;
    float _MicroErosionScale;
    float _ErosionOcclusion;
    float _ErosionFactor;
    float _ErosionScale;
    float _CloudHistoryInvalidation;
    float4 _ScatteringTint;
    float4 _FinalScreenSize;
    float4 _IntermediateScreenSize;
    float4 _TraceScreenSize;
    float2 _HistoryViewportScale;
    int2 _ReprojDepthMipOffset;
    int _LowResolutionEvaluation;
    int _EnableIntegration;
    int _ValidSceneDepth;
    uint _IntermediateResolutionScale;
    int _AccumulationFrameIndex;
    int _SubPixelIndex;
    float _NearPlaneReprojection;
    float _MaxStepSize;
    float4x4 _CloudsPixelCoordToViewDirWS[2];
    float4x4 _CameraPrevViewProjection[2];
    float _AltitudeDistortion;
    float _ErosionFactorCompensation;
    int _EnableFastToneMapping;
    float _TemporalAccumulationFactor;
    float _FadeInStart;
    float _FadeInDistance;
    float _ImprovedTransmittanceBlend;
    float _PaddingVC0;
    float4 _DistanceBasedWeights[12];
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesCloudsShadows
// PackingRules = Exact
CBUFFER_START(ShaderVariablesCloudsShadows)
    float _ShadowIntensity;
    float _PaddingVCS0;
    int _ShadowCookieResolution;
    float _PaddingVCS1;
    float4 _CloudShadowSunOrigin;
    float4 _CloudShadowSunRight;
    float4 _CloudShadowSunUp;
    float4 _CloudShadowSunForward;
    float4 _CameraPositionPS;
CBUFFER_END


#endif
