//-------------------------------------------------------------------------------------
// FragInput
// This structure gather all possible varying/interpolator for this shader.
//-------------------------------------------------------------------------------------

struct FragInput
{
    float4 unPositionSS;
    float2 texCoord0;
    float2 texCoord1;
};

//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

void GetSurfaceAndBuiltinData(FragInput input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    surfaceData.color = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.texCoord0).rgb * _Color.rgb;
    float alpha = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.texCoord0).a * _Color.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

    // Builtin Data
    builtinData.opacity = alpha;

    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor;
#else
    builtinData.emissiveColor = _EmissiveColor;
#endif

    builtinData.emissiveIntensity = _EmissiveIntensity;

    builtinData.velocity = float2(0.0, 0.0);

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
}

