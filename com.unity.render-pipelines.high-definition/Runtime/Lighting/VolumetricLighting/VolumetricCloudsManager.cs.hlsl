//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef VOLUMETRICCLOUDSMANAGER_CS_HLSL
#define VOLUMETRICCLOUDSMANAGER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesClouds
// PackingRules = Exact
CBUFFER_START(ShaderVariablesClouds)
    float _CloudDomeSize;
    float _HighestCloudAltitude;
    float _LowestCloudAltitude;
    int _NumPrimarySteps;
    int _NumLightSteps;
    float3 _ScatteringTint;
    float _EccentricityF;
    float _EccentricityB;
    float _PhaseFunctionBlend;
    float _PowderEffectIntensity;
    int _ExposureSunColor;
    float3 _SunLightColor;
    float3 _SunDirection;
    int _AccumulationFrameIndex;
    float3 _WindDirection;
    float _MultiScattering;
    float _DensityMultiplier;
    float3 _Padding;
    float4 _AmbientProbeCoeffs[7];
CBUFFER_END


#endif
