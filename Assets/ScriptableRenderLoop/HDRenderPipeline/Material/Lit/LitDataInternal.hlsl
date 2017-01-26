void ADD_IDX(ComputeLayerTexCoord)( float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                                    float3 positionWS, float3 normalWS, bool isTriplanar, inout LayerTexCoord layerTexCoord, float additionalTiling = 1.0)
{
    // Handle uv0, uv1, uv2, uv3 based on _UVMappingMask weight (exclusif 0..1)
    float2 uvBase = ADD_IDX(_UVMappingMask).x * texCoord0 +
                    ADD_IDX(_UVMappingMask).y * texCoord1 + 
                    ADD_IDX(_UVMappingMask).z * texCoord2 +
                    ADD_IDX(_UVMappingMask).w * texCoord3;

    uvBase *= additionalTiling.xx;
                    

    float2 uvDetails =  ADD_IDX(_UVDetailsMappingMask).x * texCoord0 +
                        ADD_IDX(_UVDetailsMappingMask).y * texCoord1 +
                        ADD_IDX(_UVDetailsMappingMask).z * texCoord2 +
                        ADD_IDX(_UVDetailsMappingMask).w * texCoord3;

    // Note that if base is planar/triplanar, detail map is too

    // planar
    // TODO: Do we want to manage local or world triplanar/planar
    //float3 position = localTriplanar ? TransformWorldToObject(positionWS) : positionWS;
    float3 position = positionWS;
    position *= ADD_IDX(_TexWorldScale);

    if (ADD_IDX(_UVMappingPlanar) > 0.0)
    {
        uvBase = -position.xz;
        uvDetails = -position.xz;
    }

    ADD_IDX(layerTexCoord.base).uv = TRANSFORM_TEX(uvBase, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.details).uv = TRANSFORM_TEX(uvDetails, ADD_IDX(_DetailMap));

    // triplanar
    ADD_IDX(layerTexCoord.base).isTriplanar = isTriplanar;

    float3 direction = sign(normalWS);

    // In triplanar, if we are facing away from the world axis, a different axis will be flipped for each direction.
    // This is particularly problematic for tangent space normal maps which need to be in the right direction.
    // So we multiplying the offending coordinate by the sign of the normal.
    float2 uvYZ = float2(direction.x * position.z, position.y);
    float2 uvZX = -float2(position.x, direction.y * position.z);
    float2 uvXY = float2(-position.x, direction.z * position.y);

    ADD_IDX(layerTexCoord.base).uvYZ = TRANSFORM_TEX(uvYZ, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.base).uvZX = TRANSFORM_TEX(uvZX, ADD_IDX(_BaseColorMap));
    ADD_IDX(layerTexCoord.base).uvXY = TRANSFORM_TEX(uvXY, ADD_IDX(_BaseColorMap));

    ADD_IDX(layerTexCoord.details).isTriplanar = isTriplanar;

    ADD_IDX(layerTexCoord.details).uvYZ = TRANSFORM_TEX(uvYZ, ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvZX = TRANSFORM_TEX(uvZX, ADD_IDX(_DetailMap));
    ADD_IDX(layerTexCoord.details).uvXY = TRANSFORM_TEX(uvXY, ADD_IDX(_DetailMap));
}

float ADD_IDX(SampleHeightmap)(LayerTexCoord layerTexCoord, float centerOffset = 0.0, float multiplier = 1.0)
{
#ifdef _HEIGHTMAP
    return (SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), ADD_IDX(layerTexCoord.base)).r - ADD_IDX(_HeightCenter) - centerOffset) * ADD_IDX(_HeightAmplitude) * multiplier;
#else
    return 0.0;
#endif
}

float ADD_IDX(SampleHeightmapLod)(LayerTexCoord layerTexCoord, float lod, float centerOffset = 0.0, float multiplier = 1.0)
{
#ifdef _HEIGHTMAP
    return (SAMPLE_LAYER_TEXTURE2D_LOD(ADD_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), ADD_IDX(layerTexCoord.base), lod).r - ADD_IDX(_HeightCenter) - centerOffset) * ADD_IDX(_HeightAmplitude) * multiplier;
#else
    return 0.0;
#endif
}

// Note: The sampling of heightmap inside POM don't use sampling abstraction (with triplanar) as 
// POM must be apply separately for each uv set (so 3 time for triplanar)
void ADD_IDX(ParallaxOcclusionMappingLayer)(inout LayerTexCoord layerTexCoord, int numSteps, float3 viewDirTS)
{
    // Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
    float stepSize = 1.0 / (float)numSteps;

    // View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
    // The length of viewDirTS vector determines the furthest amount of displacement:
    // float parallaxLimit = -length(viewDirTS.xy) / viewDirTS.z;
    // float2 parallaxDir = normalize(Out.viewDirTS.xy);
    // float2 parallaxMaxOffsetTS = parallaxDir * parallaxLimit;
    // Above code simplify to
    float2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z) * ADD_IDX(_HeightAmplitude);
    float2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;

    float2 uv = ADD_IDX(layerTexCoord.base).uv;

    // Compute lod as we will sample inside a loop (so can't use regular sampling)
    // It appear that CALCULATE_TEXTURE2D_LOD only return interger lod. We want to use float lod to have smoother transition and fading
    // float lod = CALCULATE_TEXTURE2D_LOD(ADD_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), uv);
    float lod = ComputeTextureLOD(uv, GET_TEXELSIZE_NAME(ADD_IDX(_HeightMap))); 

    // Do a first step before the loop to init all value correctly
    float2 texOffsetCurrent = 0;
    float prevHeight = SAMPLE_TEXTURE2D_LOD(ADD_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), uv + texOffsetCurrent, lod).r;
    texOffsetCurrent += texOffsetPerStep;
    float currHeight = SAMPLE_TEXTURE2D_LOD(ADD_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), uv + texOffsetCurrent, lod).r;
    float rayHeight = 1.0 - stepSize; // Start at top less one sample

    // Linear search
    for (int stepIndex = 0; stepIndex < numSteps; ++stepIndex)
    {
        // Have we found a height below our ray height ? then we have an intersection
        if (currHeight > rayHeight)
            break; // end the loop

        prevHeight = currHeight;
        rayHeight -= stepSize;
        texOffsetCurrent += texOffsetPerStep;

        // Sample height map which in this case is stored in the alpha channel of the normal map:
        currHeight = SAMPLE_TEXTURE2D_LOD(ADD_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), uv + texOffsetCurrent, lod).r;
    }

    // Found below and above points, now perform line interesection (ray) with piecewise linear heightfield approximation

    // Refine the search by adding few extra intersection
#define POM_REFINE 1
#if POM_REFINE

    float pt0 = rayHeight + stepSize;
    float pt1 = rayHeight;
    float delta0 = pt0 - prevHeight;
    float delta1 = pt1 - currHeight;

    float2 offset = float2(0.0, 0.0);

    float threshold = 1.0;

    for (int i = 0; i < 5; ++i)
    {
        float t = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
        offset = (1 - t) * texOffsetPerStep * numSteps;

        currHeight = SAMPLE_TEXTURE2D_LOD(ADD_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), uv + offset, lod).r;

        threshold = t - currHeight;

        if (abs(threshold) <= 0.01)
            break;

        if (threshold < 0.0)
        {
            delta1 = threshold;
            pt1 = t;
        }
        else
        {
            delta0 = threshold;
            pt0 = t;
        }
    }

#else
    
    //float pt0 = rayHeight + stepSize;
    //float pt1 = rayHeight; 
    //float delta0 = pt0 - prevHeight;
    //float delta1 = pt1 - currHeight;
    //float t = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
    //float2 offset = (1 - t) * texOffsetPerStep * numSteps;

    // A bit more optimize
    float delta0 = currHeight - rayHeight;
    float delta1 = (rayHeight + stepSize) - prevHeight;
    float ratio = delta0 / (delta0 + delta1);
    float2 offset = texOffsetCurrent - ratio * texOffsetPerStep;

#endif

    // TODO: expose LOD fading
    //float lodThreshold = 0.0;
    //offset *= (1.0 - saturate(lod - lodThreshold));

    // Apply offset only on base. Details could use another mapping and will not be consistant...
    // Don't know if this will still ok.
    // TODO: check with artists
    ADD_IDX(layerTexCoord.base).uv += offset;
}

float3 ADD_IDX(GetNormalTS)(FragInputs input, LayerTexCoord layerTexCoord, float3 detailNormalTS, float detailMask, bool useBias, float bias)
{
    float3 normalTS;

    #ifdef _NORMALMAP
        #ifdef _NORMALMAP_TANGENT_SPACE
            if (useBias)
            {
                normalTS = SAMPLE_LAYER_NORMALMAP_BIAS(ADD_IDX(_NormalMap), ADD_ZERO_IDX(sampler_NormalMap), ADD_IDX(layerTexCoord.base), ADD_ZERO_IDX(_NormalScale), bias);
            }
            else
            {
                normalTS = SAMPLE_LAYER_NORMALMAP(ADD_IDX(_NormalMap), ADD_ZERO_IDX(sampler_NormalMap), ADD_IDX(layerTexCoord.base), ADD_ZERO_IDX(_NormalScale));
            }            
        #else // Object space
            // to be able to combine object space normal with detail map we transform it to tangent space (object space normal composition is not simple).
            // then later we will re-transform it to world space.
            if (useBias)
            {
                float3 normalOS = SAMPLE_LAYER_NORMALMAP_RGB_BIAS(ADD_IDX(_NormalMap), ADD_ZERO_IDX(sampler_NormalMap), ADD_IDX(layerTexCoord.base), ADD_ZERO_IDX(_NormalScale), bias).rgb;
                normalTS = TransformObjectToTangent(normalOS, input.tangentToWorld);
            }
            else
            {
                float3 normalOS = SAMPLE_LAYER_NORMALMAP_RGB(ADD_IDX(_NormalMap), ADD_ZERO_IDX(sampler_NormalMap), ADD_IDX(layerTexCoord.base), ADD_ZERO_IDX(_NormalScale)).rgb;
                normalTS = TransformObjectToTangent(normalOS, input.tangentToWorld);
            }
        #endif

        #ifdef _DETAIL_MAP
            normalTS = lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask);
        #endif
    #else
        normalTS = float3(0.0, 0.0, 1.0);
    #endif

    #if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
        #ifdef _DOUBLESIDED_LIGHTING_FLIP
            float3 oppositeNormalTS = -normalTS;
        #else
            // Mirror the normal with the plane define by vertex normal
            float3 oppositeNormalTS = reflect(normalTS, float3(0.0, 0.0, 1.0)); // Reflect around vertex normal (in tangent space this is z)
        #endif
        // TODO : Test if GetOddNegativeScale() is necessary here in case of normal map, as GetOddNegativeScale is take into account in CreateTangentToWorld();
        normalTS = input.isFrontFace ?
                        (GetOddNegativeScale() >= 0.0 ? normalTS : oppositeNormalTS) :
                        (-GetOddNegativeScale() >= 0.0 ? normalTS : oppositeNormalTS);
    #endif

    return normalTS;
}

// Return opacity
float ADD_IDX(GetSurfaceData)(FragInputs input, LayerTexCoord layerTexCoord, out SurfaceData surfaceData, out float3 normalTS)
{
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    float alpha = ADD_IDX(_BaseColor).a;
#else
    float alpha = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).a * ADD_IDX(_BaseColor).a;
#endif

    // Perform alha test very early to save performance (a killed pixel will not sample textures)
#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

    float3 detailNormalTS = float3(0.0, 0.0, 0.0);
    float detailMask = 0.0;
#ifdef _DETAIL_MAP
    detailMask = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_DetailMask), ADD_ZERO_IDX(sampler_DetailMask), ADD_IDX(layerTexCoord.base)).g;
    float2 detailAlbedoAndSmoothness = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_DetailMap), ADD_ZERO_IDX(sampler_DetailMap), ADD_IDX(layerTexCoord.details)).rb;
    float detailAlbedo = detailAlbedoAndSmoothness.r;
    float detailSmoothness = detailAlbedoAndSmoothness.g;
    #ifdef _DETAIL_MAP_WITH_NORMAL
    // Resample the detail map but this time for the normal map. This call should be optimize by the compiler
    // We split both call due to trilinear mapping
    detailNormalTS = SAMPLE_LAYER_NORMALMAP_AG(ADD_IDX(_DetailMap), ADD_ZERO_IDX(sampler_DetailMap), ADD_IDX(layerTexCoord.details), ADD_ZERO_IDX(_DetailNormalScale));
    //float detailAO = 0.0;
    #else
    // TODO: Use heightmap as a derivative with Morten Mikklesen approach, how this work with our abstraction and triplanar ?
    detailNormalTS = float3(0.0, 0.0, 1.0);
    //float detailAO = detail.b;
    #endif
#endif

    surfaceData.baseColor = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).rgb * ADD_IDX(_BaseColor).rgb;
#ifdef _DETAIL_MAP
    surfaceData.baseColor *= LerpWhiteTo(2.0 * saturate(detailAlbedo * ADD_IDX(_DetailAlbedoScale)), detailMask);
#endif

#ifdef _SPECULAROCCLUSIONMAP
    // TODO: Do something. For now just take alpha channel
    surfaceData.specularOcclusion = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_SpecularOcclusionMap), ADD_ZERO_IDX(sampler_SpecularOcclusionMap), ADD_IDX(layerTexCoord.base)).a;
#else
    // Horizon Occlusion for Normal Mapped Reflections: http://marmosetco.tumblr.com/post/81245981087
    //surfaceData.specularOcclusion = saturate(1.0 + horizonFade * dot(r, input.tangentToWorld[2].xyz);
    // smooth it
    //surfaceData.specularOcclusion *= surfaceData.specularOcclusion;
    surfaceData.specularOcclusion = 1.0;
#endif
    surfaceData.normalWS = float3(0.0, 0.0, 0.0); // Need to init this so that the compiler leaves us alone.

    // TODO: think about using BC5
    normalTS = ADD_IDX(GetNormalTS)(input, layerTexCoord, detailNormalTS, detailMask, false, 0.0);

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    surfaceData.perceptualSmoothness = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_BaseColorMap), ADD_ZERO_IDX(sampler_BaseColorMap), ADD_IDX(layerTexCoord.base)).a;
#elif defined(_MASKMAP)
    surfaceData.perceptualSmoothness = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_MaskMap), ADD_ZERO_IDX(sampler_MaskMap), ADD_IDX(layerTexCoord.base)).a;
#else
    surfaceData.perceptualSmoothness = 1.0;
#endif
    surfaceData.perceptualSmoothness *= ADD_IDX(_Smoothness);
#ifdef _DETAIL_MAP
    surfaceData.perceptualSmoothness *= LerpWhiteTo(2.0 * saturate(detailSmoothness * ADD_IDX(_DetailSmoothnessScale)), detailMask);
#endif

    // MaskMap is Metallic, Ambient Occlusion, (Optional) - emissive Mask, Optional - Smoothness (in alpha)
#ifdef _MASKMAP
    surfaceData.metallic = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_MaskMap), ADD_ZERO_IDX(sampler_MaskMap), ADD_IDX(layerTexCoord.base)).r;
    surfaceData.ambientOcclusion = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_MaskMap), ADD_ZERO_IDX(sampler_MaskMap), ADD_IDX(layerTexCoord.base)).g;
#else
    surfaceData.metallic = 1.0;
    surfaceData.ambientOcclusion = 1.0;
#endif
    surfaceData.metallic *= ADD_IDX(_Metallic);

    // This part of the code is not used in case of layered shader but we keep the same macro system for simplicity
#if !defined(LAYERED_LIT_SHADER)

    surfaceData.materialId = 0; // TODO

    // TODO: think about using BC5
#ifdef _TANGENTMAP
#ifdef _NORMALMAP_TANGENT_SPACE // Normal and tangent use same space
    float3 tangentTS = SAMPLE_LAYER_NORMALMAP(ADD_IDX(_TangentMap), ADD_ZERO_IDX(sampler_TangentMap), ADD_IDX(layerTexCoord.base), 1.0);
    surfaceData.tangentWS = TransformTangentToWorld(tangentTS, input.tangentToWorld);
#else // Object space
    float3 tangentOS = SAMPLE_LAYER_NORMALMAP_RGB(ADD_IDX(_TangentMap), ADD_ZERO_IDX(sampler_TangentMap), ADD_IDX(layerTexCoord.base), 1.0).rgb;
    surfaceData.tangentWS = TransformObjectToWorldDir(tangentOS);
#endif
#else
    surfaceData.tangentWS = normalize(input.tangentToWorld[0].xyz);
#endif
    // TODO: Is there anything todo regarding flip normal but for the tangent ?

#ifdef _ANISOTROPYMAP
    surfaceData.anisotropy = SAMPLE_LAYER_TEXTURE2D(ADD_IDX(_AnisotropyMap), ADD_ZERO_IDX(sampler_AnisotropyMap), ADD_IDX(layerTexCoord.base)).b;
#else
    surfaceData.anisotropy = 1.0;
#endif
    surfaceData.anisotropy *= ADD_IDX(_Anisotropy);

    surfaceData.specular = 0.04;

    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;

    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

#else // #if !defined(LAYERED_LIT_SHADER)

    // Mandatory to setup value to keep compiler quiet

    // Layered shader only support materialId 0
    surfaceData.materialId = 0;

    surfaceData.tangentWS = input.tangentToWorld[0].xyz;
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;

    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;

    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

#endif // #if !defined(LAYERED_LIT_SHADER)

    return alpha;
}

