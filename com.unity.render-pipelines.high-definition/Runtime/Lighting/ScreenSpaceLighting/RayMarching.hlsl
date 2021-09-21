// Epslon value used for the computation
#define RAY_TRACE_EPS 0.00024414

// TODO: It is not really possible to share all of this with SSR for couple reason, but it probably possible share a part of it
bool RayMarch(float3 positionWS, float3 sampleDir, float3 normalWS, float2 positionSS, float deviceDepth, bool killRay, out float3 rayPos)
{
    // Initialize ray pos to invalid value
    rayPos = float3(-1.0, -1.0, -1.0);

    // Due to a warning on Vulkan and Metal if returning early, this is the only way we found to avoid it.
    bool status = false;

    // We start tracing from the center of the current pixel, and do so up to the far plane.
    float3 rayOrigin = float3(positionSS + 0.5, deviceDepth);

    float3 reflPosWS  = positionWS + sampleDir;
    float3 reflPosNDC = ComputeNormalizedDeviceCoordinatesWithZ(reflPosWS, UNITY_MATRIX_VP); // Jittered
    float3 reflPosSS  = float3(reflPosNDC.xy * _ScreenSize.xy, reflPosNDC.z);
    float3 rayDir     = reflPosSS - rayOrigin;
    float3 rcpRayDir  = rcp(rayDir);
    int2   rayStep    = int2(rcpRayDir.x >= 0 ? 1 : 0,
                             rcpRayDir.y >= 0 ? 1 : 0);
    float3 raySign  = float3(rcpRayDir.x >= 0 ? 1 : -1,
                             rcpRayDir.y >= 0 ? 1 : -1,
                             rcpRayDir.z >= 0 ? 1 : -1);
    bool   rayTowardsEye  =  rcpRayDir.z >= 0;

    killRay = killRay || (reflPosSS.z <= 0);

    // If the point is behind the camera or the ray is invalid, this ray should not be cast
    if (!killRay)
    {
        // Extend and clip the end point to the frustum.
        float tMax;
        {
            // Shrink the frustum by half a texel for efficiency reasons.
            const float halfTexel = 0.5;

            float3 bounds;
            bounds.x = (rcpRayDir.x >= 0) ? _ScreenSize.x - halfTexel : halfTexel;
            bounds.y = (rcpRayDir.y >= 0) ? _ScreenSize.y - halfTexel : halfTexel;
            // If we do not want to intersect the skybox, it is more efficient to not trace too far.
            float maxDepth = (_RayMarchingReflectsSky != 0) ? -0.00000024 : 0.00000024; // 2^-22
            bounds.z = (rcpRayDir.z >= 0) ? 1 : maxDepth;

            float3 dist = bounds * rcpRayDir - (rayOrigin * rcpRayDir);
            tMax = Min3(dist.x, dist.y, dist.z);
        }

        // Start ray marching from the next texel to avoid self-intersections.
        float t;
        {
            // 'rayOrigin' is the exact texel center.
            float2 dist = abs(0.5 * rcpRayDir.xy);
            t = min(dist.x, dist.y);
        }

        int  mipLevel  = 0;
        int2 mipOffset = _DepthPyramidMipLevelOffsets[mipLevel];
        int  iterCount = 0;
        bool hit       = false;
        bool miss      = false;
        bool belowMip0 = false; // This value is set prior to entering the cell

        while (!(hit || miss) && (t <= tMax) && (iterCount < _RayMarchingSteps))
        {
            rayPos = rayOrigin + t * rayDir;

            // Ray position often ends up on the edge. To determine (and look up) the right cell,
            // we need to bias the position by a small epsilon in the direction of the ray.
            float2 sgnEdgeDist = round(rayPos.xy) - rayPos.xy;
            float2 satEdgeDist = clamp(raySign.xy * sgnEdgeDist + RAY_TRACE_EPS, 0, RAY_TRACE_EPS);
            rayPos.xy += raySign.xy * satEdgeDist;

            int2 mipCoord  = (int2)rayPos.xy >> mipLevel;
            // Bounds define 4 faces of a cube:
            // 2 walls in front of the ray, and a floor and a base below it.
            float4 bounds;

            bounds.z  = LOAD_TEXTURE2D_X(_DepthTexture, mipOffset + mipCoord).r;
            bounds.xy = (mipCoord + rayStep) << mipLevel;

            // We define the depth of the base as the depth value as:
            // b = DeviceDepth((1 + thickness) * LinearDepth(d))
            // b = ((f - n) * d + n * (1 - (1 + thickness))) / ((f - n) * (1 + thickness))
            // b = ((f - n) * d - n * thickness) / ((f - n) * (1 + thickness))
            // b = d / (1 + thickness) - n / (f - n) * (thickness / (1 + thickness))
            // b = d * k_s + k_b
            bounds.w = bounds.z * _RayMarchingThicknessScale + _RayMarchingThicknessBias;

            float4 dist      = bounds * rcpRayDir.xyzz - (rayOrigin.xyzz * rcpRayDir.xyzz);
            float  distWall  = min(dist.x, dist.y);
            float  distFloor = dist.z;
            float  distBase  = dist.w;

            // Note: 'rayPos' given by 't' can correspond to one of several depth values:
            // - above or exactly on the floor
            // - inside the floor (between the floor and the base)
            // - below the base
            bool belowFloor  = rayPos.z  < bounds.z;
            bool aboveBase   = rayPos.z >= bounds.w;

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
            mipLevel  = clamp(mipLevel, 0, 6);
            mipOffset = _DepthPyramidMipLevelOffsets[mipLevel];
            // mipLevel = 0;

            iterCount++;
        }

        // Treat intersections with the sky as misses.
        miss = miss || ((_RayMarchingReflectsSky == 0) && (rayPos.z == 0));
        status = hit && !miss;
    }
    return status;
}
