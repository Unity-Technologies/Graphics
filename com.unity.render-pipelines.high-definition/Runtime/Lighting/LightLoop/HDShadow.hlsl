#ifndef LIGHTLOOP_HD_SHADOW_HLSL
#define LIGHTLOOP_HD_SHADOW_HLSL

#define SHADOW_OPTIMIZE_REGISTER_USAGE 1

#ifndef SHADOW_USE_DEPTH_BIAS
#define SHADOW_USE_DEPTH_BIAS                   1   // Enable clip space z biasing
#endif

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
#   pragma warning( disable : 3557 ) // loop only executes for 1 iteration(s)
#endif

# include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowContext.hlsl"

// normalWS is the vertex normal if available or shading normal use to bias the shadow position
float GetDirectionalShadowAttenuation(HDShadowContext shadowContext, float2 positionSS, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L)
{
    // If NdotL < 0, we flip the normal in case it is used for the transmission to correctly bias shadow position
    normalWS *= FastSign(dot(normalWS, L));
#if defined(SHADOW_LOW) || defined(SHADOW_MEDIUM)
    return EvalShadow_CascadedDepth_Dither(shadowContext, _ShadowmapCascadeAtlas, s_linear_clamp_compare_sampler, positionSS, positionWS, normalWS, shadowDataIndex, L);
#else
    return EvalShadow_CascadedDepth_Blend(shadowContext, _ShadowmapCascadeAtlas, s_linear_clamp_compare_sampler, positionSS, positionWS, normalWS, shadowDataIndex, L);
#endif
}

float GetPunctualShadowAttenuation(HDShadowContext shadowContext, float2 positionSS, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, bool pointLight, bool perspecive)
{
#if (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER))
    shadowDataIndex = WaveReadLaneFirst(shadowDataIndex);
#endif

    // If NdotL < 0, we flip the normal in case it is used for the transmission to correctly bias shadow position
    normalWS *= FastSign(dot(normalWS, L));

    // Note: Here we assume that all the shadow map cube faces have been added contiguously in the buffer to retreive the shadow information
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex];

    if (pointLight)
    {
        sd.rot0 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot0;
        sd.rot1 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot1;
        sd.rot2 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot2;
        sd.atlasOffset = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].atlasOffset;
    }

    return EvalShadow_PunctualDepth(sd, _ShadowmapAtlas, s_linear_clamp_compare_sampler, positionSS, positionWS, normalWS, L, L_dist, perspecive);
}

float GetPunctualShadowClosestDistance(HDShadowContext shadowContext, SamplerState sampl, real3 positionWS, int shadowDataIndex, float3 L, float3 lightPositionWS, bool pointLight)
{
#if (defined(PLATFORM_SUPPORTS_WAVE_INTRINSICS) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER))
    shadowDataIndex = WaveReadLaneFirst(shadowDataIndex);
#endif

    // Note: Here we assume that all the shadow map cube faces have been added contiguously in the buffer to retreive the shadow information
    // TODO: if on the light type to retrieve the good shadow data
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
    
    if (pointLight)
    {
        sd.shadowToWorld = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].shadowToWorld;
        sd.atlasOffset = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].atlasOffset;
        sd.rot0 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot0;
        sd.rot1 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot1;
        sd.rot2 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot2;
    }
    
    return EvalShadow_SampleClosestDistance_Punctual(sd, _ShadowmapAtlas, sampl, positionWS, L, lightPositionWS);
}

float GetAreaLightAttenuation(HDShadowContext shadowContext, float2 positionSS, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist)
{
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
    return EvalShadow_AreaDepth(sd, _AreaShadowmapMomentAtlas, positionSS, positionWS, normalWS, L, L_dist, true);
}


#endif // LIGHTLOOP_HD_SHADOW_HLSL
