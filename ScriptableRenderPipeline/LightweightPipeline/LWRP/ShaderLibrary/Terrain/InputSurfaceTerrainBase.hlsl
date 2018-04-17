#ifndef LIGHTWEIGHT_INPUT_SURFACE_TERRAIN_BASE_INCLUDED
#define LIGHTWEIGHT_INPUT_SURFACE_TERRAIN_BASE_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"
#include "CoreRP/ShaderLibrary/CommonMaterial.hlsl"
#include "LWRP/ShaderLibrary/InputSurfaceCommon.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _MainTex_ST;
half4 _Color;
half _Cutoff;
CBUFFER_END

TEXTURE2D(_MetallicTex);   SAMPLER(sampler_MetallicTex);

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;
    specGloss = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, uv);
    specGloss.a = albedoAlpha;
    return specGloss;
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoSmoothness= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    outSurfaceData.alpha = 1;

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoSmoothness.a);
    outSurfaceData.albedo = albedoSmoothness.rgb;

    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap));
    outSurfaceData.occlusion = 1;
    outSurfaceData.emission = 0;
}

#endif // LIGHTWEIGHT_INPUT_SURFACE_TERRAIN_BASE_INCLUDED
