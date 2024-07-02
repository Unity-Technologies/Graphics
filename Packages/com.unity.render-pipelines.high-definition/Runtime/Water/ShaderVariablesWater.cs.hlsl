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
    float2 _GroupOrientation;
    uint _BandResolution;
    int _SurfaceIndex;
    float4 _Band0_ScaleOffset_AmplitudeMultiplier;
    float4 _Band1_ScaleOffset_AmplitudeMultiplier;
    float4 _Band2_ScaleOffset_AmplitudeMultiplier;
    float2 _Band0_Fade;
    float2 _Band1_Fade;
    float2 _Band2_Fade;
    int _DeformationRegionResolution;
    float _WaterFoamRegionResolution;
    float _SimulationFoamIntensity;
    float _SimulationFoamAmount;
    float _WaterFoamTiling;
    float _DecalAtlasScale;
    float2 _DecalRegionScale;
    float2 _DecalRegionOffset;
    float4 _WaterUpDirection;
    float4 _WaterExtinction;
    float _MaxRefractionDistance;
    float _CausticsRegionSize;
    int _CausticsBandIndex;
    float _CausticsMaxLOD;
    float4 _WaterAlbedo;
    float _AmbientScattering;
    float _HeightBasedScattering;
    float _DisplacementScattering;
    float _ScatteringWaveHeight;
    float _FoamCurrentInfluence;
    float _WaterFoamSmoothness;
    float _WaterSmoothness;
    float _FoamPersistenceMultiplier;
    float _CausticsTilingFactor;
    float _CausticsIntensity;
    float _CausticsShadowIntensity;
    float _CausticsPlaneBlendDistance;
    float _MaxWaveDisplacement;
    float _MaxWaveHeight;
    float2 _PaddingW2;
    uint _WaterRenderingLayer;
    float _WaterMaxTessellationFactor;
    float _WaterTessellationFadeStart;
    float _WaterTessellationFadeRange;
    float4x4 _WaterCustomTransform_Inverse;
    float2 _WaterMaskScale;
    float2 _WaterMaskOffset;
    float2 _WaterMaskRemap;
    float2 _CurrentMapInfluence;
    float2 _SimulationFoamMaskScale;
    float2 _SimulationFoamMaskOffset;
    float4 _Group0CurrentRegionScaleOffset;
    float4 _Group1CurrentRegionScaleOffset;
    float _MaxWaterDeformation;
    float _SimulationTime;
    float _DeltaTime;
    float _PaddingW3;
CBUFFER_END


#endif
