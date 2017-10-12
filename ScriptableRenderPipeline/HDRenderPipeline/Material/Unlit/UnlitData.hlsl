
//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

void DoAlphaTest(float alpha, float alphaCutoff)
{
    // For Forward (Full forward or ForwardOnlyOpaque in deferred):
    // Opaque geometry always has a depth pre-pass so we never want to do the clip here. For transparent we perform the clip as usual.
#if (SHADER_PASS == SHADERPASS_FORWARD_UNLIT && defined(SURFACE_TYPE_TRANSPARENT))
    clip(alpha - alphaCutoff);
#endif
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    surfaceData.color = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.texCoord0).rgb * _Color.rgb;
    float alpha = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.texCoord0).a * _Color.a;

#ifdef _ALPHATEST_ON
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

    // Builtin Data
    builtinData.opacity = alpha;

    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
    builtinData.emissiveIntensity = _EmissiveIntensity;

#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor * builtinData.emissiveIntensity;
#else
    builtinData.emissiveColor = _EmissiveColor * builtinData.emissiveIntensity;
#endif

    builtinData.velocity = float2(0.0, 0.0);

#if (SHADERPASS == SHADERPASS_DISTORTION)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0).rgb;
    builtinData.distortion = distortion.rg * _DistortionScale;
    builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#else
    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
#endif

    builtinData.depthOffset = 0.0;
}
