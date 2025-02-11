#ifndef HD_SAFE_SCENE_COLOR
#define HD_SAFE_SCENE_COLOR

#ifdef SHADEROPTIONS_PRE_EXPOSITION
float3 HDSceneColorRaw(float2 uv, float lod, bool exposureIsOn)
{
#if defined(SHADERGRAPH_PREVIEW)
    return float3(0.0, 0.0, 0.0);
#endif

    float exposureMultiplier = 1.0;
    if (!exposureIsOn)
        exposureMultiplier = GetInverseCurrentExposureMultiplier();

#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT) && (SHADERPASS != SHADERPASS_PATH_TRACING) && (SHADERPASS != SHADERPASS_RAYTRACING_VISIBILITY) && (SHADERPASS != SHADERPASS_RAYTRACING_FORWARD)
    return SampleCameraColor(uv, lod) * exposureMultiplier;
#endif

#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(CUSTOM_PASS_SAMPLING_HLSL) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_DRAWPROCEDURAL || SHADERPASS == SHADERPASS_BLIT)
    return CustomPassSampleCameraColor(uv, lod) * exposureMultiplier;
#endif

    return float3(0.0, 0.0, 0.0);
}
#endif

void SafeHDSceneColor_float(float2 UV, float lod, bool exposureIsOn, out float3 finalColor)
{
    #if defined(SHADEROPTIONS_PRE_EXPOSITION)
        finalColor = HDSceneColorRaw(float2(UV.x, 1-UV.y), lod, exposureIsOn);
    #else
        finalColor = float3(0.0, 0.0, 0.0);
    #endif   
}

void SafeHDSceneColor_half(half2 UV, half lod, bool exposureIsOn, out half3 finalColor)
{
    SafeHDSceneColor_float(UV, lod, exposureIsOn, finalColor);
}

#endif // HD_SAFE_SCENE_COLOR