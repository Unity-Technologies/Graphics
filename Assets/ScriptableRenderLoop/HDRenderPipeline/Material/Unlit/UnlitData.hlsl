
//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    surfaceData.color = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.texCoord0).rgb * _Color.rgb;
    float alpha = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.texCoord0).a * _Color.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
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

#ifdef _DISTORTION_ON
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0).rgb;
    builtinData.distortion = distortion.rg;
    builtinData.distortionBlur = distortion.b;
#else
    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
#endif

    builtinData.depthOffset = 0.0;
}

#ifdef TESSELLATION_ON

float4 TessellationEdge(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2)
{
    return DistanceBasedTess(p0, p1, p2, 0.0, _TessellationFactorMaxDistance, unity_ObjectToWorld, _WorldSpaceCameraPos) *  _TessellationFactorFixed.xxxx;
}

void Displacement(inout Attributes v)
{
#ifdef _HEIGHTMAP
    float height = (SAMPLE_TEXTURE2D_LOD(ADD_ZERO_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), v.uv0, 0).r - ADD_ZERO_IDX(_HeightCenter)) * ADD_IDX(_HeightAmplitude);
#else
    float height = 0.0;
#endif

#if (SHADERPASS != SHADERPASS_VELOCITY) && (SHADERPASS != SHADERPASS_DISTORTION)
    v.positionOS.xyz += height * v.normalOS;
#endif
}

#endif
