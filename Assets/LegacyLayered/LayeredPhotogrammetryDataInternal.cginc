void ADD_IDX(ComputeLayerTexCoord)( float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                                    float3 positionWS, int mappingType, float worldScale, inout LayerTexCoord layerTexCoord, float additionalTiling = 1.0)
{
    // Handle uv0, uv1, uv2, uv3 based on _UVMappingMask weight (exclusif 0..1)
    float2 uvBase = ADD_IDX(_UVMappingMask).x * texCoord0 +
                    ADD_IDX(_UVMappingMask).y * texCoord1 +
                    ADD_IDX(_UVMappingMask).z * texCoord2 +
                    ADD_IDX(_UVMappingMask).w * texCoord3;

    // Only used with layered, allow to have additional tiling
    uvBase *= additionalTiling.xx;


    float2 uvDetails =  ADD_IDX(_UVDetailsMappingMask).x * texCoord0 +
                        ADD_IDX(_UVDetailsMappingMask).y * texCoord1 +
                        ADD_IDX(_UVDetailsMappingMask).z * texCoord2 +
                        ADD_IDX(_UVDetailsMappingMask).w * texCoord3;

    uvDetails *= additionalTiling.xx;

    // If base is planar/triplanar then detail map is forced to be planar/triplanar
    ADD_IDX(layerTexCoord.details).mappingType = ADD_IDX(layerTexCoord.base).mappingType = mappingType;
    ADD_IDX(layerTexCoord.details).normalWS = ADD_IDX(layerTexCoord.base).normalWS = layerTexCoord.vertexNormalWS;

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(positionWS * worldScale, uvXZ, uvXY, uvZY);

    // Planar is just XZ of triplanar
    if (mappingType == UV_MAPPING_PLANAR)
    {
        uvBase = uvDetails = uvXZ;
    }

    // Apply tiling options
    ADD_IDX(layerTexCoord.base).uv = TRANSFORM_TEX(uvBase, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.details).uv = TRANSFORM_TEX(uvDetails, ADD_IDX(_DetailMap));
}

float3 ADD_IDX(GetNormalTS)(FragInputs input, LayerTexCoord layerTexCoord, float3 detailNormalTS, float detailMask, bool useBias, float bias)
{
    float3 normalTS;

#ifdef _NORMALMAP_IDX
    if (useBias)
    {
        normalTS = SAMPLE_UVMAPPING_NORMALMAP_BIAS(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale), bias);
    }
    else
    {
        normalTS = SAMPLE_UVMAPPING_NORMALMAP(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale));
    }

    #ifdef _DETAIL_MAP_IDX
        normalTS = lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask);
    #endif
#else
    normalTS = float3(0.0, 0.0, 1.0);
#endif

    return normalTS;
}

// Return opacity
float ADD_IDX(GetSurfaceData)(FragInputs input, LayerTexCoord layerTexCoord, out SurfaceData surfaceData, out float3 normalTS)
{
    float alpha = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).a * ADD_IDX(_BaseColor).a;

    // Perform alha test very early to save performance (a killed pixel will not sample textures)
#if defined(_ALPHATEST_ON) && !defined(LAYERED_LIT_SHADER)
    clip(alpha - _AlphaCutoff);
#endif

    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;
#ifdef _DETAIL_MAP_IDX
    detailMask = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMask), SAMPLER_DETAILMASK_IDX, ADD_IDX(layerTexCoord.base)).g;
    float2 detailAlbedoAndSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details)).rb;
    float detailAlbedo = detailAlbedoAndSmoothness.r;
    float detailSmoothness = detailAlbedoAndSmoothness.g;
    // Resample the detail map but this time for the normal map. This call should be optimize by the compiler
    // We split both call due to trilinear mapping
    detailNormalTS = SAMPLE_UVMAPPING_NORMALMAP_AG(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details), ADD_IDX(_DetailNormalScale));
#endif

    surfaceData.baseColor = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).rgb * ADD_IDX(_BaseColor).rgb;
#ifdef _DETAIL_MAP_IDX
    surfaceData.baseColor *= LerpWhiteTo(2.0 * saturate(detailAlbedo * ADD_IDX(_DetailAlbedoScale)), detailMask);
#endif

#ifdef _SPECULAROCCLUSIONMAP_IDX
    // TODO: Do something. For now just take alpha channel
    surfaceData.specularOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_SpecularOcclusionMap), SAMPLER_SPECULAROCCLUSIONMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
#else
    // The specular occlusion will be perform outside the internal loop
    surfaceData.specularOcclusion = 1.0;
#endif
    surfaceData.normalWS = float3(0.0, 0.0, 0.0); // Need to init this to keep quiet the compiler, but this is overriden later (0, 0, 0) so if we forget to override the compiler may comply.

    normalTS = ADD_IDX(GetNormalTS)(input, layerTexCoord, detailNormalTS, detailMask, false, 0.0);

#if defined(_MASKMAP_IDX)
    surfaceData.perceptualSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
#else
    surfaceData.perceptualSmoothness = 1.0;
#endif
    surfaceData.perceptualSmoothness *= ADD_IDX(_Smoothness);
#ifdef _DETAIL_MAP_IDX
    surfaceData.perceptualSmoothness *= LerpWhiteTo(2.0 * saturate(detailSmoothness * ADD_IDX(_DetailSmoothnessScale)), detailMask);
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
    // Mandatory to setup value to keep compiler quiet

    // Layered shader only supports the standard material
    surfaceData.materialId = 1; // MaterialId.LitStandard

    // All these parameters are ignore as they are re-setup outside of the layers function
    surfaceData.tangentWS = float3(0.0, 0.0, 0.0);
    surfaceData.anisotropy = 0.0;
    surfaceData.specular = 0.0;

    surfaceData.subsurfaceRadius = 0.0;
    surfaceData.thickness = 0.0;
    surfaceData.subsurfaceProfile = 0;

    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

    return alpha;
}
