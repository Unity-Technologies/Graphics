#ifndef CustomDensityVolumeBase_Included
    #include "CustomDensityVolumeBase.hlsl"
#endif

[numthreads(GROUP_SIZE_1D, GROUP_SIZE_1D, 1)]
void VoxelizeComputeLocalFog(
    uint3 dispatchThreadId : SV_DispatchThreadID,
    uint2 groupId : SV_GroupID,
    uint2 groupThreadId : SV_GroupThreadID
)
{
    PositionInputs posInput;
    uint tileIndex;
    JitteredRay ray;

    PrepareVoxelization(dispatchThreadId, groupId, groupThreadId, posInput, tileIndex, ray);

    float t0 = DecodeLogarithmicDepthGeneralized(0, _VBufferDistanceDecodingParams);
    float de = _VBufferRcpSliceCount; // Log-encoded distance between slices

    for (uint slice = 0; slice < _VBufferSliceCount; slice++)
    {
        float t1, dt, t;
        uint3 voxelCoord;
        float3 voxelCenterWS = ComputeVoxelCenterWS(posInput, ray, _VBufferSliceCount, slice, t0, de, voxelCoord, t1, dt, t);

        const OrientedBBox obb = _VolumeBounds[visibleVolumeIndex];

        const float3x3 obbFrame = float3x3(obb.right, obb.up, cross(obb.right, obb.up));
        const float3   obbExtents = float3(obb.extentX, obb.extentY, obb.extentZ);

        // Express the voxel center in the local coordinate system of the box.
        const float3 voxelCenterBS = mul(voxelCenterWS - obb.center, transpose(obbFrame));
        const float3 voxelCenterCS = (voxelCenterBS * rcp(obbExtents));

        // We must clamp here, otherwise, with soft voxelization enabled,
        // the center of the voxel can be slightly outside the box.
        float3 voxelCenterNDC = saturate(voxelCenterCS * 0.5 + 0.5);

        // Due to clamping above, 't' may not exactly correspond to the distance
        // to the sample point. We ignore it for performance and simplicity.
        float dist = t;

        bool overlap = Max3(abs(voxelCenterCS.x), abs(voxelCenterCS.y), abs(voxelCenterCS.z)) <= 1;

        float overlapFraction = overlap ? 1 : 0;

        if (overlapFraction > 0)
        {
            float4 volumeDensity = VolumetricFogFunction(voxelCenterWS, voxelCenterCS);

            overlapFraction *= ComputeFadeFactor(voxelCenterNDC, dist,
                                                _VolumeData[visibleVolumeIndex].rcpPosFaceFade,
                                                _VolumeData[visibleVolumeIndex].rcpNegFaceFade,
                                                _VolumeData[visibleVolumeIndex].invertFade,
                                                _VolumeData[visibleVolumeIndex].rcpDistFadeLen,
                                                _VolumeData[visibleVolumeIndex].endTimesRcpDistFadeLen,
                                                _VolumeData[visibleVolumeIndex].falloffMode);

            float3 volumeAlbedo = _VolumeData[visibleVolumeIndex].scattering / _VolumeData[visibleVolumeIndex].extinction;

            // Multiply with settings albedo
            volumeDensity.rgb *= volumeAlbedo;

            // Apply settings extinction
            volumeDensity.a *= _VolumeData[visibleVolumeIndex].extinction;

#ifdef CUSTOM_BLEND
            float4 fogAlbedoExtinction = _VBufferDensity[voxelCoord];

            // Transform scattering to albedo
            fogAlbedoExtinction.rgb /= fogAlbedoExtinction.a;

            volumeDensity.a *= _VolumeData[visibleVolumeIndex].extinction;

            fogAlbedoExtinction = lerp(fogAlbedoExtinction, CustomBlend(fogAlbedoExtinction, volumeDensity), overlapFraction);

            // Transform back to scattering
            fogAlbedoExtinction.rgb *= fogAlbedoExtinction.a;

            _VBufferDensity[voxelCoord] = fogAlbedoExtinction;
#else

            // Transform albedo to scattering
            volumeDensity.rgb *= volumeDensity.a;

            _VBufferDensity[voxelCoord] = lerp(_VBufferDensity[voxelCoord], volumeDensity, min(volumeDensity.a , overlapFraction));
#endif
        }

        t0 = t1;
    }
}
