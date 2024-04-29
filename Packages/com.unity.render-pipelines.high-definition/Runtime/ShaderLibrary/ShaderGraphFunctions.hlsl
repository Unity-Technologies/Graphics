#ifndef UNITY_GRAPHFUNCTIONS_HD_INCLUDED
#define UNITY_GRAPHFUNCTIONS_HD_INCLUDED

// Due to order of includes (Gradient struct need to be define before the declaration of $splice(GraphProperties))
// And HDRP require that Material.hlsl and BuiltInGI are after it, we have two files to defines shader graph functions, one header and one where we setup HDRP functions

#if defined(REQUIRE_NORMAL_TEXTURE)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#endif

float shadergraph_HDSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE)
    return SampleCameraDepth(uv);
#endif
    return 0;
}

float3 shadergraph_HDSampleSceneNormal(float2 uv)
{
#if defined(REQUIRE_NORMAL_TEXTURE)
    float4 encodedNormal = SAMPLE_TEXTURE2D_X_LOD(_NormalBufferTexture, s_trilinear_clamp_sampler, uv * _RTHandleScale.xy, 0);
    NormalData normalData;
    DecodeFromNormalBuffer(encodedNormal, normalData);
    return normalData.normalWS;
#endif
    return 0;
}

float3 shadergraph_HDSampleSceneColor(float2 uv)
{
#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
    // We always remove the pre-exposure when we sample the scene color
    return SampleCameraColor(uv) * GetInverseCurrentExposureMultiplier();
#endif

// Special code for the Fullscreen target to be able to sample the color buffer at different places in the pipeline
#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(CUSTOM_PASS_SAMPLING_HLSL) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_DRAWPROCEDURAL || SHADERPASS == SHADERPASS_BLIT)
    return CustomPassSampleCameraColor(uv, 0) * GetInverseCurrentExposureMultiplier();
#endif

    return float3(0, 0, 0);
}

float3 shadergraph_HDBakedGI(float3 positionRWS, float3 normalWS, uint2 positionSS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
#if defined(__BUILTINGIUTILITIES_HLSL__)
    bool needToIncludeAPV = true;
    return SampleBakedGI(positionRWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, needToIncludeAPV);
#else
    return 0;
#endif
}

float3 shadergraph_HDMainLightDirection()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
    uint lightIndex = _DirectionalShadowIndex;
    if (_DirectionalShadowIndex < 0)
    {
        if (_DirectionalLightCount == 0)
            return 0.0f;
        lightIndex = 0;
    }
    return _DirectionalLightDatas[lightIndex].forward;
#else
    return 0.0f;
#endif
}


// If we already defined the Macro, now we need to redefine them given that HDRP functions are now defined.
#ifdef SHADERGRAPH_SAMPLE_SCENE_DEPTH
#undef SHADERGRAPH_SAMPLE_SCENE_DEPTH
#endif
#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_HDSampleSceneDepth(uv)


#ifdef SHADERGRAPH_SAMPLE_SCENE_COLOR
#undef SHADERGRAPH_SAMPLE_SCENE_COLOR
#endif
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_HDSampleSceneColor(uv)

#ifdef SHADERGRAPH_SAMPLE_SCENE_NORMAL
#undef SHADERGRAPH_SAMPLE_SCENE_NORMAL
#endif
#define SHADERGRAPH_SAMPLE_SCENE_NORMAL(uv) shadergraph_HDSampleSceneNormal(uv)

#ifdef SHADERGRAPH_BAKED_GI
#undef SHADERGRAPH_BAKED_GI
#endif
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_HDBakedGI(positionWS, normalWS, positionSS, uvStaticLightmap, uvDynamicLightmap, applyScaling)


#ifdef SHADERGRAPH_MAIN_LIGHT_DIRECTION
#undef SHADERGRAPH_MAIN_LIGHT_DIRECTION
#endif
#define SHADERGRAPH_MAIN_LIGHT_DIRECTION shadergraph_HDMainLightDirection

#ifdef SHADERGRAPH_RENDERER_BOUNDS_MIN
#undef SHADERGRAPH_RENDERER_BOUNDS_MIN
#endif
#define SHADERGRAPH_RENDERER_BOUNDS_MIN shadergraph_RendererBoundsWS_Min()

float3 shadergraph_RendererBoundsWS_Min()
{
    float3 minBounds, maxBounds;
    GetRendererBounds(minBounds, maxBounds);
    return minBounds;
}

#ifdef SHADERGRAPH_RENDERER_BOUNDS_MAX
#undef SHADERGRAPH_RENDERER_BOUNDS_MAX
#endif
#define SHADERGRAPH_RENDERER_BOUNDS_MAX shadergraph_RendererBoundsWS_Max()

float3 shadergraph_RendererBoundsWS_Max()
{
    float3 minBounds, maxBounds;
    GetRendererBounds(minBounds, maxBounds);
    return maxBounds;
}

#endif // UNITY_GRAPHFUNCTIONS_HD_INCLUDED
