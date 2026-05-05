#ifndef _UNIFIEDRAYTRACING_COMMONSTRUCTS_HLSL_
#define _UNIFIEDRAYTRACING_COMMONSTRUCTS_HLSL_

namespace UnifiedRT {

// With the HW raytracing backend use the RAY_FLAG macros directly which hold the correct platform specific values. 
// With the compute backend, the RAY_FLAG macros might not be defined so use the hardcoded DXR values.
#if defined(UNIFIED_RT_BACKEND_HARDWARE)
static const uint kRayFlagNone = RAY_FLAG_NONE;
static const uint kRayFlagForceOpaque = RAY_FLAG_FORCE_OPAQUE;
static const uint kRayFlagForceNonOpaque = RAY_FLAG_FORCE_NON_OPAQUE;
static const uint kRayFlagAcceptFirstHitAndEndSearch = RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH;
static const uint kRayFlagSkipClosestHit = RAY_FLAG_SKIP_CLOSEST_HIT_SHADER;
static const uint kRayFlagCullBackFacingTriangles = RAY_FLAG_CULL_BACK_FACING_TRIANGLES;
static const uint kRayFlagCullFrontFacingTriangles = RAY_FLAG_CULL_FRONT_FACING_TRIANGLES;
static const uint kRayFlagCullOpaque = RAY_FLAG_CULL_OPAQUE;
static const uint kRayFlagCullNonOpaque = RAY_FLAG_CULL_NON_OPAQUE;
#else
static const uint kRayFlagNone = 0x0;
static const uint kRayFlagForceOpaque = 0x01;
static const uint kRayFlagForceNonOpaque = 0x02;
static const uint kRayFlagAcceptFirstHitAndEndSearch = 0x04;
static const uint kRayFlagSkipClosestHit = 0x08;
static const uint kRayFlagCullBackFacingTriangles = 0x10;
static const uint kRayFlagCullFrontFacingTriangles = 0x20;
static const uint kRayFlagCullOpaque = 0x40;
static const uint kRayFlagCullNonOpaque = 0x80;
#endif

static const uint kIgnoreHit = 0;
static const uint kAcceptHit = 1;
static const uint kAcceptHitAndEndSearch = 2;

struct Ray
{
    float3 origin;
    float  tMin;
    float3 direction;
    float  tMax;
};

struct Hit
{
    uint instanceID;
    uint primitiveIndex;
    float2 uvBarycentrics;
    float hitDistance;
    bool isFrontFace;

    bool IsValid()
    {
        return instanceID != -1;
    }

    static Hit Invalid()
    {
        Hit hit = (Hit)0;
        hit.instanceID = -1;
        return hit;
    }
};


struct InstanceData
{
    float4x3 localToWorld; // transpose before transforming a vector (or do a left-side multiplication) float3x4 isn't used to avoid wasting space due its column alignment to float4s
    float localToWorldDeterminant;
    float localToWorldDetSign;
    uint padding0;
    uint padding1;
    float4x3 previousLocalToWorld; // transpose before transforming a vector (or do a left-side multiplication)
    float4x3 localToWorldNormals; // cast to float3x3 before use (right-side multiplication to transform a vector)
    uint renderingLayerMask;
    uint instanceMask;
    uint userMaterialID;
    uint geometryIndex;
};

struct DispatchInfo
{
    uint3 dispatchThreadID;
    uint localThreadIndex;
    uint3 dispatchDimensionsInThreads;
    uint globalThreadIndex;
};

} // namespace UnifiedRT

#endif // _UNIFIEDRAYTRACING_COMMONSTRUCTS_HLSL_
