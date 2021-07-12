#ifndef CustomDensityVolumeBase_Included
    #include "CustomDensityVolumeBase.hlsl"
#endif

[numthreads(GROUP_SIZE_1D, GROUP_SIZE_1D, 1)]
void CSMain(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    uint2 groupId : SV_GroupID,
    uint2 groupThreadId : SV_GroupThreadID
)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);

    uint2 groupOffset = groupId * GROUP_SIZE_1D;
    uint2 voxelCoord = groupOffset + groupThreadId;
#ifdef VL_PRESET_OPTIMAL
    // The entire thread group is within the same light tile.
    uint2 tileCoord = groupOffset * VBUFFER_VOXEL_SIZE / TILE_SIZE_BIG_TILE;
#else
    // No compile-time optimizations, no scalarization.
    // If _VBufferVoxelSize is not a power of 2 or > TILE_SIZE_BIG_TILE, a voxel may straddle
    // a tile boundary. This means different voxel subsamples may belong to different tiles.
    // We accept this error, and simply use the coordinates of the center of the voxel.
    uint2 tileCoord = (uint2)((voxelCoord + 0.5) * _VBufferVoxelSize / TILE_SIZE_BIG_TILE);
#endif
    uint  tileIndex = tileCoord.x + _NumTileBigTileX * tileCoord.y;

    // Reminder: our voxels are sphere-capped right frustums (truncated right pyramids).
    // The curvature of the front and back faces is quite gentle, so we can use
    // the right frustum approximation (thus the front and the back faces are squares).
    // Note, that since we still rely on the perspective camera model, pixels at the center
    // of the screen correspond to larger solid angles than those at the edges.
    // Basically, sizes of front and back faces depend on the XY coordinate.
    // https://www.desmos.com/calculator/i3rkesvidk

    float3 F = GetViewForwardDir();
    float3 U = GetViewUpDir();

    float2 centerCoord = voxelCoord + float2(0.5, 0.5);

    // Compute a ray direction s.t. ViewSpace(rayDirWS).z = 1.
    float3 rayDirWS = mul(-float4(centerCoord, 1, 1), _VBufferCoordToViewDirWS[unity_StereoEyeIndex]).xyz;
    float3 rightDirWS = cross(rayDirWS, U);
    float  rcpLenRayDir = rsqrt(dot(rayDirWS, rayDirWS));
    float  rcpLenRightDir = rsqrt(dot(rightDirWS, rightDirWS));

    JitteredRay ray;
    ray.originWS = GetCurrentViewPosition();
    ray.originWS = _WorldSpaceCameraPos; // fixes the camera position ?
    ray.centerDirWS = rayDirWS * rcpLenRayDir; // Normalize

    float FdotD = dot(F, ray.centerDirWS);
    float unitDistFaceSize = _VBufferUnitDepthTexelSpacing * FdotD * rcpLenRayDir;

    ray.xDirDerivWS = rightDirWS * (rcpLenRightDir * unitDistFaceSize); // Normalize & rescale
    ray.yDirDerivWS = cross(ray.xDirDerivWS, ray.centerDirWS); // Will have the length of 'unitDistFaceSize' by construction
    ray.jitterDirWS = ray.centerDirWS; // TODO

    PositionInputs posInput = GetPositionInput(voxelCoord, _VBufferViewportSize.zw, tileCoord);

    ApplyCameraRelativeXR(ray.originWS);

    uint volumeCount, volumeStart;

#ifdef USE_BIG_TILE_LIGHTLIST
    // Offset for stereo rendering
    tileIndex += unity_StereoEyeIndex * _NumTileBigTileX * _NumTileBigTileY;

    // The "big tile" list contains the number of objects contained within the tile followed by the
    // list of object indices. Note that while objects are already sorted by type, we don't know the
    // number of each type of objects (e.g. lights), so we should remember to break out of the loop.
    volumeCount = g_vBigTileLightList[MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE * tileIndex];
    volumeStart = MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE * tileIndex + 1;

    // For now, iterate through all the objects to determine the correct range.
    // TODO: precompute this, of course.
    {
        uint offset = 0;

        for (; offset < volumeCount; offset++)
        {
            uint objectIndex = FetchIndex(volumeStart, offset);

            if (objectIndex >= _DensityVolumeIndexShift)
            {
                // We have found the first density volume.
                break;
            }
        }

        volumeStart += offset;
        volumeCount -= offset;
    }

#else  // USE_BIG_TILE_LIGHTLIST

    volumeCount = 0;
    volumeStart = 0;

#endif // USE_BIG_TILE_LIGHTLIST

    float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);
    //t0 += 0;
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices

    for (uint slice = 0; slice < _VBufferSliceCount; slice++)
    {
        uint3 voxelCoord = uint3(posInput.positionSS, slice + _VBufferSliceCount * unity_StereoEyeIndex);

        float e1 = slice * de + de; // (slice + 1) / sliceCount
        float t1 = DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
        float dt = t1 - t0;
        float t = t0 + 0.5 * dt;
        t *= 2; // Heck, I don't know why, but this seems to fix the fog ditance !

        float3 voxelCenterWS = ray.originWS + t * ray.centerDirWS;

        float3 voxelCenterVolSpace = mul(InvVolumeMatrix, float4(voxelCenterWS, 1)).xyz;

        float4 volumeDensity = DensityInfo(voxelCenterWS);

        //_VBufferDensity[voxelCoord] = volumeDensity;

        float3 mask = abs(voxelCenterVolSpace) * 2.0;
        mask = saturate(mask);

        float l = max(mask.x, max(mask.y, mask.z)) * 2.5;
        l = 1.0-saturate( l - 1.5 );

        _VBufferDensity[voxelCoord] = lerp(_VBufferDensity[voxelCoord], volumeDensity, min(volumeDensity.a, l));
        //_VBufferDensity[voxelCoord] = lerp(_VBufferDensity[voxelCoord], volumeDensity, l);
    }
}
