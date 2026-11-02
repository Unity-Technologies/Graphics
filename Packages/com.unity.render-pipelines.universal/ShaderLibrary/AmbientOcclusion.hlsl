#ifndef AMBIENT_OCCLUSION_INCLUDED
#define AMBIENT_OCCLUSION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusionFactor.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.deprecated.hlsl"

#if !defined(_SCREEN_SPACE_OCCLUSION_KEYWORD_DECLARED)
    #if !defined(_SCREEN_SPACE_OCCLUSION)
        static const bool _SCREEN_SPACE_OCCLUSION = 0;
    #elif DEFINED_NONZERO(_SCREEN_SPACE_OCCLUSION)
        #undef _SCREEN_SPACE_OCCLUSION
        #define _SCREEN_SPACE_OCCLUSION 1
    #endif
#endif

bool ScreenSpaceOcclusionAvailable()
{
    return _SCREEN_SPACE_OCCLUSION;
}

// Ambient occlusion
TEXTURE2D_X(_ScreenSpaceOcclusionTexture);

half SampleAmbientOcclusion(float2 normalizedScreenSpaceUV)
{
    float2 uv = UnityStereoTransformScreenSpaceTex(normalizedScreenSpaceUV);
    #if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
        uv = RemovePretransformRotation(uv, GetScaledScreenParams());
    #endif
    return half(SAMPLE_TEXTURE2D_X(_ScreenSpaceOcclusionTexture, sampler_LinearClamp, uv).x);
}

AmbientOcclusionFactor GetScreenSpaceAmbientOcclusion(float2 normalizedScreenSpaceUV, bool isSurfaceTypeTransparent)
{
    AmbientOcclusionFactor aoFactor;
    aoFactor.directAmbientOcclusion = half(1.0);
    aoFactor.indirectAmbientOcclusion = half(1.0);

    if (!isSurfaceTypeTransparent && _SCREEN_SPACE_OCCLUSION)
    {
        float ssao = saturate(SampleAmbientOcclusion(normalizedScreenSpaceUV) + (1.0 - _AmbientOcclusionParam.x));
        aoFactor.indirectAmbientOcclusion = ssao;
        aoFactor.directAmbientOcclusion = lerp(half(1.0), ssao, _AmbientOcclusionParam.w);
    }

    #if defined(DEBUG_DISPLAY)
    switch(_DebugLightingMode)
    {
        case DEBUGLIGHTINGMODE_LIGHTING_WITHOUT_NORMAL_MAPS:
            aoFactor.directAmbientOcclusion = 0.5;
            aoFactor.indirectAmbientOcclusion = 0.5;
            break;

        case DEBUGLIGHTINGMODE_LIGHTING_WITH_NORMAL_MAPS:
            aoFactor.directAmbientOcclusion *= 0.5;
            aoFactor.indirectAmbientOcclusion *= 0.5;
            break;
    }
    #endif

    return aoFactor;
}

AmbientOcclusionFactor CreateAmbientOcclusionFactor(float2 normalizedScreenSpaceUV, half occlusion, bool isSurfaceTypeTransparent)
{
    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV, isSurfaceTypeTransparent);

    aoFactor.indirectAmbientOcclusion = min(aoFactor.indirectAmbientOcclusion, occlusion);
    return aoFactor;
}

AmbientOcclusionFactor CreateAmbientOcclusionFactor(InputData inputData, SurfaceData surfaceData, bool isSurfaceTypeTransparent)
{
    return CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, surfaceData.occlusion, isSurfaceTypeTransparent);
}

#endif
