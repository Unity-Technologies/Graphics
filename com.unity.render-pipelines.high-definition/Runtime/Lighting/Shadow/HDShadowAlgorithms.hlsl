#ifndef HD_SHADOW_ALGORITHMS_INCLUDED
#define HD_SHADOW_ALGORITHMS_INCLUDED
// Configure which shadow algorithms to use per shadow level quality

// Since we use slope-scale bias, the constant bias is for now set as a small fixed value
#define FIXED_UNIFORM_BIAS (1.0f / 65536.0f)

// WARNINGS:
// Keep in sync with both HDShadowManager::GetDirectionalShadowAlgorithm() and GetPunctualFilterWidthInTexels() in C# as well!

#ifdef SHADOW_ULTRA_LOW
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_PCF_Tent_3x3(sd.isInCachedAtlas ? _CachedShadowAtlasSize.zwxy : _ShadowAtlasSize.zwxy, posTC, tex, samp, bias)
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_Gather_PCF(_CascadeShadowAtlasSize.zwxy, posTC, tex, samp, bias)
#elif defined(SHADOW_LOW)
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_PCF_Tent_3x3(sd.isInCachedAtlas ? _CachedShadowAtlasSize.zwxy : _ShadowAtlasSize.zwxy, posTC, tex, samp, bias)
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_PCF_Tent_5x5(_CascadeShadowAtlasSize.zwxy, posTC, tex, samp, bias)
#elif defined(SHADOW_MEDIUM)
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_PCF_Tent_5x5(sd.isInCachedAtlas ? _CachedShadowAtlasSize.zwxy : _ShadowAtlasSize.zwxy, posTC, tex, samp, bias)
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_PCF_Tent_7x7(_CascadeShadowAtlasSize.zwxy, posTC, tex, samp, bias)
// Note: currently quality settings for PCSS need to be expose in UI and is control in HDLightUI.cs file IsShadowSettings
#elif defined(SHADOW_HIGH)
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_PCSS(posTC, posSS, sd.shadowMapSize.xy * (sd.isInCachedAtlas ? _CachedShadowAtlasSize.zw : _ShadowAtlasSize.zw), sd.atlasOffset, sd.shadowFilterParams0.x, sd.shadowFilterParams0.w, asint(sd.shadowFilterParams0.y), asint(sd.shadowFilterParams0.z), tex, samp, s_point_clamp_sampler, bias, sd.zBufferParam, true, (sd.isInCachedAtlas ? _CachedShadowAtlasSize.xz : _ShadowAtlasSize.xz))
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_PCSS(posTC, posSS, sd.shadowMapSize.xy * _CascadeShadowAtlasSize.zw, sd.atlasOffset, sd.shadowFilterParams0.x, sd.shadowFilterParams0.w, asint(sd.shadowFilterParams0.y), asint(sd.shadowFilterParams0.z), tex, samp, s_point_clamp_sampler, bias, sd.zBufferParam, false, _CascadeShadowAtlasSize.xz)
#endif

#ifndef PUNCTUAL_FILTER_ALGORITHM
#error "Undefined punctual shadow filter algorithm"
#endif
#ifndef DIRECTIONAL_FILTER_ALGORITHM
#error "Undefined directional shadow filter algorithm"
#endif

float4 EvalShadow_WorldToShadow(HDShadowData sd, float3 positionWS, bool perspProj)
{
    // Note: Due to high VGRP load we can't use the whole view projection matrix, instead we reconstruct it from
    // rotation, position and projection vectors (projection and position are stored in SGPR)
#if 0
    return mul(viewProjection, float4(positionWS, 1));
#else

    if(perspProj)
    {
        positionWS = positionWS - sd.pos;
        float3x3 view = { sd.rot0, sd.rot1, sd.rot2 };
        positionWS = mul(view, positionWS);
    }
    else
    {
        float3x4 view;
        view[0] = float4(sd.rot0, sd.pos.x);
        view[1] = float4(sd.rot1, sd.pos.y);
        view[2] = float4(sd.rot2, sd.pos.z);
        positionWS = mul(view, float4(positionWS, 1.0)).xyz;
    }

    float4x4 proj;
    proj = 0.0;
    proj._m00 = sd.proj[0];
    proj._m11 = sd.proj[1];
    proj._m22 = sd.proj[2];
    proj._m23 = sd.proj[3];
    if(perspProj)
        proj._m32 = -1.0;
    else
        proj._m33 = 1.0;

    return mul(proj, float4(positionWS, 1.0));
#endif
}

// function called by spot, point and directional eval routines to calculate shadow coordinates
float3 EvalShadow_GetTexcoordsAtlas(HDShadowData sd, float2 atlasSizeRcp, float3 positionWS, out float3 posNDC, bool perspProj)
{
    float4 posCS = EvalShadow_WorldToShadow(sd, positionWS, perspProj);
    // Avoid (0 / 0 = NaN).
    posNDC = (perspProj && posCS.w != 0) ? (posCS.xyz / posCS.w) : posCS.xyz;

    // calc TCs
    float3 posTC = float3(saturate(posNDC.xy * 0.5 + 0.5), posNDC.z);
    posTC.xy = posTC.xy * sd.shadowMapSize.xy * atlasSizeRcp + sd.atlasOffset;

    return posTC;
}

float3 EvalShadow_GetTexcoordsAtlas(HDShadowData sd, float2 atlasSizeRcp, float3 positionWS, bool perspProj)
{
    float3 ndc;
    return EvalShadow_GetTexcoordsAtlas(sd, atlasSizeRcp, positionWS, ndc, perspProj);
}

float2 EvalShadow_GetTexcoordsAtlas(HDShadowData sd, float2 atlasSizeRcp, float3 positionWS, out float2 closestSampleNDC, bool perspProj)
{
    float4 posCS = EvalShadow_WorldToShadow(sd, positionWS, perspProj);
    // Avoid (0 / 0 = NaN).
    float2 posNDC = (perspProj && posCS.w != 0) ? (posCS.xy / posCS.w) : posCS.xy;

    // calc TCs
    float2 posTC = posNDC * 0.5 + 0.5;
    closestSampleNDC = (floor(posTC * sd.shadowMapSize.xy) + 0.5) * sd.shadowMapSize.zw * 2.0 - 1.0.xx;
    return posTC * sd.shadowMapSize.xy * atlasSizeRcp + sd.atlasOffset;
}

uint2 EvalShadow_GetIntTexcoordsAtlas(HDShadowData sd, float4 atlasSize, float3 positionWS, out float2 closestSampleNDC, bool perspProj)
{
    float2 texCoords = EvalShadow_GetTexcoordsAtlas(sd, atlasSize.zw, positionWS, closestSampleNDC, perspProj);
    return uint2(texCoords * atlasSize.xy);
}

//
//  Biasing functions
//

// helper function to get the world texel size
float EvalShadow_WorldTexelSize(float worldTexelSize, float L_dist, bool perspProj)
{
    return perspProj ? (worldTexelSize * L_dist) : worldTexelSize;
}

// receiver bias either using the normal to weight normal and view biases, or just light view biasing
float3 EvalShadow_NormalBias(float worldTexelSize, float normalBias, float3 normalWS)
{
    float normalBiasMult = normalBias * worldTexelSize;
    return normalWS * normalBiasMult;
}
//
//  Point shadows
//
float EvalShadow_PunctualDepth(HDShadowData sd, Texture2D tex, SamplerComparisonState samp, float2 positionSS, float3 positionWS, float3 normalWS, float3 L, float L_dist, bool perspective)
{
    float2 texelSize = sd.isInCachedAtlas ? _CachedShadowAtlasSize.zw : _ShadowAtlasSize.zw;
    positionWS = positionWS + sd.cacheTranslationDelta.xyz;
    /* bias the world position */
    float worldTexelSize = EvalShadow_WorldTexelSize(sd.worldTexelSize, L_dist, true);
    float3 normalBias = EvalShadow_NormalBias(worldTexelSize, sd.normalBias, normalWS);
    positionWS += normalBias;
    /* get shadowmap texcoords */
    float3 posTC = EvalShadow_GetTexcoordsAtlas(sd, texelSize, positionWS, perspective);
    /* sample the texture */
    // We need to do the check on min/max coordinates because if the shadow spot angle is smaller than the actual cone, then we could have artifacts due to the clamp sampler.
    float2 maxCoord = (sd.shadowMapSize.xy - 0.5f) * texelSize + sd.atlasOffset;
    float2 minCoord = sd.atlasOffset;
    return any(posTC.xy > maxCoord || posTC.xy < minCoord) ? 1.0f : PUNCTUAL_FILTER_ALGORITHM(sd, positionSS, posTC, tex, samp, FIXED_UNIFORM_BIAS);
}

//
//  Area light shadows
//
float EvalShadow_AreaDepth(HDShadowData sd, Texture2D tex, float2 positionSS, float3 positionWS, float3 normalWS, float3 L, float L_dist, bool perspective)
{
    float2 texelSize = sd.isInCachedAtlas ? _CachedAreaShadowAtlasSize.zw : _AreaShadowAtlasSize.zw;

    positionWS = positionWS + sd.cacheTranslationDelta.xyz;

    /* get shadowmap texcoords */
    float3 posTC = EvalShadow_GetTexcoordsAtlas(sd, texelSize, positionWS, perspective);

    int blurPassesScale = (1 + min(4, sd.shadowFilterParams0.w) * 4.0f);// This is needed as blurring might cause some leaks. It might be overclipping, but empirically is a good value.
    float2 maxCoord = (sd.shadowMapSize.xy - 0.5f * blurPassesScale) * texelSize + sd.atlasOffset;
    float2 minCoord = sd.atlasOffset + texelSize * blurPassesScale;

    if (any(posTC.xy > maxCoord || posTC.xy < minCoord))
    {
        return 1.0f;
    }
    else
    {
        float2 exponents = sd.shadowFilterParams0.xx;
        float lightLeakBias = sd.shadowFilterParams0.y;
        float varianceBias = sd.shadowFilterParams0.z;
        return SampleShadow_EVSM_1tap(posTC, lightLeakBias, varianceBias, exponents, false, tex, s_linear_clamp_sampler);
    }
}


//
//  Directional shadows (cascaded shadow map)
//

int EvalShadow_GetSplitIndex(HDShadowContext shadowContext, int index, float3 positionWS, out float alpha, out int cascadeCount)
{
    uint   i = 0;
    float  relDistance = 0.0;
    float3 wposDir, splitSphere;

    HDDirectionalShadowData dsd = shadowContext.directionalShadowData;

    // find the current cascade
    for (; i < _CascadeShadowCount; i++)
    {
        float4  sphere  = dsd.sphereCascades[i];
                wposDir = -sphere.xyz + positionWS;
        float   distSq  = dot(wposDir, wposDir);
        relDistance = distSq / sphere.w;
        if (relDistance > 0.0 && relDistance <= 1.0)
        {
            splitSphere = sphere.xyz;
            wposDir    /= sqrt(distSq);
            break;
        }
    }
    int shadowSplitIndex = i < _CascadeShadowCount ? i : -1;

    cascadeCount = dsd.cascadeDirection.w;
    float border = dsd.cascadeBorders[shadowSplitIndex];
    alpha = border <= 0.0 ? 0.0 : saturate((relDistance - (1.0 - border)) / border);

    // The above code will generate transitions on the whole cascade sphere boundary.
    // It means that depending on the light and camera direction, sometimes the transition appears on the wrong side of the cascade
    // To avoid that we attenuate the effect (lerp very sharply to 0.0) when view direction and cascade center to pixel vector face opposite directions.
    // This way you only get fade out on the right side of the cascade.
    float3 viewDir = GetWorldSpaceViewDir(positionWS);

    float  cascDot = dot(viewDir, wposDir);
    // At high border sizes the sharp lerp is noticeable, hence we need to lerp how sharpenss factor
    // if we are below 80% we keep the very sharp transition.
    float lerpSharpness = 8.0f + 512.0f * smoothstep(1.0f, 0.7f, border);// lerp(1024.0f, 8.0f, saturate(border - 0.8) / 0.2f);
    alpha = lerp(alpha, 0.0, saturate(cascDot * lerpSharpness));
    return shadowSplitIndex;
}

void LoadDirectionalShadowDatas(inout HDShadowData sd, HDShadowContext shadowContext, int index)
{
    sd.proj = shadowContext.shadowDatas[index].proj;
    sd.pos = shadowContext.shadowDatas[index].pos;
    sd.worldTexelSize = shadowContext.shadowDatas[index].worldTexelSize;
    sd.atlasOffset = shadowContext.shadowDatas[index].atlasOffset;
#if defined(SHADOW_HIGH)
    sd.shadowFilterParams0.x = shadowContext.shadowDatas[index].shadowFilterParams0.x;
    sd.zBufferParam = shadowContext.shadowDatas[index].zBufferParam;
#endif
    sd.cacheTranslationDelta = shadowContext.shadowDatas[index].cacheTranslationDelta;
}

float EvalShadow_CascadedDepth_Blend_SplitIndex(HDShadowContext shadowContext, Texture2D tex, SamplerComparisonState samp, float2 positionSS, float3 positionWS, float3 normalWS, int index, float3 L, out int shadowSplitIndex)
{
    float   alpha;
    int     cascadeCount;
    float   shadow = 1.0;
    shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, alpha, cascadeCount);

    float3 basePositionWS = positionWS;

    if (shadowSplitIndex >= 0.0)
    {
        HDShadowData sd = shadowContext.shadowDatas[index];
        LoadDirectionalShadowDatas(sd, shadowContext, index + shadowSplitIndex);
        positionWS = basePositionWS + sd.cacheTranslationDelta.xyz;

        /* normal based bias */
        float3 orig_pos = positionWS;
        float3 normalBias = EvalShadow_NormalBias(sd.worldTexelSize, sd.normalBias, normalWS);
        positionWS += normalBias;

        /* get shadowmap texcoords */
        float3 posTC = EvalShadow_GetTexcoordsAtlas(sd, _CascadeShadowAtlasSize.zw, positionWS, false);
        /* evalute the first cascade */
        shadow = DIRECTIONAL_FILTER_ALGORITHM(sd, positionSS, posTC, tex, samp, FIXED_UNIFORM_BIAS);
        float  shadow1    = 1.0;

        shadowSplitIndex++;
        if (shadowSplitIndex < cascadeCount)
        {
            shadow1 = shadow;

            if (alpha > 0.0)
            {
                LoadDirectionalShadowDatas(sd, shadowContext, index + shadowSplitIndex);
                float3 evaluationPosWS = basePositionWS + sd.cacheTranslationDelta.xyz + normalBias;
                float3 posNDC;
                posTC = EvalShadow_GetTexcoordsAtlas(sd, _CascadeShadowAtlasSize.zw, evaluationPosWS, posNDC, false);
                /* sample the texture */
                UNITY_BRANCH
                if (all(abs(posNDC.xy) <= (1.0 - sd.shadowMapSize.zw * 0.5)))
                    shadow1 = DIRECTIONAL_FILTER_ALGORITHM(sd, positionSS, posTC, tex, samp, FIXED_UNIFORM_BIAS);
            }
        }
        shadow = lerp(shadow, shadow1, alpha);
    }

    return shadow;
}

float EvalShadow_CascadedDepth_Blend(HDShadowContext shadowContext, Texture2D tex, SamplerComparisonState samp, float2 positionSS, float3 positionWS, float3 normalWS, int index, float3 L)
{
    int unusedSplitIndex;
    return EvalShadow_CascadedDepth_Blend_SplitIndex(shadowContext, tex, samp, positionSS, positionWS, normalWS, index, L, unusedSplitIndex);
}

float EvalShadow_CascadedDepth_Dither_SplitIndex(HDShadowContext shadowContext, Texture2D tex, SamplerComparisonState samp, float2 positionSS, float3 positionWS, float3 normalWS, int index, float3 L, out int shadowSplitIndex)
{
    float   alpha;
    int     cascadeCount;
    float   shadow = 1.0;
    shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, alpha, cascadeCount);

    float3 basePositionWS = positionWS;

    if (shadowSplitIndex >= 0.0)
    {
        HDShadowData sd = shadowContext.shadowDatas[index];
        LoadDirectionalShadowDatas(sd, shadowContext, index + shadowSplitIndex);
        positionWS = basePositionWS + sd.cacheTranslationDelta.xyz;

        /* normal based bias */
        float worldTexelSize = sd.worldTexelSize;
        float3 normalBias = EvalShadow_NormalBias(worldTexelSize, sd.normalBias, normalWS);

        /* We select what split we need to sample from */
        float nextSplit = min(shadowSplitIndex + 1, cascadeCount - 1);
        bool evalNextCascade = nextSplit != shadowSplitIndex && alpha > 0 && step(InterleavedGradientNoise(positionSS.xy, _TaaFrameInfo.z), alpha);

        if (evalNextCascade)
        {
            LoadDirectionalShadowDatas(sd, shadowContext, index + nextSplit);
            positionWS = basePositionWS + sd.cacheTranslationDelta.xyz;
            float biasModifier = (sd.worldTexelSize / worldTexelSize);
            normalBias *= biasModifier;
        }

        positionWS += normalBias;
        float3 posTC = EvalShadow_GetTexcoordsAtlas(sd, _CascadeShadowAtlasSize.zw, positionWS, false);

        shadow = DIRECTIONAL_FILTER_ALGORITHM(sd, positionSS, posTC, tex, samp, FIXED_UNIFORM_BIAS);
        shadow = (shadowSplitIndex < cascadeCount - 1) ? shadow : lerp(shadow, 1.0, alpha);
    }

    return shadow;
}

float EvalShadow_CascadedDepth_Dither(HDShadowContext shadowContext, Texture2D tex, SamplerComparisonState samp, float2 positionSS, float3 positionWS, float3 normalWS, int index, float3 L)
{
    int unusedSplitIndex;
    return EvalShadow_CascadedDepth_Dither_SplitIndex(shadowContext, tex, samp, positionSS, positionWS, normalWS, index, L, unusedSplitIndex);
}

// TODO: optimize this using LinearEyeDepth() to avoid having to pass the shadowToWorld matrix
float EvalShadow_SampleClosestDistance_Punctual(HDShadowData sd, Texture2D tex, SamplerState sampl, float3 positionWS, float3 L, float3 lightPositionWS)
{
    float2 texelSize = sd.isInCachedAtlas ? _CachedShadowAtlasSize.zw : _ShadowAtlasSize.zw;

    float4 closestNDC = { 0,0,0,1 };
    float2 texelIdx = EvalShadow_GetTexcoordsAtlas(sd, texelSize, positionWS, closestNDC.xy, true);

    // sample the shadow map
    closestNDC.z = SAMPLE_TEXTURE2D_LOD(tex, sampl, texelIdx, 0).x;

    // reconstruct depth position
    float4 closestWS = mul(closestNDC, sd.shadowToWorld);
    float3 occluderPosWS = closestWS.xyz / closestWS.w;

    return distance(occluderPosWS, lightPositionWS);
}
#endif
