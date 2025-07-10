
StructuredBuffer<uint> _UnifiedRT_DispatchDims;

#pragma kernel MainRayGenShader
[numthreads(UNIFIED_RT_GROUP_SIZE_X, UNIFIED_RT_GROUP_SIZE_Y, UNIFIED_RT_GROUP_SIZE_Z)]
void MainRayGenShader(
    in uint3 gidx: SV_DispatchThreadID,
    in uint lidx : SV_GroupIndex)
{
    if (gidx.x >= _UnifiedRT_DispatchDims[0] || gidx.y >= _UnifiedRT_DispatchDims[1] || gidx.z >= _UnifiedRT_DispatchDims[2])
        return;

    UnifiedRT::DispatchInfo dispatchInfo;
    dispatchInfo.dispatchThreadID = gidx;
    dispatchInfo.dispatchDimensionsInThreads = int3(_UnifiedRT_DispatchDims[0], _UnifiedRT_DispatchDims[1], _UnifiedRT_DispatchDims[2]);
    dispatchInfo.localThreadIndex = lidx;
    dispatchInfo.globalThreadIndex = gidx.x + gidx.y * _UnifiedRT_DispatchDims[0] + gidx.z * (_UnifiedRT_DispatchDims[0] * _UnifiedRT_DispatchDims[1]);

    UNIFIED_RT_RAYGEN_FUNC(dispatchInfo);
}

RWStructuredBuffer<uint> _UnifiedRT_DispatchDimsInWorkgroups;

#pragma kernel ComputeIndirectDispatchDims
[numthreads(3, 1, 1)]
void ComputeIndirectDispatchDims(in uint gidx : SV_DispatchThreadID)
{
    uint3 workgroupSizes = uint3(UNIFIED_RT_GROUP_SIZE_X, UNIFIED_RT_GROUP_SIZE_Y, UNIFIED_RT_GROUP_SIZE_Z);
    _UnifiedRT_DispatchDimsInWorkgroups[gidx] = (_UnifiedRT_DispatchDims[gidx] + workgroupSizes[gidx] - 1) / workgroupSizes[gidx];
}
