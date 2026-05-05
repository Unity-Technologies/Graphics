#ifndef UNIVERSAL_SSR_INCLUDED
#define UNIVERSAL_SSR_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ScreenSpaceReflectionCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

// Textures & Samplers
TEXTURE2D_X(_CameraColorTexture);
SAMPLER(sampler_CameraColorTexture);

TEXTURE2D_X(_SmoothnessTexture);
SAMPLER(sampler_SmoothnessTexture);

TEXTURE2D_X(_MotionVectorColorTexture);
SAMPLER(sampler_MotionVectorColorTexture);

TEXTURE2D_X(_LastFrameCameraDepthTexture);

SAMPLER(sampler_BlitTexture);

// Params
float4x4 _CameraViewProjections[2];
float4x4 _CameraInverseViewProjections[2];
float4x4 _CameraProjections[2];
float4x4 _CameraInverseProjections[2];
float4x4 _CameraViews[2];
float4 _CameraDeltaJitterOffset;
float4 _SourceSize;

TYPED_TEXTURE2D_X(float, _DepthPyramid);
float4 _DepthPyramidMipLevelOffsets[15];
int _SsrDepthPyramidMaxMip;

// SSR Settings
float4 _MaxRayLength;
int _MaxRaySteps;
uint _Downsample;
int _HiZTrace;
int _HitRefinementSteps;
float4 _ThicknessScaleAndBias;
float4 _MinimumSmoothnessAndFadeStart;
float4 _ScreenEdgeFadeAndViewConeDot;
int _ReflectSky;

float GetMaxRayLength()
{
    return _MaxRayLength.x;
}

float GetRayLengthFadeStart()
{
    return _MaxRayLength.y;
}

float GetThicknessScale()
{
    return _ThicknessScaleAndBias.x;
}

float GetThicknessBias()
{
    return _ThicknessScaleAndBias.y;
}

float GetThicknessScaleFine()
{
    return _ThicknessScaleAndBias.z;
}

float GetThicknessBiasFine()
{
    return _ThicknessScaleAndBias.w;
}

float GetViewConeDot()
{
    return _ScreenEdgeFadeAndViewConeDot.z;
}

float2 GetScreenEdgeFade()
{
    return _ScreenEdgeFadeAndViewConeDot.xy;
}

float GetMinimumSmoothness()
{
    return _MinimumSmoothnessAndFadeStart.x;
}

#if defined(USING_STEREO_MATRICES)
#define unity_eyeIndex unity_StereoEyeIndex
#else
#define unity_eyeIndex 0
#endif

// Constants
#define SSR_TRACE_EPS 0.000488281f

// ------------------------------------------------------------------
// Screen Space Marching
// ------------------------------------------------------------------
bool TraceScreenSpaceRay(
    float2 startPosSS,
    float startZ,
    float2 endPosSS,
    float endZ,
    float4 screenSizeWithInverse,
    out float3 rayHitPosNDC,
    out int iterCount)
{
    // Calculate the step to take each iteration, and the total step count
    float rayScreenDeltaX = endPosSS.x - startPosSS.x;
    float rayScreenDeltaY = endPosSS.y - startPosSS.y;
    float rayScreenDeltaZ = endZ - startZ;
    float useDeltaX = abs(rayScreenDeltaX) >= abs(rayScreenDeltaY) ? 1.0 : 0.0;
    float rayScreenDelta = min(lerp(abs(rayScreenDeltaY), abs(rayScreenDeltaX), useDeltaX), _MaxRaySteps);
    float3 rayStep = float3(rayScreenDeltaX, rayScreenDeltaY, rayScreenDeltaZ) / max(rayScreenDelta, 0.001);

    // March against depth buffer with coarse steps
    float3 rayPosSS = float3(startPosSS, startZ);
    float rayHitT = 0;
    rayHitPosNDC = 0;
    float prevT = 0;
    bool hitCoarse = false;

    for (iterCount = 0; iterCount < rayScreenDelta; iterCount++)
    {
        rayPosSS += rayStep;

        // We went offscreen, so stop
        if (rayPosSS.x < 0 || rayPosSS.x > screenSizeWithInverse.z || rayPosSS.y < 0 || rayPosSS.y > screenSizeWithInverse.w)
            return false;

        // How far along the ray are we in [0; 1]?
        rayHitT = lerp((rayPosSS.y - startPosSS.y) / rayScreenDeltaY, (rayPosSS.x - startPosSS.x) / rayScreenDeltaX, useDeltaX);

        // Get current depth of scene at the ray position.
        float rawSceneDepth = LoadSceneDepth(rayPosSS.xy * _Downsample);

        // Check if we've hit something
        bool aboveBase = !COMPARE_DEVICE_DEPTH_CLOSER(rayPosSS.z, rawSceneDepth);
        bool belowFloor = COMPARE_DEVICE_DEPTH_CLOSER(rayPosSS.z, rawSceneDepth * GetThicknessScale() + GetThicknessBias());
        if (aboveBase && belowFloor)
        {
            hitCoarse = true;
            break;
        }
        prevT = rayHitT;
    }
    rayHitPosNDC = float3(rayPosSS.xy * screenSizeWithInverse.xy, rayPosSS.z);

    #ifdef _REFINE_DEPTH
    if (hitCoarse)
    {
        // Refine depth by testing intersections at points between the last 2 coarse positions,
        // using a smaller thickness value.
        float t0 = prevT;
        float t1 = 2.0 * rayHitT - t0;

        int step = 0;
        bool hitFine = false;
        for (; step < _HitRefinementSteps; step++)
        {
            float t = t0 + (t1 - t0) * 0.5;

            float2 candidateHitPosSS = lerp(startPosSS, endPosSS, t);
            candidateHitPosSS = round(candidateHitPosSS - 0.5) + 0.5; // round to nearest texel center
            float rayDepth = lerp(startZ, endZ, t);
            float rawSceneDepth = LoadSceneDepth(candidateHitPosSS * _Downsample);

            bool aboveBase = !COMPARE_DEVICE_DEPTH_CLOSER(rayDepth, rawSceneDepth);
            bool belowFloor = COMPARE_DEVICE_DEPTH_CLOSER(rayDepth, rawSceneDepth * GetThicknessScale() + GetThicknessBias());
            [branch]
            if (aboveBase && belowFloor)
            {
                hitFine = COMPARE_DEVICE_DEPTH_CLOSER(rayDepth, rawSceneDepth * GetThicknessScaleFine() + GetThicknessBiasFine());

                rayHitPosNDC = float3(candidateHitPosSS * screenSizeWithInverse.xy, rayDepth);
                t1 = t;
            }
            else
            {
                t0 = t;
            }
        }
        iterCount += step;

        if (!hitFine)
            return false;
    }
    #endif

    return hitCoarse || _ReflectSky;
}

bool TraceScreenSpaceRayHiZ(
    float2 startPosSS,
    float startZ,
    float2 endPosSS,
    float endZ,
    float2 screenSize,
    out float3 rayHitPosNDC,
    out int iterCount)
{
    // We start tracing from the center of the current pixel, and do so up to the far plane.
    float3 rayOrigin = float3(startPosSS, startZ);

    float3 rayDir     = float3(endPosSS, endZ) - rayOrigin;
    float3 rcpRayDir  = rcp(rayDir);
    int2   rayStep    = int2(rcpRayDir.x >= 0 ? 1 : 0,
                             rcpRayDir.y >= 0 ? 1 : 0);
    float3 raySign  = float3(rcpRayDir.x >= 0 ? 1 : -1,
                             rcpRayDir.y >= 0 ? 1 : -1,
                             rcpRayDir.z >= 0 ? 1 : -1);
    bool rayTowardsEye = COMPARE_DEVICE_DEPTH_CLOSEREQUAL(rcpRayDir.z, 0);

    // Extend and clip the end point to the frustum.
    float tMax;
    {
        // Shrink the frustum by half a texel for efficiency reasons.
        const float halfTexel = 0.5;

        float3 bounds;
        bounds.x = (rcpRayDir.x >= 0) ? screenSize.x - halfTexel : halfTexel;
        bounds.y = (rcpRayDir.y >= 0) ? screenSize.y - halfTexel : halfTexel;
        // If we do not want to intersect the skybox, it is more efficient to not trace too far.
        float maxDepth = (_ReflectSky != 0) ? -0.00000024 : 0.00000024; // 2^-22
        #if !defined(UNITY_REVERSED_Z)
        maxDepth = 1.0-maxDepth;
        #endif
        bounds.z = rayTowardsEye ? (1.0-UNITY_RAW_FAR_CLIP_VALUE) : maxDepth;

        float3 dist = bounds * rcpRayDir - (rayOrigin * rcpRayDir);
        tMax = Min3(dist.x, dist.y, dist.z);
    }

    // Clamp the MIP level to give the compiler more information to optimize.
    const int maxMipLevel = min(_SsrDepthPyramidMaxMip, 14);

    // Start ray marching from the next texel to avoid self-intersections.
    float t;
    {
        // 'rayOrigin' is the exact texel center.
        float2 dist = abs(0.5 * rcpRayDir.xy);
        t = min(dist.x, dist.y);
    }

    float3 rayPos;

    int  mipLevel  = 0;
         iterCount = 0;
    bool hit       = false;
    bool miss      = false;
    bool belowMip0 = false; // This value is set prior to entering the cell

    while (!(hit || miss) && (t <= tMax) && (iterCount < _MaxRaySteps))
    {
        rayPos = rayOrigin + t * rayDir;

        // Ray position often ends up on the edge. To determine (and look up) the right cell,
        // we need to bias the position by a small epsilon in the direction of the ray.
        float2 sgnEdgeDist = round(rayPos.xy) - rayPos.xy;
        float2 satEdgeDist = clamp(raySign.xy * sgnEdgeDist + SSR_TRACE_EPS, 0, SSR_TRACE_EPS);
        rayPos.xy += raySign.xy * satEdgeDist;

        int2 mipCoord  = (int2)rayPos.xy >> mipLevel;
        int2 mipOffset = int2(_DepthPyramidMipLevelOffsets[mipLevel].xy);
        // Bounds define 4 faces of a cube:
        // 2 walls in front of the ray, and a floor and a base below it.
        float4 bounds;

        bounds.xy = (mipCoord + rayStep) << mipLevel;
        bounds.z = LOAD_TEXTURE2D_X_LOD(_DepthPyramid, int2(mipOffset + mipCoord), 0).r;

        // We define the depth of the base as the depth value as:
        // b = DeviceDepth((1 + thickness) * LinearDepth(d))
        // b = ((f - n) * d + n * (1 - (1 + thickness))) / ((f - n) * (1 + thickness))
        // b = ((f - n) * d - n * thickness) / ((f - n) * (1 + thickness))
        // b = d / (1 + thickness) - n / (f - n) * (thickness / (1 + thickness))
        // b = d * k_s + k_b
        bounds.w = bounds.z * GetThicknessScale() + GetThicknessBias();

        float4 dist      = bounds * rcpRayDir.xyzz - (rayOrigin.xyzz * rcpRayDir.xyzz);
        float  distWall  = min(dist.x, dist.y);
        float  distFloor = dist.z;
        float  distBase  = dist.w;

        // Note: 'rayPos' given by 't' can correspond to one of several depth values:
        // - above or exactly on the floor
        // - inside the floor (between the floor and the base)
        // - below the base
        bool belowFloor  = !COMPARE_DEVICE_DEPTH_CLOSER(rayPos.z, bounds.z);
        bool aboveBase   = COMPARE_DEVICE_DEPTH_CLOSEREQUAL(rayPos.z, bounds.w);
        bool insideFloor = belowFloor && aboveBase;
        bool hitFloor    = (t <= distFloor) && (distFloor <= distWall);

        // Game rules:
        // * if the closest intersection is with the wall of the cell, switch to the coarser MIP, and advance the ray.
        // * if the closest intersection is with the heightmap below,  switch to the finer   MIP, and advance the ray.
        // * if the closest intersection is with the heightmap above,  switch to the finer   MIP, and do NOT advance the ray.
        // Victory conditions:
        // * See below. Do NOT reorder the statements!

        miss      = belowMip0 && insideFloor;
        hit       = (mipLevel == 0) && (hitFloor || insideFloor);
        belowMip0 = (mipLevel == 0) && belowFloor;

        // 'distFloor' can be smaller than the current distance 't'.
        // We can also safely ignore 'distBase'.
        // If we hit the floor, it's always safe to jump there.
        // If we are at (mipLevel != 0) and we are below the floor, we should not move.
        t = hitFloor ? distFloor : (((mipLevel != 0) && belowFloor) ? t : distWall);
        rayPos.z = bounds.z; // Retain the depth of the potential intersection

        // Warning: both rays towards the eye, and tracing behind objects has linear
        // rather than logarithmic complexity! This is due to the fact that we only store
        // the maximum value of depth, and not the min-max.
        mipLevel += (hitFloor || belowFloor || rayTowardsEye) ? -1 : 1;
        mipLevel  = clamp(mipLevel, 0, maxMipLevel);

        // mipLevel = 0;

        iterCount++;
    }

    // Treat intersections with the sky as misses.
    miss = miss || ((_ReflectSky == 0) && (rayPos.z == UNITY_RAW_FAR_CLIP_VALUE));
    hit  = hit && !miss;

    rayHitPosNDC = float3(floor(rayPos.xy) / screenSize + (0.5 / screenSize), rayPos.z);

    return hit;
}

float2 SampleMotionVector(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_MotionVectorColorTexture, sampler_MotionVectorColorTexture, uv).xy;
}

float SampleSmoothness(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_SmoothnessTexture, sampler_SmoothnessTexture, uv).a;
}

float4 ComputeSSR(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 positionNDC = input.texcoord;
    float2 positionSS = input.positionCS.xy;
    float deviceDepth = LoadSceneDepth(uint2(positionSS) * _Downsample).r;

    // If the smoothness is below our minimum, don't do any raymarching
    float perceptualSmoothness = SampleSmoothness(positionNDC);
    UNITY_BRANCH if (perceptualSmoothness <= GetMinimumSmoothness())
    {
        // Output the framebuffer color ->
        //   avoids bleeding black/uninitialized texels into reflections when blurring.
        // If the pixel is showing skybox, output 0 alpha ->
        //   avoids bleeding the skybox color into reflections when blurring, which would cause haloing.
        // If the pixel is showing an object, output 1 alpha ->
        //   avoids bleeding 0 alpha into reflections when blurring, which would cause peter-panning.
        float alpha = deviceDepth != UNITY_RAW_FAR_CLIP_VALUE;
        return float4(SAMPLE_TEXTURE2D_X_LOD(_CameraColorTexture, sampler_CameraColorTexture, positionNDC, 0).rgb, alpha);
    }

    // Calculate ray origin and direction in world space
    float3 normalWS = SampleSceneNormals(positionNDC);
    float3 positionWS = ComputeWorldSpacePosition(positionNDC, deviceDepth, _CameraInverseViewProjections[unity_eyeIndex]);
    float3 positionToCamWS = GetWorldSpaceNormalizeViewDir(positionWS);
    float3 rayDirWS = reflect(-positionToCamWS, normalWS);

    // Apply normal bias with the magnitude dependent on the distance from the camera.
    #ifdef _HIZ_TRACE
    float3 camPosWS = GetCurrentViewPosition();
    positionWS = camPosWS + (positionWS - camPosWS) * (1 - 0.001 * rcp(max(dot(normalWS, positionToCamWS), FLT_EPS)));
    deviceDepth = ComputeNormalizedDeviceCoordinatesWithZ(positionWS, _CameraViewProjections[unity_eyeIndex]).z;
    #endif

    // Transform ray origin and direction to view space.
    float3 positionVS = mul(_CameraViews[unity_eyeIndex], float4(positionWS, 1)).xyz;
    float3 rayDirVS = SafeNormalize(mul(_CameraViews[unity_eyeIndex], float4(rayDirWS, 0)).xyz);

    // Calculate ray end position in view space and screen space
    float rayLength = 1;

    #ifndef _HIZ_TRACE
    // Clamp ray length such that the end point is in front of the camera.
    // Not needed for Hi-Z path as there is no end point, only a direction.
    rayLength = rayDirVS.z > 0 ? min(GetMaxRayLength(), -positionVS.z / rayDirVS.z * 0.999) : GetMaxRayLength();
    #endif

    float3 endPosVS = positionVS + rayDirVS * rayLength;
    float3 startPosNDC = float3(positionNDC, deviceDepth);
    float3 endPosNDC = ComputeNormalizedDeviceCoordinatesWithZ(endPosVS, _CameraProjections[unity_eyeIndex]);

    #ifndef _HIZ_TRACE
    // Clamp ray length such that the end point is within the view frustum.
    // Not needed for Hi-Z path as there is no end point, only a direction.
    float3 rayDeltaNDC = endPosNDC - startPosNDC;
    float rayLengthNDC = length(rayDeltaNDC);
    float3 rayDirNDC = rayDeltaNDC * rcp(rayLengthNDC);
    float3 maxDistanceNDC = rayDirNDC >= 0 ? (1 - startPosNDC) / rayDirNDC : -startPosNDC / rayDirNDC;
    endPosNDC = startPosNDC + rayDirNDC * min(rayLengthNDC, min(maxDistanceNDC.x, min(maxDistanceNDC.y, maxDistanceNDC.z)));
    #endif

    float4 screenSizeWithInverse = _BlitTexture_TexelSize;
    float2 endPosSS = endPosNDC.xy * screenSizeWithInverse.zw;

    float3 rayHitPosNDC;
    int iterCount;
    bool hit;
    #ifdef _HIZ_TRACE
    hit = TraceScreenSpaceRayHiZ(positionSS, deviceDepth, endPosSS.xy, endPosNDC.z, screenSizeWithInverse.zw, rayHitPosNDC, iterCount);
    #else
    hit = TraceScreenSpaceRay(positionSS, deviceDepth, endPosSS.xy, endPosNDC.z, screenSizeWithInverse, rayHitPosNDC, iterCount);
    #endif

    UNITY_BRANCH if (hit)
    {
        #ifdef _USE_MOTION_VECTORS
        // Reproject position
        rayHitPosNDC.xy -= SampleMotionVector(rayHitPosNDC.xy);
        // Compensate for jittering
        rayHitPosNDC.xy -= _CameraDeltaJitterOffset.xy;

        const float2 downsampledScreenSize = screenSizeWithInverse.zw * _Downsample;
        const int2 topLeftReprojectedPixelPos = int2(rayHitPosNDC.xy * downsampledScreenSize - 0.5);

        // Manually apply bilinear filter at the reprojected position
        float3 hitColor = 0;
        float weightSum = 0;
        for (int dx = 0; dx < 2; dx++)
        {
            for (int dy = 0; dy < 2; dy++)
            {
                const int2 samplePixelPos = topLeftReprojectedPixelPos + int2(dx, dy);
                const float2 samplePixelCenter = float2(samplePixelPos) + 0.5;

                // Reject samples that are off-screen
                const bool isInView = all(0 <= samplePixelPos && samplePixelPos < downsampledScreenSize);
                if (!isInView)
                    continue;

                // Reject samples that land on the skybox (unless we are intentionally reflecting skybox)
                const float sampleDeviceDepth = LOAD_TEXTURE2D_X_LOD(_LastFrameCameraDepthTexture, samplePixelPos, 0).x;
                if (sampleDeviceDepth == UNITY_RAW_FAR_CLIP_VALUE && rayHitPosNDC.z != UNITY_RAW_FAR_CLIP_VALUE)
                    continue;

                // Calculate bilinear weight and accumulate
                const float weight =
                    (1.0f - abs(rayHitPosNDC.x * downsampledScreenSize.x - samplePixelCenter.x)) *
                    (1.0f - abs(rayHitPosNDC.y * downsampledScreenSize.y - samplePixelCenter.y));
                const float3 sampleColor = LOAD_TEXTURE2D_X_LOD(_CameraColorTexture, samplePixelPos, 0).rgb;
                hitColor += weight * sampleColor;
                weightSum += weight;
            }
        }
        // If we got no valid samples, just return the existing framebuffer color. The sample is invalid if the total
        // sum of weights is 0 but due to precision issues if the total weight is extremely low (which can lead
        // to wrongly high color values, once divided by it) we check if the total weight is under an epsilon
        // value instead.
        const float k_MinimumWeight = 1e-3;
        if (weightSum < k_MinimumWeight)
            return float4(SAMPLE_TEXTURE2D_X_LOD(_CameraColorTexture, sampler_CameraColorTexture, positionNDC, 0).rgb, 0);
        else
            hitColor /= weightSum;
        #else
        float3 hitColor = SAMPLE_TEXTURE2D_X_LOD(_CameraColorTexture, sampler_CameraColorTexture, rayHitPosNDC.xy, 0).rgb;
        #endif

        // Fade rays pointing toward camera.
        float viewDotRay = dot(SafeNormalize(positionVS), rayDirVS);
        float viewConeDot = GetViewConeDot();
        const float normalFadeFactor = 0.1;
        float fade = smoothstep(viewConeDot, viewConeDot + normalFadeFactor, viewDotRay);

        // Fade rays hitting near the max distance, if we aren't reflecting the sky.
        #ifndef _HIZ_TRACE
        if (!_ReflectSky)
        {
            float4 rayHitPosCS = ComputeClipSpacePosition(rayHitPosNDC.xy, rayHitPosNDC.z);
            float4 rayHitPosVS = mul(_CameraInverseProjections[unity_eyeIndex], rayHitPosCS);
            rayHitPosVS.xyz /= rayHitPosVS.w;
            fade *= smoothstep(GetMaxRayLength(), GetRayLengthFadeStart(), distance(positionVS, rayHitPosVS.xyz));
        }
        #endif

        // Fade rays reaching near the edge of the screen, to avoid a harsh discontinuity.
        float2 edgeDist = smoothstep(0, GetScreenEdgeFade().x, rayHitPosNDC.xy) * smoothstep(1, GetScreenEdgeFade().y, rayHitPosNDC.xy);
        fade *= edgeDist.x * edgeDist.y;

        return float4(hitColor, fade);
    }

    // Even if we hit nothing, we output the framebuffer color (but with 0 weight).
    // This provides the blur/upscale kernel with data needed to avoid blurring black into
    // the reflections, which leads to ugly borders.
    return float4(SAMPLE_TEXTURE2D_X_LOD(_CameraColorTexture, sampler_CameraColorTexture, positionNDC, 0).rgb, 0);
}

// ------------------------------------------------------------------
// Compositing for AfterOpaque mode
// ------------------------------------------------------------------
float4 CompositeSSRAfterOpaque(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

    // Reconstruct world position
    float2 positionNDC = uv;
    float deviceDepth = SampleSceneDepth(uv).r;
    float3 positionWS = ComputeWorldSpacePosition(positionNDC, deviceDepth, _CameraInverseViewProjections[unity_eyeIndex]);

    float perceptualSmoothness = SAMPLE_TEXTURE2D_X(_SmoothnessTexture, sampler_SmoothnessTexture, uv).a;
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(perceptualSmoothness);

    // Map roughness to mip level to get blur.
    float mipLevel = GetSSRMipLevelFromPerceptualRoughness(positionWS, perceptualRoughness);
    float4 reflColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_TrilinearClamp, uv, mipLevel);

    // Fade out reflections with smoothness.
    // Not physically correct, but we can't do much better without more data.
    reflColor.a *= perceptualSmoothness;

    return reflColor;
}

// ------------------------------------------------------------------
// Bilateral Blur
// ------------------------------------------------------------------
#define SAMPLE_BASEMAP(uv) float4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv)));

float CompareNormal(float3 d1, float3 d2)
{
    return smoothstep(0.8, 1.0, dot(d1, d2));
}

// Geometry-aware separable bilateral filter
float4 BilateralBlur(const float2 uv, const float2 delta) : SV_Target
{
    float4 p0 =  SAMPLE_BASEMAP(uv);
    float4 p1a = SAMPLE_BASEMAP(uv - delta * 1.3846153846);
    float4 p1b = SAMPLE_BASEMAP(uv + delta * 1.3846153846);
    float4 p2a = SAMPLE_BASEMAP(uv - delta * 3.2307692308);
    float4 p2b = SAMPLE_BASEMAP(uv + delta * 3.2307692308);

    float3 n0 =  SampleSceneNormals(uv);
    float3 n1a = SampleSceneNormals(uv - delta * 1.3846153846);
    float3 n1b = SampleSceneNormals(uv + delta * 1.3846153846);
    float3 n2a = SampleSceneNormals(uv - delta * 3.2307692308);
    float3 n2b = SampleSceneNormals(uv + delta * 3.2307692308);

    float w0  = float(0.2270270270);
    float w1a = CompareNormal(n0, n1a) * float(0.3162162162);
    float w1b = CompareNormal(n0, n1b) * float(0.3162162162);
    float w2a = CompareNormal(n0, n2a) * float(0.0702702703);
    float w2b = CompareNormal(n0, n2b) * float(0.0702702703);

    float4 s = 0.0;
    s += p0 * w0;
    s += p1a * w1a;
    s += p1b * w1b;
    s += p2a * w2a;
    s += p2b * w2b;
    s *= rcp(w0 + w1a + w1b + w2a + w2b);

    return s;
}

// Geometry-aware bilateral filter (single pass/small kernel)
float4 BilateralBlurSinglePass(const float2 uv, const float2 delta)
{
    float4 p0 = SAMPLE_BASEMAP(uv                            );
    float4 p1 = SAMPLE_BASEMAP(uv + float2(-delta.x, -delta.y));
    float4 p2 = SAMPLE_BASEMAP(uv + float2( delta.x, -delta.y));
    float4 p3 = SAMPLE_BASEMAP(uv + float2(-delta.x,  delta.y));
    float4 p4 = SAMPLE_BASEMAP(uv + float2( delta.x,  delta.y));

    float3 n0 =  SampleSceneNormals(uv);
    float3 n1a = SampleSceneNormals(uv - delta * 1.3846153846);
    float3 n1b = SampleSceneNormals(uv + delta * 1.3846153846);
    float3 n2a = SampleSceneNormals(uv - delta * 3.2307692308);
    float3 n2b = SampleSceneNormals(uv + delta * 3.2307692308);

    float w0 = 1.0;
    float w1 = CompareNormal(n0, n1a);
    float w2 = CompareNormal(n0, n1b);
    float w3 = CompareNormal(n0, n2a);
    float w4 = CompareNormal(n0, n2b);

    float4 s = 0.0;
    s += p0 * w0;
    s += p1 * w1;
    s += p2 * w2;
    s += p3 * w3;
    s += p4 * w4;

    return s *= rcp(w0 + w1 + w2 + w3 + w4);
}

float4 HorizontalBilateralBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = float2(_SourceSize.z * _Downsample, 0.0);
    return BilateralBlur(uv, delta);
}

float4 VerticalBilateralBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = float2(0.0, _SourceSize.w * _Downsample);
    return BilateralBlur(uv, delta);
}

float4 FinalBilateralBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = _SourceSize.zw * _Downsample;
    return BilateralBlurSinglePass(uv, delta );
}

// ------------------------------------------------------------------
// Gaussian Blur
// ------------------------------------------------------------------
float4 GaussianBlur(float2 uv, float2 pixelOffset)
{
    float4 colOut = 0;

    // Kernel width 7 x 7
    const int stepCount = 2;

    const half gWeights[stepCount] ={
        0.44908,
        0.05092
     };
    const half gOffsets[stepCount] ={
        0.53805,
        2.06278
     };

    UNITY_UNROLL
    for( int i = 0; i < stepCount; i++ )
    {
        float2 texCoordOffset = gOffsets[i] * pixelOffset;
        float4 p1 = SAMPLE_BASEMAP(uv + texCoordOffset);
        float4 p2 = SAMPLE_BASEMAP(uv - texCoordOffset);
        float4 col = p1 + p2;
        colOut += gWeights[i] * col;
    }

    return colOut;
}

float4 HorizontalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
    float2 delta = float2(_SourceSize.z * _Downsample, 0.0);
    return GaussianBlur(uv, delta);
}

float4 VerticalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
    float2 delta = float2(0.0, _SourceSize.w * _Downsample);
    return GaussianBlur(uv, delta);
}

// ------------------------------------------------------------------
// Kawase Blur
// ------------------------------------------------------------------
float4 KawaseBlurFilter(float2 texCoord, float2 pixelSize)
{
    float2 texCoordSample;
    float2 halfPixelSize = pixelSize * 0.5;
    float2 dUV = halfPixelSize.xy;

    float4 cOut;

    // Sample top left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;
    cOut = SAMPLE_BASEMAP(texCoordSample);

    // Sample top right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;
    cOut += SAMPLE_BASEMAP(texCoordSample);

    // Sample bottom right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;
    cOut += SAMPLE_BASEMAP(texCoordSample);

    // Sample bottom left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;
    cOut += SAMPLE_BASEMAP(texCoordSample);

    // Average
    cOut *= 0.25;

    return cOut;
}

float4 KawaseBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
    float2 texelSize = _SourceSize.zw * _Downsample;
    return KawaseBlurFilter(uv, texelSize);
}

#endif //UNIVERSAL_SSR_INCLUDED
