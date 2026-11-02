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
float4 _SmoothnessAndStrengthAndClamp;
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
    return _SmoothnessAndStrengthAndClamp.x;
}

float GetStrength()
{
    return _SmoothnessAndStrengthAndClamp.z;
}

float GetClampValue()
{
    return _SmoothnessAndStrengthAndClamp.w * GetCurrentExposureMultiplier();
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
    float rawSceneDepth = startZ;

    for (iterCount = 0; iterCount < rayScreenDelta; iterCount++)
    {
        rayPosSS += rayStep;

        // We went offscreen, so stop
        if (rayPosSS.x < 0 || rayPosSS.x > screenSizeWithInverse.z || rayPosSS.y < 0 || rayPosSS.y > screenSizeWithInverse.w)
            return false;

        // How far along the ray are we in [0; 1]?
        rayHitT = lerp((rayPosSS.y - startPosSS.y) / rayScreenDeltaY, (rayPosSS.x - startPosSS.x) / rayScreenDeltaX, useDeltaX);

        // Get current depth of scene at the ray position.
        rawSceneDepth = LoadSceneDepth(rayPosSS.xy * _Downsample);

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
            float rawSceneDepthFine = LoadSceneDepth(candidateHitPosSS * _Downsample);

            bool aboveBase = !COMPARE_DEVICE_DEPTH_CLOSER(rayDepth, rawSceneDepthFine);
            bool belowFloor = COMPARE_DEVICE_DEPTH_CLOSER(rayDepth, rawSceneDepthFine * GetThicknessScale() + GetThicknessBias());
            [branch]
            if (aboveBase && belowFloor)
            {
                hitFine = COMPARE_DEVICE_DEPTH_CLOSER(rayDepth, rawSceneDepthFine * GetThicknessScaleFine() + GetThicknessBiasFine());

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

    // If we have a hit, we are done.
    if (hitCoarse)
        return true;

    // If we hit no geometry, and the depth is at the far plane, we have hit the skybox.
    // Treat it as a valid hit if _ReflectSky is enabled, otherwise return false.
    UNITY_BRANCH
    if (_ReflectSky)
    {
        rayHitPosNDC.z = UNITY_RAW_FAR_CLIP_VALUE;
        return rawSceneDepth == UNITY_RAW_FAR_CLIP_VALUE;
    }
    return false;
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

float3 ClampColor(float3 color)
{
    float maxChannel = Max3(color.r, color.g, color.b);
    float scale = maxChannel > GetClampValue() ? GetClampValue() / maxChannel : 1;
    return scale * color;
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
        return float4(ClampColor(SAMPLE_TEXTURE2D_X_LOD(_CameraColorTexture, sampler_CameraColorTexture, positionNDC, 0).rgb), alpha);
    }

    // Calculate ray origin and direction in world space
    float3 normalWS = SampleSceneNormals(positionNDC);
    float3 positionWS = ComputeWorldSpacePosition(positionNDC, deviceDepth, _CameraInverseViewProjections[unity_eyeIndex]);
    float3 positionToCamWS = GetWorldSpaceNormalizeViewDir(positionWS);
    float3 rayDirWS = reflect(-positionToCamWS, normalWS);

    // Apply normal bias with the magnitude dependent on the distance from the camera.
    float3 camPosWS = GetCurrentViewPosition();
    positionWS = camPosWS + (positionWS - camPosWS) * (1 - 0.001 * _Downsample * rcp(max(dot(normalWS, positionToCamWS), FLT_EPS)));
    deviceDepth = ComputeNormalizedDeviceCoordinatesWithZ(positionWS, _CameraViewProjections[unity_eyeIndex]).z;

    // Transform ray origin and direction to view space.
    float3 positionVS = mul(_CameraViews[unity_eyeIndex], float4(positionWS, 1)).xyz;
    float3 rayDirVS = SafeNormalize(mul(_CameraViews[unity_eyeIndex], float4(rayDirWS, 0)).xyz);

    // Clamp ray length such that the end point is in front of the camera.
    float maxRayLength = 1;
    #ifndef _HIZ_TRACE
    maxRayLength = GetMaxRayLength();
    #endif
    float rayLength = rayDirVS.z > 0 ? min(maxRayLength, -positionVS.z / rayDirVS.z * 0.999) : maxRayLength;

    // Calculate ray end position in view space and screen space
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
        const bool hitIsSky = rayHitPosNDC.z == UNITY_RAW_FAR_CLIP_VALUE;

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

                // We are either reflecting the sky or not, never mix sky and non-sky hits.
                const float sampleDeviceDepth = LOAD_TEXTURE2D_X_LOD(_LastFrameCameraDepthTexture, samplePixelPos, 0).x;
                const bool sampleIsSky = sampleDeviceDepth == UNITY_RAW_FAR_CLIP_VALUE;
                if (sampleIsSky != hitIsSky)
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
            return float4(ClampColor(SAMPLE_TEXTURE2D_X_LOD(_CameraColorTexture, sampler_CameraColorTexture, positionNDC, 0).rgb), 0);
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
        if (!hitIsSky)
        {
            float4 rayHitPosCS = ComputeClipSpacePosition(rayHitPosNDC.xy, rayHitPosNDC.z);
            float4 rayHitPosVS = mul(_CameraInverseProjections[unity_eyeIndex], rayHitPosCS);
            rayHitPosVS.xyz /= rayHitPosVS.w;
            fade *= smoothstep(maxRayLength, GetRayLengthFadeStart(), distance(positionVS, rayHitPosVS.xyz));
        }
        #endif

        // Fade rays reaching near the edge of the screen, to avoid a harsh discontinuity.
        float2 edgeDist = smoothstep(0, GetScreenEdgeFade().x, rayHitPosNDC.xy) * smoothstep(1, GetScreenEdgeFade().y, rayHitPosNDC.xy);
        fade *= edgeDist.x * edgeDist.y;

        // Scale the SSR contribution by the user-defined strength.
        fade *= GetStrength();

        return float4(ClampColor(hitColor), fade);
    }

    // Even if we hit nothing, we output the framebuffer color (but with 0 weight).
    // This provides the blur/upscale kernel with data needed to avoid blurring black into
    // the reflections, which leads to ugly borders.
    return float4(ClampColor(SAMPLE_TEXTURE2D_X_LOD(_CameraColorTexture, sampler_CameraColorTexture, positionNDC, 0).rgb), 0);
}

// ------------------------------------------------------------------
// Compositing for AfterOpaque mode
// ------------------------------------------------------------------
float4 CompositeSSRAfterOpaque(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;

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
    float fadeStart = _ScreenSpaceReflectionParam.y;
    float fadeEnd = _ScreenSpaceReflectionParam.z;
    reflColor.a *= smoothstep(fadeStart, fadeEnd, perceptualSmoothness);

    return reflColor;
}

// ------------------------------------------------------------------
// Upscaling
// ------------------------------------------------------------------
float4 BilinearUpscale(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
}

float CompareNormal(float3 d1, float3 d2)
{
    return smoothstep(0.8, 1.0, dot(d1, d2));
}

float4 BilateralUpscale(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float4 texelSize = _BlitTexture_TexelSize;
    float2 uv = input.texcoord;
    // Position of texel, [0; LowResTextureSize]
    float2 texelPos = uv * texelSize.zw - 0.5;
    // Position of top-left texel center in bilinear neighborhood, [0; LowResTextureSize]
    float2 topLeftTexelPos = floor(texelPos) + 0.5;
    // Offset in texel, [0; 1]
    float2 offsetInTexel = frac(texelPos);

    float3 normalRef = SampleSceneNormals(uv);

    float4 result = 0;
    float weightSum = 0;
    for (int dx = 0; dx < 2; dx++)
    {
        for (int dy = 0; dy < 2; dy++)
        {
            // Reject off-screen samples.
            float2 sampleTexelPos = topLeftTexelPos + float2(dx, dy);
            const bool isInView = all(0 <= sampleTexelPos && sampleTexelPos < texelSize.zw);
            if (!isInView)
                continue;

            // Sample color and normal from bilinear neighborhood.
            float2 sampleUvPos = sampleTexelPos * texelSize.xy;
            float4 sampleColor = LOAD_TEXTURE2D_X(_BlitTexture, sampleTexelPos);
            float3 sampleNormal = SampleSceneNormals(sampleUvPos);

            // Bilinear weight.
            float weight =
                (dx ? offsetInTexel.x : 1.0 - offsetInTexel.x) *
                (dy ? offsetInTexel.y : 1.0 - offsetInTexel.y);

            // Bilateral weight. Clamped to prevent division by 0, and also to ensure we get a
            // reasonable value when none of the samples pass the normal comparison condition.
            const float k_MinWeight = 0.01;
            weight *= max(k_MinWeight, CompareNormal(normalRef, sampleNormal));

            result += sampleColor * weight;
            weightSum += weight;
        }
    }
    return result * rcp(weightSum);
}

// ------------------------------------------------------------------
// Temporal Filtering
// ------------------------------------------------------------------
TEXTURE2D_X(_ReflectionHistoryTexture);
float _BaseBlendFactor;

float4 SSRTemporalFiltering(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // Reproject pixel position, get current and last frame color
    int2 pixelCoord = input.positionCS.xy;
    float2 reprojectedUv = input.texcoord - SampleMotionVector(input.texcoord);
    float4 current = LOAD_TEXTURE2D_X(_BlitTexture, pixelCoord);
    float4 history = SAMPLE_TEXTURE2D_X(_ReflectionHistoryTexture, sampler_LinearClamp, reprojectedUv);

    // Take 4 taps around center in a cross pattern
    float4 nb0 = LOAD_TEXTURE2D_X(_BlitTexture, pixelCoord + int2( 0,  1));
    float4 nb1 = LOAD_TEXTURE2D_X(_BlitTexture, pixelCoord + int2( 0, -1));
    float4 nb2 = LOAD_TEXTURE2D_X(_BlitTexture, pixelCoord + int2( 1,  0));
    float4 nb3 = LOAD_TEXTURE2D_X(_BlitTexture, pixelCoord + int2(-1,  0));

    // Neighborhood clamping
    float4 boxMin = min(min(min(min(current, nb0), nb1), nb2), nb3);
    float4 boxMax = max(max(max(max(current, nb0), nb1), nb2), nb3);
    history = clamp(history, boxMin, boxMax);

    // If outside screen, ignore history
    float blendFactor = _BaseBlendFactor;
    if (any(reprojectedUv < 0) || any(reprojectedUv > 1))
        blendFactor = 0.0;

    return lerp(current, history, blendFactor);
}

#endif //UNIVERSAL_SSR_INCLUDED
