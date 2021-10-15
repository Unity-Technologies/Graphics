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

        const OrientedBBox obb = _VolumeBounds[0];

        const float3x3 obbFrame = float3x3(obb.right, obb.up, cross(obb.right, obb.up));
        const float3   obbExtents = float3(obb.extentX, obb.extentY, obb.extentZ);

        // Express the voxel center in the local coordinate system of the box.
        const float3 voxelCenterBS = mul(voxelCenterWS - obb.center, transpose(obbFrame));
        const float3 voxelCenterCS = (voxelCenterBS * rcp(obbExtents));

        float3 voxelCenterVolSpace = mul(InvVolumeMatrix, float4(voxelCenterWS, 1)).xyz;

        float4 volumeDensity = VolumetricFogFunction(voxelCenterWS, voxelCenterCS);

        float3 mask = abs(voxelCenterVolSpace) * 2.0;
        mask = saturate(mask);

        float l = max(mask.x, max(mask.y, mask.z)) * 2.5;
        l = 1.0-saturate( l - 1.5 );

        _VBufferDensity[voxelCoord] = lerp(_VBufferDensity[voxelCoord], volumeDensity, min(volumeDensity.a, l));

        t0 = t1;
    }
}
