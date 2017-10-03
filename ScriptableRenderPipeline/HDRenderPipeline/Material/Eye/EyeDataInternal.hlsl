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
    GetTriplanarCoordinate(positionWS * worldScale, uvXZ, uvXY, uvZY);

    // Planar is just XZ of triplanar
    if (mappingType == UV_MAPPING_PLANAR)
    {
        uvBase = uvXZ;
    }

    // Apply tiling options
    ADD_IDX(layerTexCoord.base).uv = TRANSFORM_TEX(uvBase, ADD_IDX(_BaseColorMap));

    ADD_IDX(layerTexCoord.base).uvXZ = TRANSFORM_TEX(uvXZ, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.base).uvXY = TRANSFORM_TEX(uvXY, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.base).uvZY = TRANSFORM_TEX(uvZY, ADD_IDX(_BaseColorMap));

    #ifdef SURFACE_GRADIENT
    // This part is only relevant for normal mapping with UV_MAPPING_UVSET
    // Note: This code work only in pixel shader (as we rely on ddx), it should not be use in other context
    ADD_IDX(layerTexCoord.base).tangentWS = ADD_IDX(_UVMappingMask).x * layerTexCoord.vertexTangentWS0 +
                                            ADD_IDX(_UVMappingMask).y * layerTexCoord.vertexTangentWS1 +
                                            ADD_IDX(_UVMappingMask).z * layerTexCoord.vertexTangentWS2 +
                                            ADD_IDX(_UVMappingMask).w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.base).bitangentWS =   ADD_IDX(_UVMappingMask).x * layerTexCoord.vertexBitangentWS0 +
                                                ADD_IDX(_UVMappingMask).y * layerTexCoord.vertexBitangentWS1 +
                                                ADD_IDX(_UVMappingMask).z * layerTexCoord.vertexBitangentWS2 +
                                                ADD_IDX(_UVMappingMask).w * layerTexCoord.vertexBitangentWS3;

    ADD_IDX(layerTexCoord.details).tangentWS =  ADD_IDX(_UVDetailsMappingMask).x * layerTexCoord.vertexTangentWS0 +
                                                ADD_IDX(_UVDetailsMappingMask).y * layerTexCoord.vertexTangentWS1 +
                                                ADD_IDX(_UVDetailsMappingMask).z * layerTexCoord.vertexTangentWS2 +
                                                ADD_IDX(_UVDetailsMappingMask).w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.details).bitangentWS =    ADD_IDX(_UVDetailsMappingMask).x * layerTexCoord.vertexBitangentWS0 +
                                                    ADD_IDX(_UVDetailsMappingMask).y * layerTexCoord.vertexBitangentWS1 +
                                                    ADD_IDX(_UVDetailsMappingMask).z * layerTexCoord.vertexBitangentWS2 +
                                                    ADD_IDX(_UVDetailsMappingMask).w * layerTexCoord.vertexBitangentWS3;
    #endif
}

float3 ADD_IDX(GetNormalTS)(FragInputs input, LayerTexCoord layerTexCoord, float3 detailNormalTS, float detailMask, bool useBias, float bias)
{
    float3 normalTS;

#ifdef _NORMALMAP_IDX
    #ifdef _NORMALMAP_TANGENT_SPACE_IDX
    if (useBias)
    {
        normalTS = SAMPLE_UVMAPPING_NORMALMAP_BIAS(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale), bias);
    }
    else
    {
        normalTS = SAMPLE_UVMAPPING_NORMALMAP(ADD_IDX(_NormalMap), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base), ADD_IDX(_NormalScale));
    }
    #else // Object space
    // We forbid scale in case of object space as it make no sense
    // To be able to combine object space normal with detail map then later we will re-transform it to world space.
    // Note: There is no such a thing like triplanar with object space normal, so we call directly 2D function
    if (useBias)
    {
        #ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D_BIAS(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv, bias).xyz * 2.0 - 1.0;
        // no need to renormalize normalOS for SurfaceGradientFromPerturbedNormal
        normalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], normalOS);
        #else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D_BIAS(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv, bias), 1.0);
        normalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        #endif
    }
    else
    {
        #ifdef SURFACE_GRADIENT
        // /We need to decompress the normal ourselve here as UnpackNormalRGB will return a surface gradient
        float3 normalOS = SAMPLE_TEXTURE2D(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv).xyz * 2.0 - 1.0;
        // no need to renormalize normalOS for SurfaceGradientFromPerturbedNormal
        normalTS = SurfaceGradientFromPerturbedNormal(input.worldToTangent[2], TransformObjectToWorldDir(normalOS));
        #else
        float3 normalOS = UnpackNormalRGB(SAMPLE_TEXTURE2D(ADD_IDX(_NormalMapOS), SAMPLER_NORMALMAP_IDX, ADD_IDX(layerTexCoord.base).uv), 1.0);
        normalTS = TransformObjectToTangent(normalOS, input.worldToTangent);
        #endif
    }
    #endif

    #ifdef _DETAIL_MAP_IDX
        #ifdef SURFACE_GRADIENT
        normalTS += detailNormalTS;
        #else
        normalTS = BlendNormalRNM(normalTS, detailNormalTS);
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

static int bayer4x4[16] = {  0,  8,  2, 10,
                            12,  4, 14,  6,
                             3, 11,  1,  9,
                            15,  7, 13,  5 };

float BayerDither4x4(float2 uv){
    int i = int(fmod(uv.x, 4));
    int j = int(fmod(uv.y, 4));
    return bayer4x4[(i + j * 4.0)] / 16.0;
}


// Return opacity
float ADD_IDX(GetSurfaceData)(FragInputs input, LayerTexCoord layerTexCoord, out SurfaceData surfaceData, out float3 normalTS)
{
    float alpha = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.fuzz)).a * ADD_IDX(_BaseColor).a;

    // Perform alha test very early to save performance (a killed pixel will not sample textures)

    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;
#ifdef _DETAIL_MAP_IDX
    detailMask = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMask), SAMPLER_DETAILMASK_IDX, ADD_IDX(layerTexCoord.fuzz)).r;
    float4 detailAlbedoAndSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details));
    float detailAlbedo = detailAlbedoAndSmoothness.r;
    float detailSmoothness = detailAlbedoAndSmoothness.g;
    float detailAlpha = detailAlbedoAndSmoothness.a;
    // Resample the detail map but this time for the normal map. This call should be optimize by the compiler
    // We split both call due to trilinear mapping
    detailNormalTS = SAMPLE_UVMAPPING_NORMALMAP_AG(ADD_IDX(_DetailMap), SAMPLER_DETAILMAP_IDX, ADD_IDX(layerTexCoord.details), ADD_IDX(_DetailNormalScale));
#endif

#if defined(_ALPHATEST_ON) && !defined(LAYERED_LIT_SHADER)
    #ifdef _DETAIL_MAP_IDX
    	alpha *= detailAlpha;
    #endif
    alpha = alpha > _AlphaCutoffOpacityThreshold ? 1.0 : alpha;
     //Dither
    //----------------------------------
    float a0 = round(alpha);
    float a1 = 1 - a0;
    float ditherSample = BayerDither4x4(input.unPositionSS.xy);
    float ditherPattern = (abs(a0 - alpha) < ditherSample) ? a1 : a0;
    alpha = alpha > ditherPattern ? 1.0 : alpha;
    //----------------------------------
    clip(alpha - _AlphaCutoff); // Let artists make prepass cutout thinner

#endif

    surfaceData.baseColor = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).rgb * ADD_IDX(_BaseColor).rgb;
#ifdef _DETAIL_MAP_IDX
    surfaceData.baseColor = surfaceData.baseColor*lerp(1,detailAlbedo, ADD_IDX(_DetailAlbedoScale))+_DetailFuzz1*detailMask;
#endif

#ifdef _SPECULAROCCLUSIONMAP_IDX
    // TODO: Do something. For now just take alpha channel
    surfaceData.specularOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_SpecularOcclusionMap), SAMPLER_SPECULAROCCLUSIONMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
#else
    // The specular occlusion will be perform outside the internal loop
    surfaceData.specularOcclusion = 1.0;
#endif
    surfaceData.normalWS = float3(0.0, 0.0, 0.0); // Need to init this to keep quiet the compiler, but this is overriden later (0, 0, 0) so if we forget to override the compiler may comply.

    normalTS = ADD_IDX(GetNormalTS)(input, layerTexCoord, detailNormalTS, 1.0, false, 0.0);

#if defined(_MASKMAP_IDX)
    surfaceData.perceptualSmoothness = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).a;
#else
    surfaceData.perceptualSmoothness = 1.0;
#endif
    surfaceData.perceptualSmoothness *= ADD_IDX(_Smoothness);
#ifdef _DETAIL_MAP_IDX
    surfaceData.perceptualSmoothness *= 2.0 * saturate(detailSmoothness * ADD_IDX(_DetailSmoothnessScale));
#endif

    // MaskMap is RGBA: Metallic, Ambient Occlusion (Optional), emissive Mask (Optional), Smoothness
#ifdef _MASKMAP_IDX
    surfaceData.ambientOcclusion = SAMPLE_UVMAPPING_TEXTURE2D(ADD_IDX(_MaskMap), SAMPLER_MASKMAP_IDX, ADD_IDX(layerTexCoord.base)).g;
#else
    surfaceData.ambientOcclusion = 1.0;
#endif
    
    // This part of the code is not used in case of layered shader but we keep the same macro system for simplicity
#if !defined(LAYERED_LIT_SHADER)

    // TODO: In order to let the compiler optimize in case of forward rendering this need to be a variant (shader feature) and not a parameter!
    surfaceData.materialId = _MaterialID;

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

    surfaceData.anisotropy = 0.0;

    surfaceData.specular = 0.04;

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

#else // #if !defined(LAYERED_LIT_SHADER)

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

#endif // #if !defined(LAYERED_LIT_SHADER)

    return alpha;
}
