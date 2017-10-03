void ADD_IDX(ComputeLayerTexCoord)( float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3, float4 uvMappingMask, float4 uvMappingMaskDetail,
                                    float3 positionWS, int mappingType, float worldScale, inout LayerTexCoord layerTexCoord, float additionalTiling = 1.0)
{
    // Handle uv0, uv1, uv2, uv3 based on _UVMappingMask weight (exclusif 0..1)
    float2 uvBase = uvMappingMask.x * texCoord0 +
                    uvMappingMask.y * texCoord1 +
                    uvMappingMask.z * texCoord2 +
                    uvMappingMask.w * texCoord3;

    // Only used with layered, allow to have additional tiling
    uvBase *= additionalTiling.xx;


    float2 uvDetails =  uvMappingMaskDetail.x * texCoord0 +
                        uvMappingMaskDetail.y * texCoord1 +
                        uvMappingMaskDetail.z * texCoord2 +
                        uvMappingMaskDetail.w * texCoord3;

    uvDetails *= additionalTiling.xx;

    // If base is planar/triplanar then detail map is forced to be planar/triplanar
    ADD_IDX(layerTexCoord.details).mappingType = ADD_IDX(layerTexCoord.base).mappingType = mappingType;
    ADD_IDX(layerTexCoord.details).normalWS = ADD_IDX(layerTexCoord.base).normalWS = layerTexCoord.vertexNormalWS;
    // Copy data for the uvmapping
    ADD_IDX(layerTexCoord.details).triplanarWeights = ADD_IDX(layerTexCoord.base).triplanarWeights = layerTexCoord.triplanarWeights;

    // TODO: Currently we only handle world planar/triplanar but we may want local planar/triplanar.
    // In this case both position and normal need to be convert to object space.

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;

    GetTriplanarCoordinate(GetAbsolutePositionWS(positionWS) * worldScale, uvXZ, uvXY, uvZY);

    // Planar is just XZ of triplanar
    if (mappingType == UV_MAPPING_PLANAR)
    {
        uvBase = uvDetails = uvXZ;
    }

    // Apply tiling options
    ADD_IDX(layerTexCoord.base).uv = TRANSFORM_TEX(uvBase, ADD_IDX(_BaseColorMap));
    // Detail map tiling option inherit from the tiling of the base
    ADD_IDX(layerTexCoord.details).uv = TRANSFORM_TEX(TRANSFORM_TEX(uvDetails, ADD_IDX(_BaseColorMap)), ADD_IDX(_DetailMap));

    ADD_IDX(layerTexCoord.base).uvXZ = TRANSFORM_TEX(uvXZ, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.base).uvXY = TRANSFORM_TEX(uvXY, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.base).uvZY = TRANSFORM_TEX(uvZY, ADD_IDX(_BaseColorMap));
    
    ADD_IDX(layerTexCoord.details).uvXZ = TRANSFORM_TEX(TRANSFORM_TEX(uvXZ, ADD_IDX(_BaseColorMap)), ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvXY = TRANSFORM_TEX(TRANSFORM_TEX(uvXY, ADD_IDX(_BaseColorMap)), ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvZY = TRANSFORM_TEX(TRANSFORM_TEX(uvZY, ADD_IDX(_BaseColorMap)), ADD_IDX(_DetailMap));

    #ifdef SURFACE_GRADIENT
    // This part is only relevant for normal mapping with UV_MAPPING_UVSET
    // Note: This code work only in pixel shader (as we rely on ddx), it should not be use in other context
    ADD_IDX(layerTexCoord.base).tangentWS = uvMappingMask.x * layerTexCoord.vertexTangentWS0 +
                                            uvMappingMask.y * layerTexCoord.vertexTangentWS1 +
                                            uvMappingMask.z * layerTexCoord.vertexTangentWS2 +
                                            uvMappingMask.w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.base).bitangentWS =   uvMappingMask.x * layerTexCoord.vertexBitangentWS0 +
                                                uvMappingMask.y * layerTexCoord.vertexBitangentWS1 +
                                                uvMappingMask.z * layerTexCoord.vertexBitangentWS2 +
                                                uvMappingMask.w * layerTexCoord.vertexBitangentWS3;

    ADD_IDX(layerTexCoord.details).tangentWS =  uvMappingMaskDetail.x * layerTexCoord.vertexTangentWS0 +
                                                uvMappingMaskDetail.y * layerTexCoord.vertexTangentWS1 +
                                                uvMappingMaskDetail.z * layerTexCoord.vertexTangentWS2 +
                                                uvMappingMaskDetail.w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.details).bitangentWS =    uvMappingMaskDetail.x * layerTexCoord.vertexBitangentWS0 +
                                                    uvMappingMaskDetail.y * layerTexCoord.vertexBitangentWS1 +
                                                    uvMappingMaskDetail.z * layerTexCoord.vertexBitangentWS2 +
                                                    uvMappingMaskDetail.w * layerTexCoord.vertexBitangentWS3;
    #endif
}

// Caution: Duplicate from GetBentNormalTS - keep in sync!
float3 ADD_IDX(GetNormalTS)(FragInputs input, LayerTexCoord layerTexCoord, float3 detailNormalTS, float detailMask)
{
    float3 normalTS;

#ifdef _NORMALMAP_IDX
    #ifdef _NORMALMAP_TANGENT_SPACE_IDX
        normalTS = SAMPLE_UVMAPPING_NORMALMAP(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale));
    #else // Object space
        // We forbid scale in case of object space as it make no sense
        // To be able to combine object space normal with detail map then later we will re-transform it to world space.
        // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
        #ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv).xyz * 2.0 - 1.0;
        // no need to renormalize normalOS for SurfaceGradientFromPerturbedNormal
        normalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], TransformObjectToWorldDir(normalOS));
        #else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv), 1.0);
        normalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        #endif
    #endif

    #ifdef _DETAIL_MAP_IDX
        #ifdef SURFACE_GRADIENT
        normalTS += detailNormalTS * detailMask;
        #else
        normalTS = lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask);
        #endif
    #endif
#else
    #ifdef SURFACE_GRADIENT
    normalTS = float3(0.0, 0.0, 0.0); // No gradient
    #else
    normalTS = float3(0.0, 0.0, 1.0);
    #endif
#endif

    return normalTS;
}

// Caution: Duplicate from GetNormalTS - keep in sync!
float3 ADD_IDX(GetBentNormalTS)(FragInputs input, LayerTexCoord layerTexCoord, float3 normalTS, float3 detailNormalTS, float detailMask)
{
    float3 bentNormalTS;

#ifdef _BENTNORMALMAP_IDX
    #ifdef _NORMALMAP_TANGENT_SPACE_IDX
        bentNormalTS = SAMPLE_UVMAPPING_NORMALMAP(ADD_IDX(_BentNormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale));
    #else // Object space
        // We forbid scale in case of object space as it make no sense
        // To be able to combine object space normal with detail map then later we will re-transform it to world space.
        // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
        #ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D(ADD_IDX(_BentNormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv).xyz * 2.0 - 1.0;
        // no need to renormalize normalOS for SurfaceGradientFromPerturbedNormal
        bentNormalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], TransformObjectToWorldDir(normalOS));
        #else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(ADD_IDX(_BentNormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv), 1.0);
        bentNormalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        #endif
    #endif

    #ifdef _DETAIL_MAP_IDX
        #ifdef SURFACE_GRADIENT
        bentNormalTS += detailNormalTS * detailMask;
        #else
        bentNormalTS = lerp(bentNormalTS, BlendNormalRNM(bentNormalTS, detailNormalTS), detailMask);
        #endif
    #endif
#else
    // If there is no bent normal map provided, fallback on regular normal map
    bentNormalTS = normalTS;
#endif

    return bentNormalTS;
}

// Return opacity
float ADD_IDX(GetSurfaceData)(FragInputs input, LayerTexCoord layerTexCoord, out SurfaceData surfaceData, out float3 normalTS, out float3 bentNormalTS)
{
    float alpha = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).a * ADD_IDX(_BaseColor).a;

    // Perform alha test very early to save performance (a killed pixel will not sample textures)
#if defined(_ALPHATEST_ON) && !defined(LAYERED_LIT_SHADER)
    DoAlphaTest(alpha, _AlphaCutoff);
#endif

    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;
#ifdef _DETAIL_MAP_IDX
    detailMask = 1.0;
    #ifdef _MASKMAP_IDX
        detailMask = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).b;
    #endif
    float2 detailAlbedoAndSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details)).rb;
    float detailAlbedo = detailAlbedoAndSmoothness.r;
    float detailSmoothness = detailAlbedoAndSmoothness.g;
    // Resample the detail map but this time for the normal map. This call should be optimize by the compiler
    // We split both call due to trilinear mapping
    detailNormalTS = SAMPLE_UVMAPPING_NORMALMAP_AG(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details), ADD_IDX(_DetailNormalScale));
#endif

    surfaceData.baseColor = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).rgb * ADD_IDX(_BaseColor).rgb;
#ifdef _DETAIL_MAP_IDX
    surfaceData.baseColor *= LerpWhiteTo(2.0 * detailAlbedo, detailMask * ADD_IDX(_DetailAlbedoScale));
    // we saturate to avoid to have a smoothness value above 1
    surfaceData.baseColor = saturate(surfaceData.baseColor);
#endif

    surfaceData.specularOcclusion = 1.0; // Will be setup outside of this function

    surfaceData.normalWS = float3(0.0, 0.0, 0.0); // Need to init this to keep quiet the compiler, but this is overriden later (0, 0, 0) so if we forget to override the compiler may comply.

    normalTS = ADD_IDX(GetNormalTS)(input, layerTexCoord, detailNormalTS, detailMask);
    bentNormalTS = ADD_IDX(GetBentNormalTS)(input, layerTexCoord, normalTS, detailNormalTS, detailMask);

#if defined(_MASKMAP_IDX)
    surfaceData.perceptualSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
    surfaceData.perceptualSmoothness = lerp(ADD_IDX(_SmoothnessRemapMin), ADD_IDX(_SmoothnessRemapMax), surfaceData.perceptualSmoothness);
#else
    surfaceData.perceptualSmoothness = ADD_IDX(_Smoothness);
#endif

#ifdef _DETAIL_MAP_IDX
    surfaceData.perceptualSmoothness *= LerpWhiteTo(2.0 * detailSmoothness, detailMask * ADD_IDX(_DetailSmoothnessScale));
    // we saturate to avoid to have a smoothness value above 1
    surfaceData.perceptualSmoothness = saturate(surfaceData.perceptualSmoothness);
#endif

    // MaskMap is RGBA: Metallic, Ambient Occlusion (Optional), emissive Mask (Optional), Smoothness
#ifdef _MASKMAP_IDX
    surfaceData.metallic = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).r;
    surfaceData.ambientOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).g;
#else
    surfaceData.metallic = 1.0;
    surfaceData.ambientOcclusion = 1.0;
#endif
    surfaceData.metallic *= ADD_IDX(_Metallic);

    // This part of the code is not used in case of layered shader but we keep the same macro system for simplicity
#if !defined(LAYERED_LIT_SHADER)

    // Having individual shader features for each materialID like this allow the compiler to optimize
#ifdef _MATID_SSS
    surfaceData.materialId = MATERIALID_LIT_SSS;
#elif defined(_MATID_ANISO)
    surfaceData.materialId = MATERIALID_LIT_ANISO;
#elif defined(_MATID_SPECULAR)
    surfaceData.materialId = MATERIALID_LIT_SPECULAR;
#elif defined(_MATID_CLEARCOAT)
    surfaceData.materialId = MATERIALID_LIT_CLEAR_COAT;
#else // Default
    surfaceData.materialId = MATERIALID_LIT_STANDARD;
#endif

#ifdef _TANGENTMAP
    #ifdef _NORMALMAP_TANGENT_SPACE_IDX // Normal and tangent use same space
    float3 tangentTS = SAMPLE_UVMAPPING_NORMALMAP(_TangentMap, sampler_TangentMap, layerTexCoord.base, 1.0);
    surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.worldToTangent);
    #else // Object space
    // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
    float3 tangentOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(_TangentMapOS, sampler_TangentMapOS,  layerTexCoord.base.uv), 1.0);
    surfaceData.tangentWS = TransformObjectToWorldDir(tangentOS);
    #endif
#else
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
#endif

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_UVMAPPING_TEXTURE2D(_AnisotropyMap, sampler_AnisotropyMap, layerTexCoord.base).r;
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= ADD_IDX(_Anisotropy);

    surfaceData.subsurfaceProfile = _SubsurfaceProfile;
    surfaceData.subsurfaceRadius  = _SubsurfaceRadius;
    surfaceData.thickness         = _Thickness;

#ifdef _SUBSURFACE_RADIUS_MAP
    surfaceData.subsurfaceRadius *= SAMPLE_UVMAPPING_TEXTURE2D(_SubsurfaceRadiusMap, sampler_SubsurfaceRadiusMap, layerTexCoord.base).r;
#endif

#ifdef _THICKNESSMAP
    surfaceData.thickness *= SAMPLE_UVMAPPING_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, layerTexCoord.base).r;
#endif

    surfaceData.specularColor = _SpecularColor.rgb;
#ifdef _SPECULARCOLORMAP
    surfaceData.specularColor *= SAMPLE_UVMAPPING_TEXTURE2D(_SpecularColorMap, sampler_SpecularColorMap, layerTexCoord.base).rgb;
#endif

    surfaceData.coatNormalWS    = input.worldToTangent[2].xyz; // Assign vertex normal
    surfaceData.coatCoverage    = _CoatCoverage;
    surfaceData.coatIOR         = _CoatIOR;

#else // #if !defined(LAYERED_LIT_SHADER)

    // Mandatory to setup value to keep compiler quiet

    // Layered shader only supports the standard material
    surfaceData.materialId = MATERIALID_LIT_STANDARD;

    // All these parameters are ignore as they are re-setup outside of the layers function
    // Note: any parameters set here must also be set in GetSurfaceAndBuiltinData() layer version
    surfaceData.tangentWS = float3(0.0, 0.0, 0.0);
    surfaceData.anisotropy = 0.0;
    surfaceData.subsurfaceRadius = 0.0;
    surfaceData.thickness = 0.0;
    surfaceData.subsurfaceProfile = 0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatNormalWS = float3(0.0, 0.0, 0.0);
    surfaceData.coatCoverage = 0.0f;
    surfaceData.coatIOR = 0.5;

#endif // #if !defined(LAYERED_LIT_SHADER)

    return alpha;
}
