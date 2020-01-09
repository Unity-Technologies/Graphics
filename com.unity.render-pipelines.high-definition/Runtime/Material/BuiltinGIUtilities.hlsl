#ifndef __BUILTINGIUTILITIES_HLSL__
#define __BUILTINGIUTILITIES_HLSL__

// Return camera relative probe volume world to object transformation
float4x4 GetProbeVolumeWorldToObject()
{
    return ApplyCameraTranslationToInverseMatrix(unity_ProbeVolumeWorldToObject);
}

// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
//forest-begin: sky occlusion / Tree occlusion
float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, float skyOcclusion, float grassOcclusion, float treeOcclusion);

float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap) {
	return SampleBakedGI(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap, 1.f, 1.f, 1.f);
}

float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, float skyOcclusion, float grassOcclusion, float treeOcclusion)
//forest-end:
{
    // If there is no lightmap, it assume lightprobe
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)

    if (unity_ProbeVolumeParams.x == 0.0)
    {
        // TODO: pass a tab of coefficient instead!
        real4 SHCoefficients[7];
        SHCoefficients[0] = unity_SHAr;
        SHCoefficients[1] = unity_SHAg;
        SHCoefficients[2] = unity_SHAb;
        SHCoefficients[3] = unity_SHBr;
        SHCoefficients[4] = unity_SHBg;
        SHCoefficients[5] = unity_SHBb;
        SHCoefficients[6] = unity_SHC;

//forest-begin: sky occlusion
        #if SKY_OCCLUSION
			SHCoefficients[0] += _AmbientProbeSH[0] * skyOcclusion;
			SHCoefficients[1] += _AmbientProbeSH[1] * skyOcclusion;
			SHCoefficients[2] += _AmbientProbeSH[2] * skyOcclusion;
			SHCoefficients[3] += _AmbientProbeSH[3] * skyOcclusion;
			SHCoefficients[4] += _AmbientProbeSH[4] * skyOcclusion;
			SHCoefficients[5] += _AmbientProbeSH[5] * skyOcclusion;
			SHCoefficients[6] += _AmbientProbeSH[6] * skyOcclusion;
       #endif
//forest-end:

//forest-begin: Tree occlusion
        return SampleSH9(SHCoefficients, normalWS) * treeOcclusion;
//forest-end:
    }
    else
    {
#if RAYTRACING_ENABLED
        if (unity_ProbeVolumeParams.w == 1.0)
            return SampleProbeVolumeSH9(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, normalWS, GetProbeVolumeWorldToObject(),
//forest-begin: Tree occlusion
                unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz) * treeOcclusion;
//forest-end:
        else
#endif
            return SampleProbeVolumeSH4(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, normalWS, GetProbeVolumeWorldToObject(),
//forest-begin: Tree occlusion
                unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz) * treeOcclusion;
//forest-end:
    }

#else

    float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

#ifdef UNITY_LIGHTMAP_FULL_HDR
    bool useRGBMLightmap = false;
    float4 decodeInstructions = float4(0.0, 0.0, 0.0, 0.0); // Never used but needed for the interface since it supports gamma lightmaps
#else
    bool useRGBMLightmap = true;
#if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
    float4 decodeInstructions = float4(34.493242, 2.2, 0.0, 0.0); // range^2.2 = 5^2.2, gamma = 2.2
#else
    float4 decodeInstructions = float4(2.0, 2.2, 0.0, 0.0); // range = 2.0^2.2 = 4.59
#endif
#endif

#ifdef LIGHTMAP_ON
#ifdef DIRLIGHTMAP_COMBINED
    bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
        TEXTURE2D_ARGS(unity_LightmapInd, samplerunity_Lightmap),
        uvStaticLightmap, unity_LightmapST, normalWS, useRGBMLightmap, decodeInstructions);
#else
    bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), uvStaticLightmap, unity_LightmapST, useRGBMLightmap, decodeInstructions);
#endif
#endif

#ifdef DYNAMICLIGHTMAP_ON
#ifdef DIRLIGHTMAP_COMBINED
    bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
        TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
        uvDynamicLightmap, unity_DynamicLightmapST, normalWS, false, decodeInstructions);
#else
    bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST, false, decodeInstructions);
#endif
#endif

//forest-begin: sky occlusion
    return bakeDiffuseLighting * grassOcclusion;
//forest-end:
#endif
}

float4 SampleShadowMask(float3 positionRWS, float2 uvStaticLightmap) // normalWS not use for now
{
#if defined(LIGHTMAP_ON)
    float2 uv = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;
    float4 rawOcclusionMask = SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, uv); // Can't reuse sampler from Lightmap because with shader graph, the compile could optimize out the lightmaps if metal is 1
#else
    float4 rawOcclusionMask;
    if (unity_ProbeVolumeParams.x == 1.0)
    {
        rawOcclusionMask = SampleProbeOcclusion(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, GetProbeVolumeWorldToObject(),
            unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz);
    }
    else
    {
        // Note: Default value when the feature is not enabled is float(1.0, 1.0, 1.0, 1.0) in C++
        rawOcclusionMask = unity_ProbesOcclusion;
    }
#endif

    return rawOcclusionMask;
}

#endif
