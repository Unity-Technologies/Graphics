#ifndef HD_SHADOW_SAMPLING_INCLUDED
#define HD_SHADOW_SAMPLING_INCLUDED
// Various shadow sampling logic.
// Again two versions, one for dynamic resource indexing, one for static resource access.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


// ------------------------------------------------------------------
//  PCF Filtering methods
// ------------------------------------------------------------------

real SampleShadow_Gather_PCF(float4 shadowAtlasSize, float3 coord, Texture2D tex, SamplerComparisonState compSamp, float depthBias)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    float2 f = frac(coord.xy * shadowAtlasSize.zw - 0.5f);

    float4 shadowMapTaps = GATHER_TEXTURE2D(tex, s_point_clamp_sampler, coord.xy);
    float4 shadowResults = (coord.z > shadowMapTaps.x);

    return lerp(lerp(shadowResults.w, shadowResults.z, f.x),
                lerp(shadowResults.x, shadowResults.y, f.x), f.y);
}

real SampleShadow_PCF_Tent_3x3(float4 shadowAtlasSize, float3 coord, Texture2D tex, SamplerComparisonState compSamp, float depthBias)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif
    real shadow = 0.0;
    real fetchesWeights[4];
    real2 fetchesUV[4];

    SampleShadow_ComputeSamples_Tent_3x3(shadowAtlasSize, coord.xy, fetchesWeights, fetchesUV);
    UNITY_LOOP
    for (int i = 0; i < 4; i++)
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[i].xy, coord.z)).x;
    }
    return shadow;
}

//
//                  5x5 tent PCF sampling (9 taps)
//

// shadowAtlasSize.xy is the shadow atlas size in pixel and shadowAtlasSize.zw is rcp(shadow atlas size)
real SampleShadow_PCF_Tent_5x5(float4 shadowAtlasSize, float3 coord, Texture2D tex, SamplerComparisonState compSamp, float depthBias)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif
    real shadow = 0.0;
    real fetchesWeights[9];
    real2 fetchesUV[9];

    SampleShadow_ComputeSamples_Tent_5x5(shadowAtlasSize, coord.xy, fetchesWeights, fetchesUV);

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
    // the loops are only there to prevent the compiler form coalescing all 9 texture fetches which increases register usage
    int i;
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 0] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 0].xy, coord.z)).x;
        shadow += fetchesWeights[ 1] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 1].xy, coord.z)).x;
        shadow += fetchesWeights[ 2] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 2].xy, coord.z)).x;
        shadow += fetchesWeights[ 3] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 3].xy, coord.z)).x;
    }

    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 4] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 4].xy, coord.z)).x;
        shadow += fetchesWeights[ 5] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 5].xy, coord.z)).x;
        shadow += fetchesWeights[ 6] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 6].xy, coord.z)).x;
        shadow += fetchesWeights[ 7] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 7].xy, coord.z)).x;
    }

    shadow += fetchesWeights[ 8] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 8].xy, coord.z)).x;
#else
    for (int i = 0; i < 9; i++)
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[i].xy, coord.z)).x;
    }
#endif

    return shadow;
}

//
//                  7x7 tent PCF sampling (16 taps)
//
real SampleShadow_PCF_Tent_7x7(float4 shadowAtlasSize, float3 coord, Texture2D tex, SamplerComparisonState compSamp, float depthBias)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif
    real shadow = 0.0;
    real fetchesWeights[16];
    real2 fetchesUV[16];

    SampleShadow_ComputeSamples_Tent_7x7(shadowAtlasSize, coord.xy, fetchesWeights, fetchesUV);

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
    // the loops are only there to prevent the compiler form coalescing all 16 texture fetches which increases register usage
    int i;
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 0] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 0].xy, coord.z)).x;
        shadow += fetchesWeights[ 1] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 1].xy, coord.z)).x;
        shadow += fetchesWeights[ 2] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 2].xy, coord.z)).x;
        shadow += fetchesWeights[ 3] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 3].xy, coord.z)).x;
    }
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 4] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 4].xy, coord.z)).x;
        shadow += fetchesWeights[ 5] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 5].xy, coord.z)).x;
        shadow += fetchesWeights[ 6] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 6].xy, coord.z)).x;
        shadow += fetchesWeights[ 7] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 7].xy, coord.z)).x;
    }
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 8] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 8].xy, coord.z)).x;
        shadow += fetchesWeights[ 9] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[ 9].xy, coord.z)).x;
        shadow += fetchesWeights[10] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[10].xy, coord.z)).x;
        shadow += fetchesWeights[11] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[11].xy, coord.z)).x;
    }
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[12] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[12].xy, coord.z)).x;
        shadow += fetchesWeights[13] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[13].xy, coord.z)).x;
        shadow += fetchesWeights[14] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[14].xy, coord.z)).x;
        shadow += fetchesWeights[15] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[15].xy, coord.z)).x;
    }
#else
    for(int i = 0; i < 16; i++)
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, real3(fetchesUV[i].xy, coord.z)).x;
    }
#endif

    return shadow;
}

//
//                  9 tap adaptive PCF sampling
//
float SampleShadow_PCF_9tap_Adaptive(float4 texelSizeRcp, float3 tcs, float filterSize, Texture2D tex, SamplerComparisonState compSamp, float depthBias)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    tcs.z += depthBias;
#endif

    texelSizeRcp *= filterSize;

    // Terms0 are weights for the individual samples, the other terms are offsets in texel space
    float4 vShadow3x3PCFTerms0 = float4(20.0 / 267.0, 33.0 / 267.0, 55.0 / 267.0, 0.0);
    float4 vShadow3x3PCFTerms1 = float4(texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y);
    float4 vShadow3x3PCFTerms2 = float4(texelSizeRcp.x,  texelSizeRcp.y, 0.0, 0.0);
    float4 vShadow3x3PCFTerms3 = float4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0, 0.0);

    float4 v20Taps;
    v20Taps.x = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z)).x; //  1  1
    v20Taps.y = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z)).x; // -1  1
    v20Taps.z = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z)).x; //  1 -1
    v20Taps.w = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z)).x; // -1 -1
    float flSum = dot(v20Taps.xyzw, float4(0.25, 0.25, 0.25, 0.25));
    // fully in light or shadow? -> bail
    if ((flSum == 0.0) || (flSum == 1.0))
        return flSum;

    // we're in a transition area, do 5 more taps
    flSum *= vShadow3x3PCFTerms0.x * 4.0;

    float4 v33Taps;
    v33Taps.x = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z)).x; //  1  0
    v33Taps.y = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z)).x; // -1  0
    v33Taps.z = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z)).x; //  0 -1
    v33Taps.w = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z)).x; //  0  1
    flSum += dot(v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy);

    flSum += SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, tcs).x * vShadow3x3PCFTerms0.z;

    return flSum;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/ShadowMoments.hlsl"

//
//                  1 tap VSM sampling
//
float SampleShadow_VSM_1tap(float3 tcs, float lightLeakBias, float varianceBias, Texture2D tex, SamplerState samp)
{
#if UNITY_REVERSED_Z
    float  depth      = 1.0 - tcs.z;
#else
    float  depth      = tcs.z;
#endif

    float2 moments = SAMPLE_TEXTURE2D_LOD(tex, samp, tcs.xy, 0.0).xy;

    return ShadowMoments_ChebyshevsInequality(moments, depth, varianceBias, lightLeakBias);
}

//
//                  1 tap EVSM sampling
//
float SampleShadow_EVSM_1tap(float3 tcs, float lightLeakBias, float varianceBias, float2 evsmExponents, bool fourMoments, Texture2D tex, SamplerState samp)
{
#if UNITY_REVERSED_Z
    float  depth      = 1.0 - tcs.z;
#else
    float  depth      = tcs.z;
#endif


    float4 moments = SAMPLE_TEXTURE2D_LOD(tex, samp, tcs.xy, 0.0);

    UNITY_BRANCH
    if (fourMoments)
    {
        float2 warpedDepth = ShadowMoments_WarpDepth(depth, evsmExponents);

        // Derivate of warping at depth
        float2 depthScale = evsmExponents * warpedDepth;
        float2 minVariance = depthScale * depthScale * varianceBias;

        float posContrib = ShadowMoments_ChebyshevsInequality(moments.xz, warpedDepth.x, minVariance.x, lightLeakBias);
        float negContrib = ShadowMoments_ChebyshevsInequality(moments.yw, warpedDepth.y, minVariance.y, lightLeakBias);
        return min(posContrib, negContrib);
    }
    else
    {
        float warpedDepth = ShadowMoments_WarpDepth_PosOnlyBaseTwo(depth, evsmExponents.x);

        // Derivate of warping at depth
        float depthScale = evsmExponents.x * warpedDepth;
        float minVariance = depthScale * depthScale * varianceBias;

        return ShadowMoments_ChebyshevsInequality(moments.xy, warpedDepth, minVariance, lightLeakBias);
    }
}


//
//                  1 tap MSM sampling
//
float SampleShadow_MSM_1tap(float3 tcs, float lightLeakBias, float momentBias, float depthBias, float bpp16, bool useHamburger, Texture2D tex, SamplerState samp)
{
#if UNITY_REVERSED_Z
    float  depth         = (1.0 - tcs.z) - depthBias;
#else
    float  depth         = tcs.z + depthBias;
#endif

    float4 moments = SAMPLE_TEXTURE2D_LOD(tex, samp, tcs.xy, 0.0);
    if (bpp16 != 0.0)
        moments = ShadowMoments_Decode16MSM(moments);

    float3 z;
    float4 b;
    ShadowMoments_SolveMSM(moments, depth, momentBias, z, b);

    if (useHamburger)
        return ShadowMoments_SolveDelta3MSM(z, b.xy, lightLeakBias);
    else
        return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM(z, b, lightLeakBias) : ShadowMoments_SolveDelta3MSM(z, b.xy, lightLeakBias);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDPCSS.hlsl"

//
//                  PCSS sampling
//
// Note shadowAtlasInfo contains: x: resolution, y: the inverse of atlas resolution
float SampleShadow_PCSS(float3 tcs, float2 posSS, float2 scale, float2 offset, float shadowSoftness, float minFilterRadius, int blockerSampleCount, int filterSampleCount, Texture2D tex, SamplerComparisonState compSamp, SamplerState samp, float depthBias, float4 zParams, bool isPerspective, float2 shadowAtlasInfo)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    tcs.z += depthBias;
#endif

    uint taaFrameIndex = _TaaFrameInfo.z;
    float sampleJitterAngle = InterleavedGradientNoise(posSS.xy, taaFrameIndex) * 2.0 * PI;
    float2 sampleJitter = float2(sin(sampleJitterAngle), cos(sampleJitterAngle));


    // Note: this is a hack, but the original implementation was faulty as it didn't scale offset based on the resolution of the atlas (*not* the shadow map).
    // All the softness fitting has been done using a reference 4096x4096, hence the following scale.
    float atlasResFactor = (4096 * shadowAtlasInfo.y);

    // max softness is empirically set resolution of 512, so needs to be set res independent.
    float shadowMapRes = scale.x * shadowAtlasInfo.x; // atlas is square
    float resIndepenentMaxSoftness = 0.04 * (shadowMapRes / 512);

    real halfTexelOffset = 0.5 * shadowAtlasInfo.y;

    real UMin = offset.x + halfTexelOffset;
    real UMax = offset.x + scale.x - halfTexelOffset;

    real VMin = offset.y + halfTexelOffset;
    real VMax = offset.y + scale.y - halfTexelOffset;

    //1) Blocker Search
    float averageBlockerDepth = 0.0;
    float numBlockers         = 0.0;
    bool blockerFound = BlockerSearch(averageBlockerDepth, numBlockers, min((shadowSoftness + 0.000001), resIndepenentMaxSoftness) * atlasResFactor, tcs, UMin, UMax, VMin, VMax, sampleJitter, tex, samp, blockerSampleCount);

    // We scale the softness also based on the distance between the occluder if we assume that the light is a sphere source.
    // Also, we don't bother if the blocker has not been found.
    if (isPerspective)
    {
        float dist = 1.0f / (zParams.z * averageBlockerDepth + zParams.w);
        dist = min(dist, 7.5);  // We need to clamp the distance as the fitted curve will do strange things after this and because there is no point in scale further after this point.
        float dist2 = dist * dist;
        float dist4 = dist2 * dist2;
        // Fitted curve to match ray trace reference as good as possible.
        float distScale = 3.298300241 - 2.001364639  * dist + 0.4967311427 * dist2 - 0.05464058455 * dist * dist2 + 0.0021974 * dist2 * dist2;
        shadowSoftness *= distScale;

        // Clamp maximum softness here again and not C# as it could be scaled signifcantly by the distance scale.
        shadowSoftness = min(shadowSoftness, resIndepenentMaxSoftness);
    }

    //2) Penumbra Estimation
    float filterSize = shadowSoftness * (isPerspective ? PenumbraSizePunctual(tcs.z, averageBlockerDepth) :
                                                         PenumbraSizeDirectional(tcs.z, averageBlockerDepth, zParams.x));
    filterSize = blockerFound ? max(filterSize, minFilterRadius) : minFilterRadius;
    filterSize *= atlasResFactor;

    //3) Filter
    // Note: we can't early out of the function if blockers are not found since Vulkan triggers a warning otherwise. Hence, we check for blockerFound here.
    return PCSS(tcs, UMin, UMax, VMin, VMax, filterSize, sampleJitter, tex, compSamp, filterSampleCount);
}


// TODO: This PCSS variant works for other types of lights as well, but is not well tested there, so we're introducing it only for area lights for now.
float SampleShadow_PCSS_Area(float3 posTCAtlas, float2 posSS, float2 shadowmapInAtlasScale, float2 shadowmapInAtlasOffset, float shadowSoftness, float minFilterRadius, int blockerSampleCount, int filterSampleCount, Texture2D tex, SamplerComparisonState compSamp, SamplerState samp, float depthBias, float4 zParams, bool isPerspective, float2 shadowAtlasInfo)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    posTCAtlas.z += depthBias;
#endif

    // This is a modified PCSS. Instead of performing both the blocker search and filtering phases using a flat disc of samples centered around
    // the shaded point, it adds a z offset to sample points extruding them in a cone shape - pyramid, actually - towards the light. The base of the pyramid
    // is the near plane of the area light (surface of the area light when near plane is at 0), the apex at the shaded point, and samples lie on the 4 sides
    // of the pyramid.
    //
    // The idea is that only casters within the volume of that pyramid would contribute to the shadow. In other words any casters caught by a sample with
    // z further away from the light than z of that sample don't contribute to the shadow.
    //
    // The maximum heigh of the pyramid is the z distance between the shaded point and the near plane. Lowering that height is necessary to keep
    // the sampling kernel sizes reasonable and is controlled by maxSampleZDistance. Higher maxSampleZDistance values result in wider penumbras.

    // Rescale the softness param so that the default 1 gives a very soft shadow without pushing it to edge, where artifacts start to show up.
    // This way setting softness to slightly more than 1 will get the shadow close to the raytraced reference, but with a more stable default.
    float maxSampleZDistance = shadowSoftness * 65.0;

    // Undo shadowmap-in-atlas scaling of this value, since we don't interpret it here as the size of the sampling kernel, but z distance
    float shadowmapWidth = shadowmapInAtlasScale.x * shadowAtlasInfo.x;
    // The 4096 literal is here for historical reasons
    maxSampleZDistance *= 4096.0 / shadowmapWidth;
    // TODO: move all the softness aka max distance rescaling to c#

    uint taaFrameIndex = _TaaFrameInfo.z;
    float sampleJitterAngle = InterleavedGradientNoise(posSS.xy, taaFrameIndex) * 2.0 * PI;
    float2 sampleJitter = float2(sin(sampleJitterAngle), cos(sampleJitterAngle));

    // TODO: should maybe pass it from an earlier stage instead of calculating it again
    float3 posTCShadowmap = float3((posTCAtlas.xy - shadowmapInAtlasOffset) / shadowmapInAtlasScale, posTCAtlas.z);

    real2 minCoord = shadowmapInAtlasOffset;
    real2 maxCoord = shadowmapInAtlasOffset + shadowmapInAtlasScale;

    //1) Blocker Search
    float blocker = 0.0;
    bool blockerFound = BlockerSearch_Area(blocker, maxSampleZDistance, shadowmapInAtlasScale, posTCAtlas.xy, posTCShadowmap, minCoord, maxCoord, sampleJitter, tex, samp, blockerSampleCount);

    //2) Penumbra Estimation
    maxSampleZDistance *= isPerspective ? PenumbraSizePunctual(posTCAtlas.z, blocker) : PenumbraSizeDirectional(posTCAtlas.z, blocker, zParams.x);
    // Extend the sampling cone only up to a certain margin before the blocker. Extending it past that distance will make samples miss the blocker and the shadow will fade.
    maxSampleZDistance = min(maxSampleZDistance, (blocker - posTCAtlas.z) * 0.9);
    // minFilterRadius can extend the cone past the above, so min&max instead of clamp.
    maxSampleZDistance = max(maxSampleZDistance, minFilterRadius * 10);

    //3) Filter
    // We can't early out of the function if blockers are not found since Vulkan triggers a warning otherwise
    bool withinShadowmap = all(posTCShadowmap.xy > 0) && all(posTCShadowmap.xy < 1);
    return blockerFound && withinShadowmap ? PCSS_Area(posTCAtlas.xy, posTCShadowmap, maxSampleZDistance, shadowmapInAtlasScale, shadowmapInAtlasOffset, minCoord, maxCoord, sampleJitter, tex, compSamp, filterSampleCount) : 1.0f;
}

// Adaptation of improved PCSS algorithm originally developed for area lights
float SampleShadow_PCSS_Directional(HDShadowData sd, float3 posTCAtlas, float2 posSS, float2 shadowmapInAtlasScale, float2 shadowmapInAtlasOffset, float texelSize, int blockerSampleCount, int filterSampleCount, Texture2D tex, SamplerComparisonState compSamp, SamplerState samp, float depthBias)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    posTCAtlas.z += depthBias;
#endif
    // This is a modified PCSS. Instead of performing both the blocker search and filtering phases using a flat disc of samples centered around
    // the shaded point, it adds a z offset to sample points extruding them in a cone shape towards the light. The base of the cone
    // is at infinity, the apex at the shaded point, and samples lie on the surface of the cone.
    //
    // The idea is that only casters within the volume of that pyramid would contribute to the shadow. In other words any casters caught by a sample with
    // z further away from the light than z of that sample don't contribute to the shadow.
    //
    // The maximum height of the pyramid is the z distance between the shaded point and a specified parameter, this clamps the penumbra size
    // and also avoid inconsistencies between cascades shall the distance exceed the near plane of a cascade because all occluders behind
    // the near plane are clamped and are considered closer to the receiver than they are.  As the cascade frustum can dynamically change, we
    // cannot use the near plane and it is thus best to use a fixed distance as tuning parameter so the penumbra size is at least consistent within
    // each cascade. Higher maxSampleZDistance values result in wider penumbras.
    float depth2RadialScale =  sd.dirLightPCSSParams0.x;
    float radial2DepthScale = sd.dirLightPCSSParams0.y;
    float maxBlockerDistance = sd.dirLightPCSSParams0.z;
    float maxSamplingDistance = sd.dirLightPCSSParams0.w;
    float minFilterRadius = texelSize * sd.dirLightPCSSParams1.x;
    float minFilterRadial2DepthScale = sd.dirLightPCSSParams1.y;
    float blockerRadial2DepthScale = sd.dirLightPCSSParams1.z;
    float blockerClumpSampleExponent = sd.dirLightPCSSParams1.w;
    float maxPCSSOffset = maxSamplingDistance * abs(sd.proj.z);
    float maxSampleZDistance = maxBlockerDistance * abs(sd.proj.z);

    // Fetch temporal rotational sample jitter
    uint taaFrameIndex = _TaaFrameInfo.z;
    float sampleJitterAngle = InterleavedGradientNoise(posSS.xy, taaFrameIndex) * 2.0 * PI;
    float2 sampleJitter = float2(sin(sampleJitterAngle), cos(sampleJitterAngle));

    //1) Blocker Search
    #if UNITY_REVERSED_Z
    float blockSearchFilterSize = max(min(1.0 - posTCAtlas.z, maxSampleZDistance) * depth2RadialScale, minFilterRadius);
    #else
    float blockSearchFilterSize = max(min(posTCAtlas.z, maxSampleZDistance) * depth2RadialScale, minFilterRadius);
    #endif
    float blockerDepth = 0.0;
    bool blockerFound = BlockerSearch_Directional(blockerDepth, blockSearchFilterSize, posTCAtlas, shadowmapInAtlasScale, shadowmapInAtlasOffset, sampleJitter, tex, samp, blockerSampleCount, blockerRadial2DepthScale, minFilterRadius, minFilterRadial2DepthScale, blockerClumpSampleExponent);

    //2) Penumbra Estimation
    // Extend the sampling cone only up to a certain margin before the blocker. Extending it past that distance will make samples miss the blocker and the shadow will fade.
    float blockerDistance = min(abs(blockerDepth - posTCAtlas.z) * 0.9, maxSampleZDistance);
    // Convert to filter size and clamp to minFilterRadius
    float filterSize = blockerDistance * depth2RadialScale;
    float samplingFilterSize = max(filterSize, minFilterRadius);

    // Ensure PCSS sampling offset is at most at 25% of the blocker distance to deal
    // with very close blockers causing light bleeding across jagged depths in shadowmap
    maxPCSSOffset = min(maxPCSSOffset, blockerDistance * 0.25);

    //3) Filter
    // Note: we can't early out of the function if blockers are not found since Vulkan triggers a warning otherwise. Hence, we check for blockerFound here.
    return blockerFound ? PCSS_Directional(posTCAtlas, filterSize, shadowmapInAtlasScale, shadowmapInAtlasOffset, sampleJitter, tex, compSamp, filterSampleCount, minFilterRadial2DepthScale, maxPCSSOffset, samplingFilterSize) : 1.0f;
}

// Note this is currently not available as an option, but is left here to show what needs including if IMS is to be used.
// Also, please note that the UI for the IMS parameters has been deleted, but the parameters are still in the relevant data structures.
// To make use of IMS, please make sure GetDirectionalShadowAlgorithm returns DirectionalShadowAlgorithm.IMS and that there is a UI for the parameters kernelSize, lightAngle and maxDepthBias
// These parameters need to be set in the shadowData.shadowFilterParams0 as follow:
//  shadowData.shadowFilterParams0.x = shadowRequest.kernelSize;
//  shadowData.shadowFilterParams0.y = shadowRequest.lightAngle;
//  shadowData.shadowFilterParams0.z = shadowRequest.maxDepthBias;

// #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDIMS.hlsl"
#endif
