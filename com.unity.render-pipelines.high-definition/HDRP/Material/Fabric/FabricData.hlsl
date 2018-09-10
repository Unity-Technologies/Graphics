//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "CoreRP/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "HDRP/Material/MaterialUtilities.hlsl"
#include "HDRP/Material/BuiltinUtilities.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal
    
    // Initial value of the material features
    surfaceData.materialFeatures = 0;
    
// Transform the preprocess macro into a material feature (note that silk flag is deduced from the abscence of this one)
#ifdef _MATERIAL_FEATURE_COTTON_WOOL
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_COTTON_WOOL;
#endif

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_SUBSURFACE_SCATTERING;
#endif

#ifdef _MATERIAL_FEATURE_TRANSMISSION
    surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION;
#endif
    
    // Generate the primary uv coordinates
    float2 uvBase = _UVMappingMask.x * input.texCoord0 +
                    _UVMappingMask.y * input.texCoord1 +
                    _UVMappingMask.z * input.texCoord2 +
                    _UVMappingMask.w * input.texCoord3;

    // Apply tiling and offset
    uvBase = uvBase * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;


    // Generate the detail uv coordinates
    float2 uvDetails =  _UVMappingMaskDetail.x * input.texCoord0 +
                        _UVMappingMaskDetail.y * input.texCoord1 +
                        _UVMappingMaskDetail.z * input.texCoord2 +
                        _UVMappingMaskDetail.w * input.texCoord3;

    // Apply offset and tiling
    uvDetails = uvDetails * _DetailMap_ST.xy + _DetailMap_ST.zw;


// The Mask map also contains the detail mask flag, se we need to read it first
#ifdef _MASKMAP
    float4 maskValue = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uvBase);
#else
    #ifdef _DETAIL_MAP
        // If we have no mask map, but we have a detail map; we use the detail map and the smoothness is the value version
        float4 maskValue = float4(1, 1, 1, _Smoothness);
    #else
        // If we have no mask map, no detail map AO is 1, smoothness is the value and mask
        float4 maskValue = float4(1, 1, 0, _Smoothness);
    #endif
#endif

// We need to start by reading the detail (if any available to override the initial values)
#ifdef _DETAIL_MAP
    float4 detailSample = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, uvDetails);
    float detailAO = detailSample.x * 2.0 - 1.0;
    float detailSmoothness = detailSample.z * 2.0 - 1.0;

    // Handle the normal detail
    float2 detailDerivative = UnpackDerivativeNormalRGorAG(float4(detailSample.w, detailSample.y, 1, 1), _DetailNormalScale);
    float3 detailGradient =  SurfaceGradientFromTBN(detailDerivative, input.worldToTangent[0], input.worldToTangent[1]);
#else
    float4 detailSample = float4(1.0, 0.0, 0.0, 1.0);
    float3 detailGradient = float3(0.0, 0.0, 0.0);
#endif
    
    // The base color of the object mixed with the base color texture
    surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, uvBase).rgb * _BaseColor.rgb;

    // Extract the alpha value (will be useful if we need to trigger the alpha test)
    float alpha = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, uvBase).a * _BaseColor.a * detailSample.r;

#ifdef _NORMALMAP
    float2 derivative = UnpackDerivativeNormalRGorAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvBase), _NormalScale);
    #ifdef _DETAIL_MAP
        float3 gradient =  SurfaceGradientFromTBN(derivative, input.worldToTangent[0], input.worldToTangent[1]) + detailGradient * maskValue.z;
    #else
        float3 gradient =  SurfaceGradientFromTBN(derivative, input.worldToTangent[0], input.worldToTangent[1]);
    #endif
    surfaceData.normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], gradient);
#else
    #ifdef _DETAIL_MAP
        surfaceData.normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], detailGradient);
    #else
        surfaceData.normalWS = input.worldToTangent[2];
    #endif
#endif

#ifdef _TANGENTMAP
    float3 tangentTS = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(_TangentMap, sampler_TangentMap, uvBase, 1.0));
    surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.worldToTangent);
#else
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
#endif

    // Make the tagent match the normal
    surfaceData.tangentWS = Orthonormalize(input.worldToTangent[0], surfaceData.normalWS);


#ifdef _MASKMAP
    surfaceData.ambientOcclusion = lerp(_AORemapMin, _AORemapMax, maskValue.y);
    surfaceData.perceptualSmoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, maskValue.w);
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#else
    surfaceData.ambientOcclusion = maskValue.y;
    surfaceData.perceptualSmoothness = maskValue.w;
    surfaceData.specularOcclusion = 1.0;
#endif

// If a detail map was provided, modify the matching smoothness
#ifdef _DETAIL_MAP
    float smoothnessDetailSpeed = saturate(abs(detailSmoothness) * _DetailSmoothnessScale);
    float smoothnessOverlay = lerp(surfaceData.perceptualSmoothness, (detailSmoothness < 0.0) ? 0.0 : 1.0, smoothnessDetailSpeed);
    surfaceData.perceptualSmoothness = lerp(surfaceData.perceptualSmoothness, saturate(smoothnessOverlay), maskValue.z);
#endif
    
// If a detail map was provided, modify the matching ao
#ifdef _DETAIL_MAP
    float aoDetailSpeed = saturate(abs(detailAO) * _DetailAOScale);
    float aoOverlay = lerp(surfaceData.ambientOcclusion, (aoDetailSpeed < 0.0) ? 0.0 : 1.0, aoDetailSpeed);
    surfaceData.ambientOcclusion = lerp(surfaceData.ambientOcclusion, saturate(aoOverlay), maskValue.z);
#endif

    // Propagate the fuzz tint
    surfaceData.fuzzTint = _FuzzTint.xyz;

#ifdef _FUZZDETAIL_MAP
    surfaceData.fuzzTint *= SAMPLE_TEXTURE2D(_FuzzDetailMap, sampler_FuzzDetailMap, uvDetails).rgb;
#endif

#ifdef _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    surfaceData.diffusionProfile = _DiffusionProfile;
    #ifdef _SUBSURFACEMASK
        float4 subSurfaceMaskSample = SAMPLE_TEXTURE2D(_SubsurfaceMaskMap, sampler_SubsurfaceMaskMap, uvBase);
        surfaceData.subsurfaceMask = subSurfaceMaskSample.x;
    #else
        surfaceData.subsurfaceMask = _SubsurfaceMask;
    #endif
#else
    surfaceData.subsurfaceMask = 0.0;
    surfaceData.diffusionProfile = 0;
#endif

#ifdef _MATERIALFEATUREFLAGS_FABRIC_TRANSMISSION
    float4 subSurfaceMaskSample = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, uvBase);
    surfaceData.thickness = dot(SAMPLE_TEXTURE2D_SCALE_BIAS(_ThicknessMap), _ThicknessMapChannelMask);
    surfaceData.thickness = lerp(_ThicknessMapRange.x, _ThicknessMapRange.y, surfaceData.thickness);
    surfaceData.thickness = lerp(_Thickness, surfaceData.thickness, _ThicknessUseMap);
    surfaceData.thickness = _ThicknessRemap.x +  surfaceData.thickness * _ThicknessRemap.y;
#else
    surfaceData.thickness = _Thickness;
#endif

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_TEXTURE2D(_AnisotropyMap, sample_AnisotropyMap, uvBase).x;
#else
    surfaceData.anisotropy = _Anisotropy;
#endif

#ifdef _ALPHATEST_ON
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, uvBase, _BaseColorMap, _BaseColorMap_TexelSize, _BaseColorMap_MipInfo, surfaceData.baseColor);
    }
#endif

    // -------------------------------------------------------------
    // Builtin Data
    // -------------------------------------------------------------

    // For back lighting we use the oposite vertex normal 
    InitBuiltinData(alpha, surfaceData.normalWS, -input.worldToTangent[2], input.positionRWS, input.texCoord1, input.texCoord2, builtinData);
    
    // Support the emissive color and map
    builtinData.emissiveColor = _EmissiveColor * lerp(float3(1.0, 1.0, 1.0), surfaceData.baseColor.rgb, _AlbedoAffectEmissive);
#ifdef _EMISSIVE_COLOR_MAP
    // Generate the primart uv coordinates
    float2 uvEmissive = _UVMappingMaskEmissive.x * input.texCoord0 +
                    _UVMappingMaskEmissive.y * input.texCoord1 +
                    _UVMappingMaskEmissive.z * input.texCoord2 +
                    _UVMappingMaskEmissive.w * input.texCoord3;
    
    uvEmissive = uvEmissive * _EmissiveColorMap_ST.xy + _EmissiveColorMap_ST.zw;

    builtinData.emissiveColor *= SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, uvEmissive).rgb;
#endif

    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}
