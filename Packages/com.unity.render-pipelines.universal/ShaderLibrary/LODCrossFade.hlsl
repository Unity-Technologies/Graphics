#ifndef UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED
#define UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalShaderVariables.hlsl"

#if !defined(LOD_FADE_CROSSFADE_KEYWORD_DECLARED)
    #if !defined(LOD_FADE_CROSSFADE)
        static const bool LOD_FADE_CROSSFADE = 0;
    #elif DEFINED_NONZERO(LOD_FADE_CROSSFADE)
        #undef LOD_FADE_CROSSFADE
        #define LOD_FADE_CROSSFADE 1
    #endif
#endif

TEXTURE2D(_DitheringTexture);

half CopySign(half x, half s)
{
    return (s >= 0) ? abs(x) : -abs(x);
}

void LODFadeCrossFade(float4 positionCS)
{
    if (LOD_FADE_CROSSFADE)
    {
        half2 uv = positionCS.xy * _DitheringTextureInvSize;

        half d = SAMPLE_TEXTURE2D(_DitheringTexture, sampler_PointRepeat, uv).a;

        d = unity_LODFade.x - CopySign(d, unity_LODFade.x);

        clip(d);
    }
}

#endif
