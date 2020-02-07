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
    float alpha = SAMPLE_TEXTURE2D(_UnlitColorMap, sampler_UnlitColorMap, unlitColorMapUv).a * _UnlitColor.a;

#ifdef _ALPHATEST_ON
    GENERIC_ALPHA_TEST(alpha, _AlphaCutoff);
#endif

    // Builtin Data
    ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
    builtinData.opacity = alpha;

#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, TRANSFORM_TEX(input.texCoord0.xy, _EmissiveColorMap)).rgb * _EmissiveColor;
#else
    builtinData.emissiveColor = _EmissiveColor;
#endif

#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT || SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER
    builtinData.emissiveColor *= _IncludeIndirectLighting;
#elif SHADERPASS == SHADERPASS_RAYTRACING_FORWARD || SHADERPASS == SHADERPASS_PATH_TRACING
    if(rayCone.spreadAngle < 0.0)
    {
        builtinData.emissiveColor *= _IncludeIndirectLighting;
    }
#endif

    // Inverse pre-expose using _EmissiveExposureWeight weight
    float3 emissiveRcpExposure = builtinData.emissiveColor * GetInverseCurrentExposureMultiplier();
    builtinData.emissiveColor = lerp(emissiveRcpExposure, builtinData.emissiveColor, _EmissiveExposureWeight);

#if (SHADERPASS == SHADERPASS_DISTORTION) || defined(DEBUG_DISPLAY)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0.xy).rgb;
    distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
    builtinData.distortion = distortion.rg * _DistortionScale;
    builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#endif

#if defined(DEBUG_DISPLAY) && !defined(SHADER_STAGE_RAY_TRACING)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.color = GetTextureDataDebug(_DebugMipMapMode, unlitColorMapUv, _UnlitColorMap, _UnlitColorMap_TexelSize, _UnlitColorMap_MipInfo, surfaceData.color);
    }
#endif

    RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
}
