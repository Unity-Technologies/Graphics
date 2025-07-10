#ifndef _UNIFIEDRAYTRACING_COMMONSTRUCTS_HLSL_
#define _UNIFIEDRAYTRACING_COMMONSTRUCTS_HLSL_

namespace UnifiedRT {

static const uint kRayFlagNone = 0x0;
static const uint kRayFlagForceOpaque = 0x01;
static const uint kRayFlagForceNonOpaque = 0x02;
static const uint kRayFlagAcceptFirstHitAndEndSearch = 0x04;
static const uint kRayFlagSkipClosestHit = 0x08;
static const uint kRayFlagCullBackFacingTriangles = 0x10;
static const uint kRayFlagCullFrontFacingTriangles = 0x20;
static const uint kRayFlagCullOpaque = 0x40;
static const uint kRayFlagCullNonOpaque = 0x80;

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
    float4x4 localToWorld;
    float4x4 previousLocalToWorld;
    float4x4 localToWorldNormals;
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
