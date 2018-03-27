#ifndef LIGHTWEIGHT_INPUT_SURFACE_COMMON_INCLUDED
#define LIGHTWEIGHT_INPUT_SURFACE_COMMON_INCLUDED

#include "Core.hlsl"
#include "CoreRP/ShaderLibrary/Packing.hlsl"
#include "CoreRP/ShaderLibrary/CommonMaterial.hlsl"

TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);

// Must match Lightweigth ShaderGraph master node
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
};

///////////////////////////////////////////////////////////////////////////////
//                      Material Property Helpers                            //
///////////////////////////////////////////////////////////////////////////////
float2 TransformMainTextureCoord(float2 uv)
{
    return TRANSFORM_TEX(uv, _MainTex);
}

half Alpha(half albedoAlpha)
{
#if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) && !defined(_GLOSSINESS_FROM_BASE_ALPHA)
    half alpha = albedoAlpha * _Color.a;
#else
    half alpha = _Color.a;
#endif

#if defined(_ALPHATEST_ON)
    clip(alpha - _Cutoff);
#endif

    return alpha;
}

half4 MainTexture(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
}

half3 Normal(float2 uv)
{
#if _NORMALMAP
    #if BUMP_SCALE_NOT_SUPPORTED
        return UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv));
    #else
        return UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
    #endif
#else
    return half3(0.0h, 0.0h, 1.0h);
#endif
}

half3 Emission(float2 uv)
{
#ifndef _EMISSION
    return 0;
#else
    return SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb;
#endif
}

#endif // LIGHTWEIGHT_INPUT_SURFACE_COMMON_INCLUDED
