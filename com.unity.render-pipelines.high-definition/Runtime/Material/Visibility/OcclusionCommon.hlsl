#ifndef OCCLUSION_COMMON_HLSL
#define OCCLUSION_COMMON_HLSL

#include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

ByteAddressBuffer instanceData;
// TODO: Somehow read these per draw command instead of one global one (works with RenderBRG though)
uint instancePositionMetadata;
uint instanceBatchID;
static uint occlusion_instanceID;

uint ComputeDOTSInstanceDataAddressCustom(uint metadata, uint instanceIndex, uint stride)
{
    uint baseAddress  = metadata & 0x7fffffff;
    uint offset       = instanceIndex * stride;
    return baseAddress + offset;
}

float4x4 LoadDOTSInstancedData_float4x4_from_float3x4_customBuffer(uint metadata, uint instanceIndex)
{
    uint address = ComputeDOTSInstanceDataAddressCustom(metadata, instanceIndex, 3 * 16);
    float4 p1 = asfloat(instanceData.Load4(address + 0 * 16));
    float4 p2 = asfloat(instanceData.Load4(address + 1 * 16));
    float4 p3 = asfloat(instanceData.Load4(address + 2 * 16));

    return float4x4(
        p1.x, p1.w, p2.z, p3.y,
        p1.y, p2.x, p2.w, p3.z,
        p1.z, p2.y, p3.x, p3.w,
        0.0,  0.0,  0.0,  1.0
    );
}

float4x4 LoadObjectToWorld()
{
    return LoadDOTSInstancedData_float4x4_from_float3x4_customBuffer(instancePositionMetadata, occlusion_instanceID);
}

#undef UNITY_MATRIX_M
#undef UNITY_MATRIX_I_M
#undef UNITY_PREV_MATRIX_M
#undef UNITY_PREV_MATRIX_I_M

#if defined(MODIFY_MATRIX_FOR_CAMERA_RELATIVE_RENDERING) || (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
#define UNITY_MATRIX_M          ApplyCameraTranslationToMatrix(LoadObjectToWorld())
#else
#define UNITY_MATRIX_M          LoadObjectToWorld()
#endif
#define UNITY_MATRIX_I_M        float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)
#define UNITY_PREV_MATRIX_M     float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)
#define UNITY_PREV_MATRIX_I_M   float4x4(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1)

struct MeshBounds
{
    float3 center;
    float3 extents;
};

static const float qnan = asfloat(0x7FFFFFFF);
static const int occlusion_debugInstanceID = -1;

static MeshBounds occlusion_meshBounds;
float3 TransformBoundsVertex(float3 positionOS)
{
    if (occlusion_debugInstanceID >= 0)
    {
        if (occlusion_instanceID != occlusion_debugInstanceID)
            return qnan;
    }

    return (positionOS * occlusion_meshBounds.extents) + occlusion_meshBounds.center;
}

#endif
