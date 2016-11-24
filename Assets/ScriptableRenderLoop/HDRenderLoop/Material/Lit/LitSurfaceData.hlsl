void ADD_IDX(ComputeLayerTexCoord)(FragInput input, int index, bool isTriplanar, out LayerTexCoord outLayerTexCoord)
{
    // Handle uv0, uv1 and plnar XZ coordinate based on _CoordWeight weight (exclusif 0..1)
    ADD_IDX(layerTexCoord.base).uv =    ADD_IDX(_UVMappingMask).x * input.texCoord0 +
                                        ADD_IDX(_UVMappingMask).y * input.texCoord1 + 
                                        ADD_IDX(_UVMappingMask).z * input.positionWS.xz * ADD_IDX(_TexWorldScale);
    float2 uvDetails =  ADD_IDX(_UVDetailsMappingMask).x * input.texCoord0 +
                        ADD_IDX(_UVDetailsMappingMask).y * input.texCoord1;
    ADD_IDX(layerTexCoord.details).uv = TRANSFORM_TEX(uvDetails, ADD_IDX(_DetailMap));

    // triplanar
    ADD_IDX(layerTexCoord.base).isTriplanar = isTriplanar;

    // TODO: local or world triplanar
    //float3 position = localTriplanar ? TransformWorldToObject(input.positionWS) : input.positionWS;
    float3 position = input.positionWS;
    position *= ADD_IDX(_TexWorldScale);

    ADD_IDX(layerTexCoord.base).uvYZ = position.yz;
    ADD_IDX(layerTexCoord.base).uvZX = position.xy;
    ADD_IDX(layerTexCoord.base).uvXY = position.xz;

    ADD_IDX(layerTexCoord.details).uvYZ = TRANSFORM_TEX(position.yz, ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvZX = TRANSFORM_TEX(position.xy, ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvXY = TRANSFORM_TEX(position.xz, ADD_IDX(_DetailMap));

    layerTexCoord.uvStaticLightmap = input.texCoord1;
    layerTexCoord.uvDynamicLightmap = input.texCoord2;
}

void ADD_IDX(ApplyDisplacement)(FragInput input, inout LayerTexCoord layerTexCoord)
{
#ifdef _HEIGHTMAP
    // TODO: in case of shader graph, a node like parallax must be nullify if use to generate code for Meta pass
    #ifndef _HEIGHTMAP_AS_DISPLACEMENT
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS); // This should be remove by the compiler as we usually cal it before.
    float height = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_HeightMap), ADD_IDX(sampler_HeightMap), ADD_IDX(layerTexCoord.base)).r * ADD_IDX(_HeightScale) + ADD_IDX(_HeightBias);
    // Transform view vector in tangent space
    float3 viewDirTS = TransformWorldToTangent(V, input.tangentToWorld);
    float2 offset = ParallaxOffset(viewDirTS, height);

    ADD_IDX(layerTexCoord.base).uv += offset;
    ADD_IDX(layerTexCoord.base).uvYZ += offset;
    ADD_IDX(layerTexCoord.base).uvZX += offset;
    ADD_IDX(layerTexCoord.base).uvXY += offset;

    ADD_IDX(layerTexCoord.details).uv += offset;
    ADD_IDX(layerTexCoord.details).uvYZ += offset;
    ADD_IDX(layerTexCoord.details).uvZX += offset;
    ADD_IDX(layerTexCoord.details).uvXY += offset;

    if (LAYER_INDEX == 0)
    {
        layerTexCoord.uvStaticLightmap += offset;
        layerTexCoord.uvDynamicLightmap += offset;
    }
    #endif
#endif
}

// Return opacity
float ADD_IDX(GetSurfaceData)(FragInput input, int index, inout LayerTexCoord layerTexCoord, out SurfaceData surfaceData)
{
#ifdef _DETAIL_MAP
    float detailMask = SAMPLE_LAYER_TEXTURE2D(_DetailMask, sampler_DetailMask, layerTexCoord).b;
    float4 detail = SAMPLE_LAYER_DETAIL_TEXTURE2D(_DetailMap, sampler_DetailMap, layerTexCoord);
    float detailAlbedo = detail.r;
    float detailSmoothness = detail.b;
    #ifdef _DETAIL_MAP_WITH_NORMAL
    float3 detailNormalTS = UnpackNormalAG(detail, _DetailNormalScale);
    //float detailAO = 0.0;
    #else
    // TODO: Use heightmap as a derivative with Morten Mikklesen approach
    // Or reconstruct
    float U = SAMPLE_LAYER_TEXTURE2D(_DetailMap, sampler_DetailMap, texCoordDetail + float2(0.005, 0)).a;
    float V = SAMPLE_LAYER_TEXTURE2D(_DetailMap, sampler_DetailMap, texCoordDetail + float2(0, 0.005)).a;
    float dHdU = U - detail.a;	//create bump map U offset
    float dHdV = V - detail.a;	//create bump map V offset
    //float3 detailNormal = 1 - float3(dHdU, dHdV, 0.05);	//create the tangent space normal
    float3 detailNormalTS = float3(0.0, 0.0, 1.0);
    //float3 detailNormal = UnpackNormalAG(unifiedDetail.r).a;
    //float detailAO = detail.b;
    #endif
#endif

    surfaceData.baseColor = SAMPLE_LAYER_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.texCoord0).rgb * _BaseColor.rgb;
#ifdef _DETAIL_MAP
    surfaceData.baseColor *= LerpWhiteTo(2.0 * saturate(detailAlbedo * _DetailAlbedoScale), detailMask);
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    float alpha = _BaseColor.a;
#else
    float alpha = SAMPLE_LAYER_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.texCoord0).a * _BaseColor.a;
#endif

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

#ifdef _SPECULAROCCLUSIONMAP
    // TODO: Do something. For now just take alpha channel
    surfaceData.specularOcclusion = SAMPLE_LAYER_TEXTURE2D(_SpecularOcclusionMap, sampler_SpecularOcclusionMap, input.texCoord0).a;
#else
    // Horizon Occlusion for Normal Mapped Reflections: http://marmosetco.tumblr.com/post/81245981087
    //surfaceData.specularOcclusion = saturate(1.0 + horizonFade * dot(r, input.tangentToWorld[2].xyz);
    // smooth it
    //surfaceData.specularOcclusion *= surfaceData.specularOcclusion;
    surfaceData.specularOcclusion = 1.0;
#endif

    // TODO: think about using BC5
    float3 vertexNormalWS = input.tangentToWorld[2].xyz;

#ifdef _NORMALMAP
    #ifdef _NORMALMAP_TANGENT_SPACE
        float3 normalTS = UnpackNormalAG(SAMPLE_LAYER_TEXTURE2D(_NormalMap, sampler_NormalMap, input.texCoord0));
        #ifdef _DETAIL_MAP
        normalTS = lerp(normalTS, blendNormal(normalTS, detailNormalTS), detailMask);
        #endif
        surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);
    #else // Object space
        float3 normalOS = SAMPLE_LAYER_TEXTURE2D(_NormalMap, sampler_NormalMap, input.texCoord0).rgb;
        surfaceData.normalWS = TransformObjectToWorldDir(normalOS);
        #ifdef _DETAIL_MAP
        float3 detailNormalWS = TransformTangentToWorld(detailNormalTS, input.tangentToWorld);
        surfaceData.normalWS = lerp(surfaceData.normalWS, blendNormal(surfaceData.normalWS, detailNormalWS), detailMask);
        #endif
    #endif
#else
    surfaceData.normalWS = vertexNormalWS;
#endif

#if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    #ifdef _DOUBLESIDED_LIGHTING_FLIP
    float3 oppositeNormalWS = -surfaceData.normalWS;
    #else
    // Mirror the normal with the plane define by vertex normal
    float3 oppositeNormalWS = reflect(surfaceData.normalWS, vertexNormalWS);
#endif
    // TODO : Test if GetOdddNegativeScale() is necessary here in case of normal map, as GetOdddNegativeScale is take into account in CreateTangentToWorld();
    surfaceData.normalWS =  input.isFrontFace ? 
                                (GetOdddNegativeScale() >= 0.0 ? surfaceData.normalWS : oppositeNormalWS) :
                                (-GetOdddNegativeScale() >= 0.0 ? surfaceData.normalWS : oppositeNormalWS);
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    surfaceData.perceptualSmoothness = SAMPLE_LAYER_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.texCoord0).a;
#elif defined(_MASKMAP)
    surfaceData.perceptualSmoothness = SAMPLE_LAYER_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).a;
#else
    surfaceData.perceptualSmoothness = 1.0;
#endif
    surfaceData.perceptualSmoothness *= _Smoothness;
#ifdef _DETAIL_MAP
    surfaceData.perceptualSmoothness *= LerpWhiteTo(2.0 * saturate(detailSmoothness * _DetailSmoothnessScale), detailMask);
#endif

    surfaceData.materialId = 0;

    // MaskMap is Metallic, Ambient Occlusion, (Optional) - emissive Mask, Optional - Smoothness (in alpha)
#ifdef _MASKMAP
    surfaceData.metallic = SAMPLE_LAYER_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).r;
    surfaceData.ambientOcclusion = SAMPLE_LAYER_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).g;
#else
    surfaceData.metallic = 1.0;
    surfaceData.ambientOcclusion = 1.0;
#endif
    surfaceData.metallic *= _Metallic;

#if !defined(LAYERED_LIT_SHADER)

    // TODO: think about using BC5
#ifdef _TANGENTMAP
#ifdef _NORMALMAP_TANGENT_SPACE // Normal and tangent use same space
    float3 tangentTS = UnpackNormalAG(SAMPLE_TEXTURE2D(_TangentMap, sampler_TangentMap, input.texCoord0));
    surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.tangentToWorld);
#else // Object space (TODO: We need to apply the world rotation here! - Require to pass world transform)
    surfaceData.tangentWS = SAMPLE_TEXTURE2D(_TangentMap, sampler_TangentMap, input.texCoord0).rgb;
#endif
#else
    surfaceData.tangentWS = input.tangentToWorld[0].xyz;
#endif
    // TODO: Is there anything todo regarding flip normal but for the tangent ?

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_TEXTURE2D(_AnisotropyMap, sampler_AnisotropyMap, input.texCoord0).g;
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= _Anisotropy;

    surfaceData.specular = 0.04;

    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;

    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

#else // #if !defined(LAYERED_LIT_SHADER)
    surfaceData.tangentWS = input.tangentToWorld[0].xyz;
#endif // #if !defined(LAYERED_LIT_SHADER)

    return alpha;
}

