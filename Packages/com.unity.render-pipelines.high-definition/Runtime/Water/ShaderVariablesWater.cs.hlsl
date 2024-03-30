//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESWATER_CS_HLSL
#define SHADERVARIABLESWATER_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterDebug
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterDebug)
    int _WaterDebugMode;
    int _WaterMaskDebugMode;
    int _WaterCurrentDebugMode;
    float _CurrentDebugMultiplier;
    int _WaterFoamDebugMode;
    int _PaddingWDbg0;
    int _PaddingWDbg1;
    int _PaddingWDbg2;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterPerCamera
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterPerCamera)
    float2 _PatchOffset;
    float2 _GridSize;
    float2 _RegionExtent;
    float _GridSizeMultiplier;
    uint _MaxLOD;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesWaterPerSurface
// PackingRules = Exact
CBUFFER_START(ShaderVariablesWaterPerSurface)
    float4x4 _WaterSurfaceTransform;
    float4x4 _WaterSurfaceTransform_Inverse;
    float4 _PatchOrientation;
    float4 _PatchWindSpeed;
    float4 _PatchDirectionDampener;
    int4 _PatchGroup;
    float2 _WaterMaskScale;
    float2 _WaterMaskOffset;
    float2 _WaterMaskRemap;
    float2 _GroupOrientation;
    float2 _DeformationRegionOffset;
    float2 _DeformationRegionScale;
    float4 _Band0_ScaleOffset_AmplitudeMultiplier;
    float4 _Band1_ScaleOffset_AmplitudeMultiplier;
    float4 _Band2_ScaleOffset_AmplitudeMultiplier;
    float2 _Band0_Fade;
    float2 _Band1_Fade;
    float2 _Band2_Fade;
    uint _BandResolution;
    int _SurfaceIndex;
    float2 _SimulationFoamMaskScale;
    float2 _SimulationFoamMaskOffset;
    float _SimulationFoamIntensity;
    float _SimulationFoamAmount;
    float _WaterFoamRegionResolution;
    float _FoamTiling;
    float2 _FoamRegionScale;
    float2 _FoamRegionOffset;
    float4 _WaterUpDirection;
    float4 _WaterExtinction;
    float _MaxRefractionDistance;
    float _CausticsRegionSize;
    int _CausticsBandIndex;
    float _PaddingW2;
    float4 _WaterAlbedo;
    float _AmbientScattering;
    float _HeightBasedScattering;
    float _DisplacementScattering;
    float _ScatteringWaveHeight;
    float _FoamCurrentInfluence;
    float _FoamSmoothness;
    float _WaterSmoothness;
    float _FoamPersistenceMultiplier;
    float2 _WaterForwardXZ;
    int _DeformationRegionResolution;
    float _CausticsMaxLOD;
    float _CausticsTilingFactor;
    float _CausticsIntensity;
    float _CausticsShadowIntensity;
    float _CausticsPlaneBlendDistance;
    float _MaxWaveDisplacement;
    float _MaxWaveHeight;
    float2 _CurrentMapInfluence;
    float4 _Group0CurrentRegionScaleOffset;
    float4 _Group1CurrentRegionScaleOffset;
    uint _WaterRenderingLayer;
    float _WaterMaxTessellationFactor;
    float _WaterTessellationFadeStart;
    float _WaterTessellationFadeRange;
    float4x4 _WaterCustomTransform_Inverse;
    float _MaxWaterDeformation;
    float _SimulationTime;
    float _DeltaTime;
    float _PaddingW1;
CBUFFER_END


#endif
