#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ShaderVariablesProbeVolumes.cs.hlsl"

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

#define RNG_METHOD 2 // XOR_SHIFT
#define SAMPLE_COUNT 32
#define RAND_SAMPLES_PER_BOUNCE 2
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/Random.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/Common.hlsl"

UNITY_DECLARE_RT_ACCEL_STRUCT(_AccelStruct);

StructuredBuffer<float3> _ProbePositions;
RWStructuredBuffer<uint> _LayerMasks;

float4 _RenderingLayerMasks;

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    UnifiedRT::Ray ray;
    ray.origin = _ProbePositions[dispatchInfo.globalThreadIndex].xyz;
    ray.tMax = FLT_MAX;
    ray.tMin = 0.0f;

    RngState rngState;
    rngState.Init(0, 1, SAMPLE_COUNT);

    int4 hitCount = 0;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNITY_GET_RT_ACCEL_STRUCT(_AccelStruct);

    for (uint i = 0; i < SAMPLE_COUNT; ++i)
    {
        float2 u = float2(rngState.GetFloatSample(2*i), rngState.GetFloatSample(2*i+1));
        ray.direction = SphereSample(u);

        uint hitMask = 0;
        UnifiedRT::Hit hit = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, 0);

        if (hit.IsValid() & hit.isFrontFace)
        {
            // we use material id to store layer mask
            uint objMask = accelStruct.instanceList[hit.instanceIndex].userMaterialID;

            [unroll]
            for (int l = 0; l < PROBE_MAX_REGION_COUNT; l++)
            {
                if ((asuint(_RenderingLayerMasks[l]) & objMask) != 0)
                    hitCount[l]++;
            }
        }
    }

    uint layerMask = 0;

    if (true)
    {
        // Find the layer with the most hits
        uint index = 0;
        layerMask = 0xF;

        [unroll]
        for (uint l = 1; l < PROBE_MAX_REGION_COUNT; l++)
        {
            if (hitCount[l] > hitCount[index])
                index = l;
        }
        if (hitCount[index] != 0)
            layerMask = 1u << index;
    }
    else
    {
        // Find any layer that was hit
        [unroll]
        for (uint l = 1; l < PROBE_MAX_REGION_COUNT; l++)
        {
            if (hitCount[l] != 0)
                layerMask |= 1u << l;
        }
    }

    _LayerMasks[dispatchInfo.globalThreadIndex] = layerMask;
}
