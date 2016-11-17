
//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
float3 SampleBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    // If there is no lightmap, it assume lightprobe
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)

// TODO: Confirm with Ionut but it seems that UNITY_LIGHT_PROBE_PROXY_VOLUME is always define for high end and 
// unity_ProbeVolumeParams always bind.
    if (unity_ProbeVolumeParams.x == 0.0)
    {
        // TODO: pass a tab of coefficient instead!
        float4 SHCoefficients[7];
        SHCoefficients[0] = unity_SHAr;
        SHCoefficients[1] = unity_SHAg;
        SHCoefficients[2] = unity_SHAb;
        SHCoefficients[3] = unity_SHBr;
        SHCoefficients[4] = unity_SHBg;
        SHCoefficients[5] = unity_SHBb;
        SHCoefficients[6] = unity_SHC;

        return SampleSH9(SHCoefficients, normalWS);
    }
    else
    {
        // TODO: Move all this to C++!
        float4x4 identity = 0;
        identity._m00_m11_m22_m33 = 1.0;
        float4x4 WorldToTexture = (unity_ProbeVolumeParams.y == 1.0f) ? unity_ProbeVolumeWorldToObject : identity;

        float4x4 translation = identity;
        translation._m30_m31_m32 = -unity_ProbeVolumeMin.xyz;

        float4x4 scale = 0;
        scale._m00_m11_m22_m33 = float4(unity_ProbeVolumeSizeInv.xyz, 1.0);

        WorldToTexture = mul(mul(scale, translation), WorldToTexture);
    
        return SampleProbeVolumeSH4(TEXTURE3D_PARAM(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionWS, normalWS, WorldToTexture, unity_ProbeVolumeParams.z);
    }

#else

    float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    #ifdef LIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap),
                                                        TEXTURE2D_PARAM(unity_LightmapInd, samplerunity_Lightmap),
                                                        uvStaticLightmap, unity_LightmapST, normalWS);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap), uvStaticLightmap, unity_LightmapST);
        #endif
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap),
                                                        TEXTURE2D_PARAM(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
                                                        uvDynamicLightmap, unity_DynamicLightmapST, normalWS);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST);
        #endif
    #endif

    return bakeDiffuseLighting;

#endif
}

float2 CalculateVelocity(float4 positionCS, float4 previousPositionCS)
{
    // This test on define is required to remove warning of divide by 0 when initializing empty struct
    // TODO: Add forward opaque MRT case...
#if (SHADERPASS == SHADERPASS_VELOCITY) || (SHADERPASS == SHADERPASS_GBUFFER && SHADEROPTIONS_VELOCITY_IN_GBUFFER)
    // Encode velocity
    positionCS.xy = positionCS.xy / positionCS.w;
    previousPositionCS.xy = previousPositionCS.xy / previousPositionCS.w;

    return (positionCS.xy - previousPositionCS.xy) * _ForceNoMotion;
#else
    return float2(0.0, 0.0);
#endif
}
                                           
#if !defined(LAYERED_LIT_SHADER)

void GetSurfaceAndBuiltinData(FragInput input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef _HEIGHTMAP
    // TODO: in case of shader graph, a node like parallax must be nullify if use to generate code for Meta pass
    #ifndef _HEIGHTMAP_AS_DISPLACEMENT
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS); // This should be remove by the compiler as we usually cal it before.
    float height = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, input.texCoord0).r * _HeightScale + _HeightBias;
    // Transform view vector in tangent space
    float3 viewDirTS = TransformWorldToTangent(V, input.tangentToWorld);
    float2 offset = ParallaxOffset(viewDirTS, height);
    input.texCoord0 += offset;
    input.texCoord1 += offset;
    #endif
#endif

    surfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.texCoord0).rgb * _BaseColor.rgb;
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    float alpha = _BaseColor.a;
#else
    float alpha = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.texCoord0).a * _BaseColor.a;
#endif

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

#ifdef _SPECULAROCCLUSIONMAP
    // TODO: Do something. For now just take alpha channel
    surfaceData.specularOcclusion = SAMPLE_TEXTURE2D(_SpecularOcclusionMap, sampler_SpecularOcclusionMap, input.texCoord0).a;
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
    float3 normalTS = UnpackNormalAG(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.texCoord0));
    surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);
    #else // Object space (TODO: We need to apply the world rotation here! - Require to pass world transform)
    surfaceData.normalWS = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.texCoord0).rgb;
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
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.texCoord0).a;
#elif defined(_MASKMAP)
    surfaceData.perceptualSmoothness = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).a;
#else
    surfaceData.perceptualSmoothness = 1.0;
#endif
    surfaceData.perceptualSmoothness *= _Smoothness;

    surfaceData.materialId = 0;

    // MaskMap is Metallic, Ambient Occlusion, (Optional) - emissive Mask, Optional - Smoothness (in alpha)
#ifdef _MASKMAP
    surfaceData.metallic = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).r;
    surfaceData.ambientOcclusion = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).g;
#else
    surfaceData.metallic = 1.0;
    surfaceData.ambientOcclusion = 1.0;
#endif
    surfaceData.metallic *= _Metallic;

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
    surfaceData.anisotropy = SAMPLE_TEXTURE2D(_AnisotropyMap, sampler_AnisotropyMap, input.texCoord0).r;
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


    // Builtin Data
    builtinData.opacity = alpha;

    // TODO: Sample lightmap/lightprobe/volume proxy
    // This should also handle projective lightmap
    // Note that data input above can be use to sample into lightmap (like normal)
    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionWS, surfaceData.normalWS, input.texCoord1, input.texCoord2);

    // If we chose an emissive color, we have a dedicated texture for it and don't use MaskMap
#ifdef _EMISSIVE_COLOR
    #ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor;
    #else
    builtinData.emissiveColor = _EmissiveColor;
    #endif
#elif defined(_MASKMAP) // If we have a MaskMap, use emissive slot as a mask on baseColor
    builtinData.emissiveColor = surfaceData.baseColor * SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).bbb;
#else
    builtinData.emissiveColor = float3(0.0, 0.0, 0.0);
#endif

    builtinData.emissiveIntensity = _EmissiveIntensity;

    builtinData.velocity = CalculateVelocity(input.positionCS, input.previousPositionCS);

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
}

#else


float3 BlendLayeredColor(float3 rgb0, float3 rgb1, float3 rgb2, float3 rgb3, float weight[4])
{
    float3 result = float3(0.0, 0.0, 0.0);

    result = rgb0 * weight[0] + rgb1 * weight[1];
#if _LAYER_COUNT >= 3
    result += (rgb2 * weight[2]);
#endif
#if _LAYER_COUNT >= 4
    result += rgb3 * weight[3];
#endif

    return result;
}

float3 BlendLayeredNormal(float3 normal0, float3 normal1, float3 normal2, float3 normal3, float weight[4])
{
    float3 result = float3(0.0, 0.0, 0.0);

        // TODO : real normal map blending function
        result = normal0 * weight[0] + normal1 * weight[1];
#if _LAYER_COUNT >= 3
    result += normal2 * weight[2];
#endif
#if _LAYER_COUNT >= 4
    result += normal3 * weight[3];
#endif

    return result;
}

float BlendLayeredScalar(float x0, float x1, float x2, float x3, float weight[4])
{
    float result = 0.0;

    result = x0 * weight[0] + x1 * weight[1];
#if _LAYER_COUNT >= 3
    result += x2 * weight[2];
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

void ComputeMaskWeights(float3 inputMasks, out float outWeights[_MAX_LAYER])
{
    float masks[_MAX_LAYER];
    masks[0] = 1.0f; // Layer 0 is always full
    masks[1] = inputMasks.r;
    masks[2] = inputMasks.g;
    masks[3] = inputMasks.b;

    // calculate weight of each layers
    float left = 1.0f;

    [unroll]
    for (int i = _LAYER_COUNT - 1; i > 0; --i)
    {
        outWeights[i] = masks[i] * left;
        left -= outWeights[i];
    }
    outWeights[0] = left;
}

float2 ComputePlanarXZCoord(float3 worldPos, float layerSize)
{
    return frac(worldPos.xz / layerSize);
}

void ComputeLayerCoordinates(out LayerCoordinates outCoord, FragInput input)
{
#if defined(_LAYER_MAPPING_UV1_0)
    outCoord.texcoord[0] = input.texCoord1;
    outCoord.isTriplanar[0] = false;
#elif defined(_LAYER_MAPPING_PLANAR_0)
    outCoord.texcoord[0] = ComputePlanarXZCoord(input.positionWS, _LayerSize0);
    outCoord.isTriplanar[0] = false;
#elif defined(_LAYER_MAPPING_TRIPLANAR_0)
    outCoord.texcoord[0] = input.texCoord0;
    outCoord.isTriplanar[0] = true;
#else
    outCoord.texcoord[0] = input.texCoord0;
    outCoord.isTriplanar[0] = false;
#endif

#if defined(_LAYER_MAPPING_UV1_1)
    outCoord.texcoord[1] = input.texCoord1;
    outCoord.isTriplanar[1] = false;
#elif defined(_LAYER_MAPPING_PLANAR_1)
    outCoord.texcoord[1] = ComputePlanarXZCoord(input.positionWS, _LayerSize1);
    outCoord.isTriplanar[1] = false;
#elif defined(_LAYER_MAPPING_TRIPLANAR_1)
    outCoord.texcoord[1] = input.texCoord0;
    outCoord.isTriplanar[1] = true;
#else
    outCoord.texcoord[1] = input.texCoord0;
    outCoord.isTriplanar[1] = false;
#endif

#if defined(_LAYER_MAPPING_UV1_2)
    outCoord.texcoord[2] = input.texCoord1;
    outCoord.isTriplanar[2] = false;
#elif defined(_LAYER_MAPPING_PLANAR_2)
    outCoord.texcoord[2] = ComputePlanarXZCoord(input.positionWS, _LayerSize2);
    outCoord.isTriplanar[2] = false;
#elif defined(_LAYER_MAPPING_TRIPLANAR_2)
    outCoord.texcoord[2] = input.texCoord0;
    outCoord.isTriplanar[2] = true;
#else
    outCoord.texcoord[2] = input.texCoord0;
    outCoord.isTriplanar[2] = false;
#endif

#if defined(_LAYER_MAPPING_UV1_3)
    outCoord.texcoord[3] = input.texCoord1;
    outCoord.isTriplanar[3] = false;
#elif defined(_LAYER_MAPPING_PLANAR_3)
    outCoord.texcoord[3] = ComputePlanarXZCoord(input.positionWS, _LayerSize3);
    outCoord.isTriplanar[3] = false;
#elif defined(_LAYER_MAPPING_TRIPLANAR_3)
    outCoord.texcoord[3] = input.texCoord0;
    outCoord.isTriplanar[3] = true;
#else
    outCoord.texcoord[3] = input.texCoord0;
    outCoord.isTriplanar[3] = false;
#endif
}

void GetSurfaceAndBuiltinData(FragInput input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    LayerCoordinates layerCoord;
    ComputeLayerCoordinates(layerCoord, input);

    // Mask Values : Layer 1, 2, 3 are r, g, b
    float3 maskValues = float3(0.0, 0.0, 0.0);

#if defined(_LAYER_MASK_MAP)
    maskValues = SAMPLE_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, input.texCoord0).rgb;
#endif

#if defined(_LAYER_MASK_VERTEX_COLOR)
    maskValues = input.vertexColor.rgb;
#endif

#if defined(_LAYER_MASK_MAP) && defined(_LAYER_MASK_VERTEX_COLOR)
    maskValues = input.vertexColor.rgb * SAMPLE_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, input.texCoord0).rgb;
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(maskValues, weights);

    PROP_DECL(float3, baseColor);
    PROP_SAMPLE(baseColor, _BaseColorMap, layerCoord, rgb);
    PROP_MUL(baseColor, _BaseColor, rgb);
    PROP_BLEND_COLOR(baseColor, weights);

    surfaceData.baseColor = baseColor;

    PROP_DECL(float, alpha);
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    PROP_ASSIGN(alpha, _BaseColor, a);
#else
    PROP_SAMPLE(alpha, _BaseColorMap, layerCoord, a);
    PROP_MUL(alpha, _BaseColor, a);
#endif
    PROP_BLEND_SCALAR(alpha, weights);

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

    builtinData.opacity = alpha;

    PROP_DECL(float, specularOcclusion);
#ifdef _SPECULAROCCLUSIONMAP
    // TODO: Do something. For now just take alpha channel
    PROP_SAMPLE(specularOcclusion, _SpecularOcclusionMap, layerCoord, a);
#else
    // Horizon Occlusion for Normal Mapped Reflections: http://marmosetco.tumblr.com/post/81245981087
    //surfaceData.specularOcclusion = saturate(1.0 + horizonFade * dot(r, input.tangentToWorld[2].xyz);
    // smooth it
    //surfaceData.specularOcclusion *= surfaceData.specularOcclusion;
    PROP_ASSIGN_VALUE(specularOcclusion, 1.0);
#endif
    PROP_BLEND_SCALAR(specularOcclusion, weights);
    surfaceData.specularOcclusion = specularOcclusion;

    // TODO: think about using BC5
    float3 vertexNormalWS = input.tangentToWorld[2].xyz;

#ifdef _NORMALMAP
    #ifdef _NORMALMAP_TANGENT_SPACE
        float3 normalTS0 = UnpackNormalAG(SampleLayer(TEXTURE2D_PARAM(_NormalMap0, sampler_NormalMap0), layerCoord, 0));
        float3 normalTS1 = UnpackNormalAG(SampleLayer(TEXTURE2D_PARAM(_NormalMap1, sampler_NormalMap0), layerCoord, 1));
        float3 normalTS2 = UnpackNormalAG(SampleLayer(TEXTURE2D_PARAM(_NormalMap2, sampler_NormalMap0), layerCoord, 2));
        float3 normalTS3 = UnpackNormalAG(SampleLayer(TEXTURE2D_PARAM(_NormalMap3, sampler_NormalMap0), layerCoord, 3));

        float3 normalTS = BlendLayeredNormal(normalTS0, normalTS1, normalTS2, normalTS3, weights);

        surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);
    #else // Object space (TODO: We need to apply the world rotation here!)
        surfaceData.normalWS = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.texCoord0).rgb;
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
        surfaceData.normalWS = IS_FRONT_VFACE(input.cullFace, GetOdddNegativeScale() >= 0.0 ? surfaceData.normalWS : oppositeNormalWS, -GetOdddNegativeScale() >= 0.0 ? surfaceData.normalWS : oppositeNormalWS);
#endif


    PROP_DECL(float, perceptualSmoothness);
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    PROP_SAMPLE(perceptualSmoothness, _BaseColorMap, layerCoord, a);
#elif defined(_MASKMAP)
    PROP_SAMPLE(perceptualSmoothness, _MaskMap, layerCoord, a);
#else
    PROP_ASSIGN_VALUE(perceptualSmoothness, 1.0);
#endif
    PROP_MUL(perceptualSmoothness, _Smoothness, r);
    PROP_BLEND_SCALAR(perceptualSmoothness, weights);

    surfaceData.perceptualSmoothness = perceptualSmoothness;

    surfaceData.materialId = 0;

    // MaskMap is Metallic, Ambient Occlusion, (Optional) - emissive Mask, Optional - Smoothness (in alpha)
    PROP_DECL(float, metallic);
    PROP_DECL(float, ambientOcclusion);
#ifdef _MASKMAP
    PROP_SAMPLE(metallic, _MaskMap, layerCoord, a);
    PROP_SAMPLE(ambientOcclusion, _MaskMap, layerCoord, g);
#else
    PROP_ASSIGN_VALUE(metallic, 1.0);
    PROP_ASSIGN_VALUE(ambientOcclusion, 1.0);
#endif
    PROP_MUL(metallic, _Metallic, r);

    PROP_BLEND_SCALAR(metallic, weights);
    PROP_BLEND_SCALAR(ambientOcclusion, weights);

    surfaceData.metallic = metallic;
    surfaceData.ambientOcclusion = ambientOcclusion;

    surfaceData.tangentWS = float3(1.0, 0.0, 0.0);
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;

    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;

    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

    // Builtin Data

    // TODO: Sample lightmap/lightprobe/volume proxy
    // This should also handle projective lightmap
    // Note that data input above can be use to sample into lightmap (like normal)
    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionWS, surfaceData.normalWS, input.texCoord1, input.texCoord2);

    // If we chose an emissive color, we have a dedicated texture for it and don't use MaskMap
    PROP_DECL(float3, emissiveColor);
#ifdef _EMISSIVE_COLOR
#ifdef _EMISSIVE_COLOR_MAP
    PROP_SAMPLE(emissiveColor, _EmissiveColorMap, layerCoord, rgb);
#else
    PROP_ASSIGN(emissiveColor, _EmissiveColor, rgb);
#endif
#elif defined(_MASKMAP) // If we have a MaskMap, use emissive slot as a mask on baseColor
    PROP_SAMPLE(emissiveColor, _MaskMap, layerCoord, bbb);
    PROP_MUL(emissiveColor, baseColor, rgb);
#else
    PROP_ASSIGN_VALUE(emissiveColor, float3(0.0, 0.0, 0.0));
#endif
    PROP_BLEND_COLOR(emissiveColor, weights);
    builtinData.emissiveColor = emissiveColor;

    PROP_DECL(float, emissiveIntensity);
    PROP_ASSIGN(emissiveIntensity, _EmissiveIntensity, r);
    PROP_BLEND_SCALAR(emissiveIntensity, weights);
    builtinData.emissiveIntensity = emissiveIntensity;

    builtinData.velocity = CalculateVelocity(input.positionCS, input.previousPositionCS);

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
}

#endif
