#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"

void GetBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, SurfaceData surfaceData, float alpha, float3 bentNormalWS, float depthOffset, float3 emissiveColor, out BuiltinData builtinData)
{
    // For back lighting we use the oposite vertex normal
    InitBuiltinData(posInput, alpha, bentNormalWS, -input.tangentToWorld[2], input.texCoord1, input.texCoord2, builtinData);

    builtinData.emissiveColor = emissiveColor;

    // Inverse pre-expose using _EmissiveExposureWeight weight
    float3 emissiveRcpExposure = builtinData.emissiveColor * GetInverseCurrentExposureMultiplier();
    builtinData.emissiveColor = lerp(emissiveRcpExposure, builtinData.emissiveColor, _EmissiveExposureWeight);

#if (SHADERPASS == SHADERPASS_DISTORTION) || defined(DEBUG_DISPLAY)
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0.xy).rgb;
    distortion.rg = distortion.rg * _DistortionVectorScale.xx + _DistortionVectorBias.xx;
    builtinData.distortion = distortion.rg * _DistortionScale;
    builtinData.distortionBlur = clamp(distortion.b * _DistortionBlurScale, 0.0, 1.0) * (_DistortionBlurRemapMax - _DistortionBlurRemapMin) + _DistortionBlurRemapMin;
#endif

    builtinData.depthOffset = depthOffset;

    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}

float3 GetEmissiveColor(SurfaceData surfaceData)
{
    return _EmissiveColor * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
}

#ifdef _EMISSIVE_COLOR_MAP
float3 GetEmissiveColor(SurfaceData surfaceData, UVMapping emissiveMapMapping)
{
    float3 emissiveColor = GetEmissiveColor(surfaceData);
    emissiveColor *= SAMPLE_UVMAPPING_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, emissiveMapMapping).rgb;
    return emissiveColor;
}
#endif // _EMISSIVE_COLOR_MAP

void GetBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, SurfaceData surfaceData, float alpha, float3 bentNormalWS, float depthOffset, out BuiltinData builtinData)
{
#ifdef _EMISSIVE_COLOR_MAP
    // Use layer0 of LayerTexCoord to retrieve emissive color mapping information
    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    layerTexCoord.vertexNormalWS = input.tangentToWorld[2].xyz;
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(layerTexCoord.vertexNormalWS);

    int mappingType = UV_MAPPING_UVSET;
    #if defined(_EMISSIVE_MAPPING_PLANAR)
    mappingType = UV_MAPPING_PLANAR;
    #elif defined(_EMISSIVE_MAPPING_TRIPLANAR)
    mappingType = UV_MAPPING_TRIPLANAR;
    #endif

    // Be sure that the compiler is aware that we don't use UV1 to UV3 for main layer so it can optimize code
    #ifndef LAYERED_LIT_SHADER
    ComputeLayerTexCoord(
    #else
    ComputeLayerTexCoord0(
    #endif
                            input.texCoord0.xy, input.texCoord1.xy, input.texCoord2.xy, input.texCoord3.xy, _UVMappingMaskEmissive, _UVMappingMaskEmissive,
                            _EmissiveColorMap_ST.xy, _EmissiveColorMap_ST.zw, float2(0.0, 0.0), float2(0.0, 0.0), 1.0, false,
                            input.positionRWS, _TexWorldScaleEmissive,
                            mappingType, layerTexCoord);

    #ifndef LAYERED_LIT_SHADER
    UVMapping emissiveMapMapping = layerTexCoord.base;
    #else
    UVMapping emissiveMapMapping = layerTexCoord.base0;
    #endif

    GetBuiltinData(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, GetEmissiveColor(surfaceData, emissiveMapMapping), builtinData);
#else
    GetBuiltinData(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, GetEmissiveColor(surfaceData), builtinData);
#endif
}

void GetBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, SurfaceData surfaceData, float alpha, float3 bentNormalWS, float depthOffset, UVMapping emissiveMapMapping, out BuiltinData builtinData)
{
#ifdef _EMISSIVE_MAPPING_BASE
    GetBuiltinData(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, GetEmissiveColor(surfaceData, emissiveMapMapping), builtinData);
#else
    GetBuiltinData(input, V, posInput, surfaceData, alpha, bentNormalWS, depthOffset, builtinData);
#endif
}
