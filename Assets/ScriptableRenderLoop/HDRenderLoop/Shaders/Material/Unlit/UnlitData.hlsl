//-------------------------------------------------------------------------------------
// FragInput
// This structure gather all possible varying/interpolator for this shader.
//-------------------------------------------------------------------------------------

struct FragInput
{
    float4 positionHS;
    float2 texCoord0;
};

//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

void GetSurfaceAndBuiltinData(FragInput input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    surfaceData.color = UNITY_SAMPLE_TEX2D(_ColorMap, input.texCoord0).rgb * _Color.rgb;
    float alpha = UNITY_SAMPLE_TEX2D(_ColorMap, input.texCoord0).a * _Color.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

    // Builtin Data
    builtinData.opacity = alpha;

    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = UNITY_SAMPLE_TEX2D(_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor;
#else
    builtinData.emissiveColor = _EmissiveColor;
#endif

    builtinData.emissiveIntensity = _EmissiveIntensity;

    builtinData.velocity = float2(0.0, 0.0);

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
}

