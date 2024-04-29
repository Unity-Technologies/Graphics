#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"


#define DISTANCE_THRESHOLD 5e-5f
#define DOT_THRESHOLD 1e-2f

#define SAMPLE_COUNT (3*3*3 - 1)

static const float k0 = 0, k1 = 1, k2 = 0.70710678118654752440084436210485f, k3 = 0.57735026918962576450914878050196f;
static const float3 k_RayDirections[SAMPLE_COUNT] = {
    float3(-k3, +k3, -k3), // -1  1 -1
    float3( k0, +k2, -k2), //  0  1 -1
    float3(+k3, +k3, -k3), //  1  1 -1
    float3(-k2, +k2,  k0), // -1  1  0
    float3( k0, +k1,  k0), //  0  1  0
    float3(+k2, +k2,  k0), //  1  1  0
    float3(-k3, +k3, +k3), // -1  1  1
    float3( k0, +k2, +k2), //  0  1  1
    float3(+k3, +k3, +k3), //  1  1  1

    float3(-k2,  k0, -k2), // -1  0 -1
    float3( k0,  k0, -k1), //  0  0 -1
    float3(+k2,  k0, -k2), //  1  0 -1
    float3(-k1,  k0,  k0), // -1  0  0
    // k0, k0, k0 - skip center position (which would be a zero-length ray)
    float3(+k1,  k0,  k0), //  1  0  0
    float3(-k2,  k0, +k2), // -1  0  1
    float3( k0,  k0, +k1), //  0  0  1
    float3(+k2,  k0, +k2), //  1  0  1

    float3(-k3, -k3, -k3), // -1 -1 -1
    float3( k0, -k2, -k2), //  0 -1 -1
    float3(+k3, -k3, -k3), //  1 -1 -1
    float3(-k2, -k2,  k0), // -1 -1  0
    float3( k0, -k1,  k0), //  0 -1  0
    float3(+k2, -k2,  k0), //  1 -1  0
    float3(-k3, -k3, +k3), // -1 -1  1
    float3( k0, -k2, +k2), //  0 -1  1
    float3(+k3, -k3, +k3), //  1 -1  1;
};

UNITY_DECLARE_RT_ACCEL_STRUCT(_AccelStruct);

struct ProbeData
{
    float3 position;
    float originBias;
    float tMax;
    float geometryBias;
    int probeIndex;
    float validityThreshold;
};

StructuredBuffer<ProbeData> _Probes;
RWStructuredBuffer<float3> _Offsets;

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    ProbeData probe = _Probes[dispatchInfo.globalThreadIndex];
    float3 outDirection = 0.0f;
    float maxDotSurface = -1;
    float minDist = FLT_MAX;

    UnifiedRT::Ray ray;
    ray.tMax = probe.tMax;
    ray.tMin = 0.0f;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNITY_GET_RT_ACCEL_STRUCT(_AccelStruct);

    uint validHits = 0;
    for (uint i = 0; i < SAMPLE_COUNT; ++i)
    {
        ray.direction = k_RayDirections[i];
        ray.origin = probe.position + probe.originBias * ray.direction;

        UnifiedRT::Hit hit = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, 0);

        // If any of the closest hit is the sky or a front face, skip it
        if (!hit.IsValid() || hit.isFrontFace)
        {
            validHits++;
            continue;
        }

        float distanceDiff = hit.hitDistance - minDist;
        if (distanceDiff < DISTANCE_THRESHOLD)
        {
            //accelStruct.instanceList[hit.instanceIndex].userMaterialID;

            UnifiedRT::HitGeomAttributes attributes = UnifiedRT::FetchHitGeomAttributes(accelStruct, hit, UnifiedRT::kGeomAttribFaceNormal);
            float dotSurface = dot(ray.direction, attributes.faceNormal);

            // If new distance is smaller by at least kDistanceThreshold, or if ray is at least DOT_THRESHOLD more colinear with normal
            if (distanceDiff < -DISTANCE_THRESHOLD || dotSurface - maxDotSurface > DOT_THRESHOLD)
            {
                outDirection = ray.direction;
                maxDotSurface = dotSurface;
                minDist = hit.hitDistance;
            }
        }
    }

    // Disable VO for probes that don't see enough backface
    // validity = percentage of backfaces seen
    float validity = 1.0f - validHits / (float)(SAMPLE_COUNT - 1.0f);
    if (validity <= probe.validityThreshold)
        outDirection = 0.0f;

    if (minDist == FLT_MAX)
        minDist = 0.0f;

    _Offsets[dispatchInfo.globalThreadIndex] = (minDist * 1.05f + probe.geometryBias) * outDirection;
}
