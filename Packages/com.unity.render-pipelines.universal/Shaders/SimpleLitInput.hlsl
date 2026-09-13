#ifndef UNIVERSAL_SIMPLE_LIT_INPUT_INCLUDED
#define UNIVERSAL_SIMPLE_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/NormalMap.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Emission.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/SurfaceType.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.deprecated.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseMap_TexelSize;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Blend;
    UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Blend)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

static float4 unity_DOTS_Sampled_BaseColor;
static float4 unity_DOTS_Sampled_SpecColor;
static float4 unity_DOTS_Sampled_EmissionColor;
static float  unity_DOTS_Sampled_Cutoff;
static float  unity_DOTS_Sampled_Blend;

void SetupDOTSSimpleLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor);
    unity_DOTS_Sampled_SpecColor     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _SpecColor);
    unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _EmissionColor);
    unity_DOTS_Sampled_Cutoff        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff);
    unity_DOTS_Sampled_Blend         = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Blend);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSSimpleLitMaterialPropertyCaches()

#define _BaseColor          unity_DOTS_Sampled_BaseColor
#define _SpecColor          unity_DOTS_Sampled_SpecColor
#define _EmissionColor      unity_DOTS_Sampled_EmissionColor
#define _Cutoff             unity_DOTS_Sampled_Cutoff
#define _Blend              unity_DOTS_Sampled_Blend

#endif

TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_BaseMap);

half4 GetEmissionColor()                { return _EmissionColor; }
half GetBumpScale()                     { return half(1.0); }

half4 SampleBaseMap(float2 uv)          { return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv); }
half4 SampleBumpMap(float2 uv)          { return SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv); }
half4 SampleEmissionMap(float2 uv)      { return SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv); }
half4 SampleSpecGlossMap(float2 uv)     { return SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv); }

inline void InitializeSimpleLitSurfaceData(float2 uv, out SurfaceData outSurfaceData, bool useSpecGlossMap, bool useSpecularColor, bool useGlossinessFromBaseAlpha, bool useAlphaModulate, bool useNormalMap, bool useEmission)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = SampleBaseMap(uv);
    outSurfaceData.alpha = albedoAlpha.a * _BaseColor.a;
    outSurfaceData.alpha = AlphaDiscard(outSurfaceData.alpha, _Cutoff);

    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    if (useAlphaModulate)
        outSurfaceData.albedo = ApplyAlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

    half4 specularSmoothness = half4(0, 0, 0, 1);
    if (useSpecGlossMap)
        specularSmoothness = SampleSpecGlossMap(uv) * _SpecColor;
    else if (useSpecularColor)
        specularSmoothness = _SpecColor;

    if (useGlossinessFromBaseAlpha)
        specularSmoothness.a = outSurfaceData.alpha;

    outSurfaceData.metallic = 0.0; // unused
    outSurfaceData.specular = specularSmoothness.rgb;
    outSurfaceData.smoothness = specularSmoothness.a;
    outSurfaceData.normalTS = SampleNormal(uv, useNormalMap);
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = SampleEmission(uv, useEmission);
}

#endif
