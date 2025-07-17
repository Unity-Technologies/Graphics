#ifndef _UNIFIEDRAYTRACING_BINDINGS_HLSL_
#define _UNIFIEDRAYTRACING_BINDINGS_HLSL_

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/CommonStructs.hlsl"

#if defined(UNIFIED_RT_BACKEND_COMPUTE)
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/transform.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RadeonRays/kernels/intersector_common.hlsl"

#ifndef UNIFIED_RT_GROUP_SIZE_X
#define UNIFIED_RT_GROUP_SIZE_X 16
#endif

#ifndef UNIFIED_RT_GROUP_SIZE_Y
#define UNIFIED_RT_GROUP_SIZE_Y 8
#endif

#ifndef UNIFIED_RT_GROUP_SIZE_Z
#define UNIFIED_RT_GROUP_SIZE_Z 1
#endif

#endif
namespace UnifiedRT {

struct RayTracingAccelStruct
{
#if defined(UNIFIED_RT_BACKEND_HARDWARE)
    RaytracingAccelerationStructure accelStruct;
#elif defined(UNIFIED_RT_BACKEND_COMPUTE)
    StructuredBuffer<BvhNode> bvh;
    StructuredBuffer<BvhNode> bottom_bvhs;
    StructuredBuffer<uint4> bottom_bvh_leaves;
    StructuredBuffer<InstanceInfo> instance_infos;
    StructuredBuffer<uint> vertexBuffer;

#else
    #pragma message("Error, you must define either UNIFIED_RT_BACKEND_HARDWARE or UNIFIED_RT_BACKEND_COMPUTE")
#endif

};

#if defined(UNIFIED_RT_BACKEND_HARDWARE)
RayTracingAccelStruct GetAccelStruct(RaytracingAccelerationStructure accelStruct)
{
    RayTracingAccelStruct res;
    res.accelStruct = accelStruct;
    return res;
}

#define UNIFIED_RT_DECLARE_ACCEL_STRUCT(name) RaytracingAccelerationStructure name##accelStruct
#define UNIFIED_RT_GET_ACCEL_STRUCT(name) UnifiedRT::GetAccelStruct(name##accelStruct)

#elif defined(UNIFIED_RT_BACKEND_COMPUTE)
RayTracingAccelStruct GetAccelStruct(
    StructuredBuffer<BvhNode> bvh,
    StructuredBuffer<BvhNode> bottomBvhs,
    StructuredBuffer<uint4> bottomBvhLeaves,
    StructuredBuffer<InstanceInfo> instanceInfos,
    StructuredBuffer<uint> vertexBuffer)
{
    RayTracingAccelStruct res;
    res.bvh = bvh;
    res.bottom_bvhs = bottomBvhs;
    res.bottom_bvh_leaves = bottomBvhLeaves;
    res.instance_infos = instanceInfos;
    res.vertexBuffer = vertexBuffer;
    return res;
}

#define UNIFIED_RT_DECLARE_ACCEL_STRUCT(name) StructuredBuffer<BvhNode> name##bvh; StructuredBuffer<BvhNode> name##bottomBvhs; StructuredBuffer<uint4> name##bottomBvhLeaves; StructuredBuffer<InstanceInfo> name##instanceInfos; StructuredBuffer<uint> name##vertexBuffer
#define UNIFIED_RT_GET_ACCEL_STRUCT(name) UnifiedRT::GetAccelStruct(name##bvh, name##bottomBvhs, name##bottomBvhLeaves, name##instanceInfos, name##vertexBuffer)

#endif

} // namespace UnifiedRT


#endif // UNIFIEDRAYTRACING_BINDINGS_HLSL
