// SH lighting environment
float4 unity_SHAr_RT;
float4 unity_SHAg_RT;
float4 unity_SHAb_RT;
float4 unity_SHBr_RT;
float4 unity_SHBg_RT;
float4 unity_SHBb_RT;
float4 unity_SHC_RT;

TEXTURE2D(unity_Lightmap_RT);
SAMPLER(samplerunity_Lightmap_RT);

float4 unity_LightmapST_RT;
TEXTURE2D(unity_LightmapInd_RT);

float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap)
{
    // If there is no lightmap, it assume lightprobe
    #if defined(LIGHTMAP_ON)
        bool useRGBMLightmap = true;
        float4 decodeInstructions = float4(34.493242, 2.2, 0.0, 0.0); // Never used but needed for the interface since it supports gamma lightmaps

        #if defined(DIRLIGHTMAP_COMBINED)
            return SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_Lightmap_RT, samplerunity_Lightmap_RT), TEXTURE2D_PARAM(unity_LightmapInd_RT, samplerunity_Lightmap_RT),
                                                uvStaticLightmap, unity_LightmapST_RT, normalWS, useRGBMLightmap, decodeInstructions);
        #else
            return SampleSingleLightmap(TEXTURE2D_PARAM(unity_Lightmap_RT, samplerunity_Lightmap_RT), uvStaticLightmap, unity_LightmapST_RT, useRGBMLightmap, decodeInstructions);
        #endif
    #else
        // TODO: pass a tab of coefficient instead!
        real4 SHCoefficients[7];
        SHCoefficients[0] = unity_SHAr_RT;
        SHCoefficients[1] = unity_SHAg_RT;
        SHCoefficients[2] = unity_SHAb_RT;
        SHCoefficients[3] = unity_SHBr_RT;
        SHCoefficients[4] = unity_SHBg_RT;
        SHCoefficients[5] = unity_SHBb_RT;
        SHCoefficients[6] = unity_SHC_RT;

        return SampleSH9(SHCoefficients, normalWS);
    #endif
}


void InitBuiltinData(   float alpha, float3 normalWS, float3 backNormalWS, float3 positionRWS, float4 texCoord1, out BuiltinData builtinData)
{
    ZERO_INITIALIZE(BuiltinData, builtinData);

    builtinData.opacity = alpha;

    // Sample lightmap/lightprobe/volume proxy
    builtinData.bakeDiffuseLighting = SampleBakedGI(positionRWS, normalWS, texCoord1.xy);
    // We also sample the back lighting in case we have transmission. If not use this will be optimize out by the compiler
    // For now simply recall the function with inverted normal, the compiler should be able to optimize the lightmap case to not resample the directional lightmap
    // however it may not optimize the lightprobe case due to the proxy volume relying on dynamic if (to verify), not a problem for SH9, but a problem for proxy volume.
    // TODO: optimize more this code.    
    builtinData.backBakeDiffuseLighting = SampleBakedGI(positionRWS, backNormalWS, texCoord1.xy);

    // Use uniform directly - The float need to be cast to uint (as unity don't support to set a uint as uniform)
    builtinData.renderingLayers = 0;
}

#define USE_RAY_CONE_LOD

float computeTextureLOD(Texture2D targetTexture, float4 uvMask, float3 viewWS, float3 normalWS, RayCone rayCone, IntersectionVertice intersectionVertice)
{
    // First of all we need to grab the dimensions of the target texture
    uint texWidth, texHeight, numMips;
    targetTexture.GetDimensions(0, texWidth, texHeight, numMips);

    // Fetch the target area based on the mask
    float targetTexcoordArea = uvMask.x * intersectionVertice.texCoord0Area 
                        + uvMask.y * intersectionVertice.texCoord1Area
                        + uvMask.z * intersectionVertice.texCoord2Area
                        + uvMask.w * intersectionVertice.texCoord3Area;

    // Compute dot product between view and surface normal
    float lambda = 0.0; //0.5f * log2(targetTexcoordArea / intersectionVertice.triangleArea);
    lambda += log2(abs(rayCone.width));
    lambda += 0.5 * log2(texWidth * texHeight);
    lambda -= log2(abs(dot(viewWS, normalWS)));
    return lambda;
}

// InitBuiltinData must be call before calling PostInitBuiltinData
void PostInitBuiltinData(   float3 V, PositionInputs posInput, SurfaceData surfaceData,
                            inout BuiltinData builtinData)
{
    // Apply control from the indirect lighting volume settings - This is apply here so we don't affect emissive 
    // color in case of lit deferred for example and avoid material to have to deal with it
    builtinData.bakeDiffuseLighting *= _IndirectLightingMultiplier.x;
    builtinData.backBakeDiffuseLighting *= _IndirectLightingMultiplier.x;
#ifdef MODIFY_BAKED_DIFFUSE_LIGHTING
    ModifyBakedDiffuseLighting(V, posInput, surfaceData, builtinData);
#endif
}
