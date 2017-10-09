void ADD_IDX(ComputeLayerTexCoord)( // Uv related parameters
                                    float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3, float4 uvMappingMask, float4 uvMappingMaskDetails,
                                    // scale and bias for base and detail + global tiling factor (for layered lit only)
                                    float2 texScale, float2 texBias, float2 texScaleDetails, float2 texBiasDetails, float additionalTiling,
                                    // parameter for planar/triplanar
                                    float3 positionWS, float worldScale,
                                    // mapping type and output
                                    int mappingType, inout LayerTexCoord layerTexCoord)
{
    // Handle uv0, uv1, uv2, uv3 based on _UVMappingMask weight (exclusif 0..1)
    float2 uvBase = uvMappingMask.x * texCoord0 +
                    uvMappingMask.y * texCoord1 +
                    uvMappingMask.z * texCoord2 +
                    uvMappingMask.w * texCoord3;

    // Only used with layered, allow to have additional tiling
    uvBase *= additionalTiling.xx;


    float2 uvDetails =  uvMappingMaskDetails.x * texCoord0 +
                        uvMappingMaskDetails.y * texCoord1 +
                        uvMappingMaskDetails.z * texCoord2 +
                        uvMappingMaskDetails.w * texCoord3;

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
    ADD_IDX(layerTexCoord.base).uv = uvBase * texScale + texBias;
    // Detail map tiling option inherit from the tiling of the base
    ADD_IDX(layerTexCoord.details).uv = (uvDetails * texScaleDetails + texBiasDetails) * texScale + texBias;

    ADD_IDX(layerTexCoord.base).uvXZ = uvXZ * texScale + texBias;
    ADD_IDX(layerTexCoord.base).uvXY = uvXY * texScale + texBias;
    ADD_IDX(layerTexCoord.base).uvZY = uvZY * texScale + texBias;

    ADD_IDX(layerTexCoord.details).uvXZ = (uvXZ * texScaleDetails + texBiasDetails) * texScale + texBias;
    ADD_IDX(layerTexCoord.details).uvXY = (uvXY * texScaleDetails + texBiasDetails) * texScale + texBias;
    ADD_IDX(layerTexCoord.details).uvZY = (uvZY * texScaleDetails + texBiasDetails) * texScale + texBias;

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

    ADD_IDX(layerTexCoord.details).tangentWS =  uvMappingMaskDetails.x * layerTexCoord.vertexTangentWS0 +
                                                uvMappingMaskDetails.y * layerTexCoord.vertexTangentWS1 +
                                                uvMappingMaskDetails.z * layerTexCoord.vertexTangentWS2 +
                                                uvMappingMaskDetails.w * layerTexCoord.vertexTangentWS3;

    ADD_IDX(layerTexCoord.details).bitangentWS =    uvMappingMaskDetails.x * layerTexCoord.vertexBitangentWS0 +
                                                    uvMappingMaskDetails.y * layerTexCoord.vertexBitangentWS1 +
                                                    uvMappingMaskDetails.z * layerTexCoord.vertexBitangentWS2 +
                                                    uvMappingMaskDetails.w * layerTexCoord.vertexBitangentWS3;
    #endif
}

// Return the minimun uv size for all layers including triplanar
float2 ADD_IDX(GetMinUvSize)(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

    if (ADD_IDX(layerTexCoord.base).mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(ADD_IDX(layerTexCoord.base).uvZY * MERGE_NAME(ADD_IDX(_HeightMap), _TexelSize.zw), minUvSize);
        minUvSize = min(ADD_IDX(layerTexCoord.base).uvXZ * MERGE_NAME(ADD_IDX(_HeightMap), _TexelSize.zw), minUvSize);
        minUvSize = min(ADD_IDX(layerTexCoord.base).uvXY * MERGE_NAME(ADD_IDX(_HeightMap), _TexelSize.zw), minUvSize);
    }
    else
    {
        minUvSize = min(ADD_IDX(layerTexCoord.base).uv * MERGE_NAME(ADD_IDX(_HeightMap), _TexelSize.zw), minUvSize);
    }

    return minUvSize;
}

//TODO: #define USE_HEIGHTMAP_INFLUENCE ((LAYER_INDEX != 0) && defined(_MAIN_LAYER_INFLUENCE_MODE) && defined(_HEIGHTMAP0))

// define only once
#if (LAYER_INDEX == 0) && defined(_PIXEL_DISPLACEMENT)
struct PerPixelHeightDisplacementParam
{
    float2 uv;
    TEXTURE2D(heightmap);
    SAMPLER2D(heightmapSampler);
};

// Calculate displacement for per vertex displacement mapping
float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
{
    // Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
    // Tiling is automatically handled correctly here.
    float height = SAMPLE_TEXTURE2D_LOD(param.heightmap, param.heightmapSampler, param.uv + texOffsetCurrent, lod).r;
#if USE_HEIGHTMAP_INFLUENCE
    // TODO: We are suppose to get blendmask and influence mask and apply height inheritance... Crazy in term of performance.... + need to deal with macro stuff
#endif
    return height;
}

#include "../../../Core/ShaderLibrary/PerPixelDisplacement.hlsl"

#endif

float ADD_IDX(ApplyPerPixelDisplacement)(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
{
#if defined(_PIXEL_DISPLACEMENT) && defined(_HEIGHTMAP_IDX)
    // These variables are known at the compile time.
    bool isPlanar = ADD_IDX(layerTexCoord.base).mappingType == UV_MAPPING_PLANAR;
    bool isTriplanar = ADD_IDX(layerTexCoord.base).mappingType == UV_MAPPING_TRIPLANAR;

    // _HeightAmplitude can be negative if min and max are inverted, but the max displacement must be positive
    float  maxHeight = abs(ADD_IDX(_HeightAmplitude));
    #if USE_HEIGHTMAP_INFLUENCE
    // TODO: maxHeight is supposed to be affect, but how ?
    #endif
    // Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y)
    // Inverse tiling scale *= (1 / _TexWorldScale) if planar or triplanar
    #ifdef _DISPLACEMENT_LOCK_TILING_SCALE
    maxHeight *= ADD_IDX(_InvTilingScale);
    // TODO: handle _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    #endif
    float2 minUvSize = ADD_IDX(GetMinUvSize)(layerTexCoord);
    float  lod = ComputeTextureLOD(minUvSize);

    float2 invPrimScale = (isPlanar || isTriplanar) ? float2(1.0, 1.0) : _InvPrimScale.xy;
    float  worldScale = (isPlanar || isTriplanar) ? ADD_IDX(_TexWorldScale) : 1.0;
    float2 uvSpaceScale = invPrimScale * MERGE_NAME(ADD_IDX(_BaseColorMap), _ST.xy) * (worldScale * maxHeight);

    PerPixelHeightDisplacementParam ppdParam;
    ppdParam.heightmap = ADD_IDX(_HeightMap);
    ppdParam.heightmapSampler = SAMPLER_HEIGHTMAP_IDX;

    float height = 0; // final height processed
    float NdotV = 0;

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(V, uvXZ, uvXY, uvZY);

    // TODO: support object space planar/triplanar ?

    // We need to calculate the texture space direction. It depends on the mapping.
    if (isTriplanar)
    {
        float planeHeight;

        // Perform a POM in each direction and modify appropriate texture coordinate
        [branch] if (layerTexCoord.triplanarWeights.x >= 0.001)
        {
            ppdParam.uv = ADD_IDX(layerTexCoord.base).uvZY;
            float3 viewDirTS = float3(uvZY, abs(V.x));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, 1, ppdParam, planeHeight);

            // Apply offset to all triplanar UVSet
            ADD_IDX(layerTexCoord.base).uvZY += offset;
            ADD_IDX(layerTexCoord.details).uvZY += offset;
            height += layerTexCoord.triplanarWeights.x * planeHeight;
            NdotV += layerTexCoord.triplanarWeights.x * viewDirTS.z;
        }

        [branch] if (layerTexCoord.triplanarWeights.y >= 0.001)
        {
            ppdParam.uv = ADD_IDX(layerTexCoord.base).uvXZ;
            float3 viewDirTS = float3(uvXZ, abs(V.y));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, 1, ppdParam, planeHeight);

            ADD_IDX(layerTexCoord.base).uvXZ += offset;
            ADD_IDX(layerTexCoord.details).uvXZ += offset;
            height += layerTexCoord.triplanarWeights.y * planeHeight;
            NdotV += layerTexCoord.triplanarWeights.y * viewDirTS.z;
        }

        [branch] if (layerTexCoord.triplanarWeights.z >= 0.001)
        {
            ppdParam.uv = ADD_IDX(layerTexCoord.base).uvXY;
            float3 viewDirTS = float3(uvXY, abs(V.z));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, 1, ppdParam, planeHeight);

            ADD_IDX(layerTexCoord.base).uvXY += offset;
            ADD_IDX(layerTexCoord.details).uvXY += offset;
            height += layerTexCoord.triplanarWeights.z * planeHeight;
            NdotV += layerTexCoord.triplanarWeights.z * viewDirTS.z;
        }
    }
    else
    {
        ppdParam.uv = ADD_IDX(layerTexCoord.base).uv; // For planar it is uv too, not uvXZ

        // Note: The TBN is not normalize as it is based on mikkt. We should normalize it, but POM is always use on simple enough surface that mean it is not required (save 2 normalize). Tag: SURFACE_GRADIENT
        // Note: worldToTangent is only define for UVSet0, so we expect that layer that use POM have UVSet0
        float3 viewDirTS = isPlanar ? float3(uvXZ, V.y) : TransformWorldToTangent(V, input.worldToTangent) * GetDisplacementObjectScale(false).xzy; // Switch from Y-up to Z-up (as we move to tangent space)
        NdotV = viewDirTS.z;

        // Transform the view vector into the UV space.
        float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
        float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
        int    numSteps = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
        float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, 1, ppdParam, height);

        // Apply offset to all UVSet0 / planar
        ADD_IDX(layerTexCoord.base).uv += offset;
        ADD_IDX(layerTexCoord.details).uv += isPlanar ? offset : ADD_IDX(_UVDetailsMappingMask).x * offset; // Only apply offset if details map use UVSet0 _UVDetailsMappingMask.x will be 1 in this case, else 0
    }

    // Since POM "pushes" geometry inwards (rather than extrude it), { height = height - 1 }.
    // Since the result is used as a 'depthOffsetVS', it needs to be positive, so we flip the sign.
    float verticalDisplacement = maxHeight - height * maxHeight;
    return verticalDisplacement / NdotV;
#else
    return 0.0;
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

#if defined(_REFRACTION_THINPLANE) || defined(_REFRACTION_THICKPLANE) || defined(_REFRACTION_THICKSPHERE)
    surfaceData.ior = _IOR;
    surfaceData.transmittanceColor = _TransmittanceColor;
    surfaceData.atDistance = _ATDistance;
    // Thickness already defined with SSS (from both thickness and thicknessMap)
    surfaceData.thickness *= _ThicknessMultiplier;
#else
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1.0;
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

    // Transparency
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;

#endif // #if !defined(LAYERED_LIT_SHADER)

    return alpha;
}
