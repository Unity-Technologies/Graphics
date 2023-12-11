// If you edit this file, be sure to check out if your moditications will impact CustomPassRenderers.hlsl
// and CustomPassRenderersShader.template

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#if !defined(SHADER_STAGE_RAY_TRACING)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
#endif
//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
{
    float2 unlitColorMapUv = TRANSFORM_TEX(input.texCoord0.xy, _UnlitColorMap);
    surfaceData.color = SAMPLE_TEXTURE2D(_UnlitColorMap, sampler_UnlitColorMap, unlitColorMapUv).rgb * _UnlitColor.rgb;

    float alpha = 1.0f;
#ifdef DEBUG_DISPLAY
    if (_DebugMipMapMode == DEBUGMIPMAPMODE_NONE)
#endif
    {
        alpha = SAMPLE_TEXTURE2D(_UnlitColorMap, sampler_UnlitColorMap, unlitColorMapUv).a;
        alpha = lerp(_AlphaRemapMin, _AlphaRemapMax, alpha);
        alpha *= _UnlitColor.a;
    }

    // The shader graph can require to export the geometry normal. We thus need to initialize this variable
    surfaceData.normalWS = 0.0;

#if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_PATH_TRACING)
    // Also initialize shadow tint to avoid warning (although it won't be used in that context)
    surfaceData.shadowTint = 0.0;
#endif

#ifdef _ALPHATEST_ON
    GENERIC_ALPHA_TEST(alpha, _AlphaCutoff);
#endif

    // Builtin Data
    ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
    builtinData.opacity = alpha;

#ifdef _ALPHATEST_ON
    // Used for sharpening by alpha to mask
    builtinData.alphaClipTreshold = _AlphaCutoff;
#endif

#if defined(DEBUG_DISPLAY)
    // Light Layers are currently not used for the Unlit shader (because it is not lit)
    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
    // display in the light layers visualization mode, therefore we need the renderingLayers
    builtinData.renderingLayers = GetMeshRenderingLayerMask();
#endif

#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, TRANSFORM_TEX(input.texCoord0.xy, _EmissiveColorMap)).rgb * _EmissiveColor;
#else
    builtinData.emissiveColor = _EmissiveColor;
#endif

    // Note: The code below is used only with EmmissiveMesh generated from Area Light with the option in the UX - So this code don't need
    // to be present for shader graph
#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT || SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER || SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
    builtinData.emissiveColor *= _IncludeIndirectLighting;
#elif SHADERPASS == SHADERPASS_PATH_TRACING
    if (rayCone.spreadAngle < 0.0)
    {
        builtinData.emissiveColor *= _IncludeIndirectLighting;
    }
#endif

    // Inverse pre-expose using _EmissiveExposureWeight weight
    float3 emissiveRcpExposure = builtinData.emissiveColor * GetInverseCurrentExposureMultiplier();
    builtinData.emissiveColor = lerp(emissiveRcpExposure, builtinData.emissiveColor, _EmissiveExposureWeight);

#if (SHADERPASS == SHADERPASS_DISTORTION) || defined(DEBUG_DISPLAY)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, TRANSFORM_TEX(input.texCoord0.xy, _DistortionVectorMap)).rgb;
    distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
    builtinData.distortion = distortion.rg * _DistortionScale;
    builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#endif

#if defined(DEBUG_DISPLAY) && !defined(SHADER_STAGE_RAY_TRACING) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.color = GET_TEXTURE_STREAMING_DEBUG(posInput.positionSS, input.texCoord0);
    }
#endif

    ApplyDebugToBuiltinData(builtinData);

    RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
}
