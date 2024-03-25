#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/Common.hlsl"

#define RNG_METHOD 5 // SOBOL
#define RAND_SAMPLES_PER_BOUNCE 2
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/Random.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/Common.hlsl"

UNITY_DECLARE_RT_ACCEL_STRUCT(_AccelStruct);


int _SampleCount;
int _SampleId;
int _MaxBounces;
float _OffsetRay;
float _AverageAlbedo;
int _BackFaceCulling;
int _BakeSkyShadingDirection;

StructuredBuffer<float3> _ProbePositions;
RWStructuredBuffer<float4> _SkyOcclusionOut;
RWStructuredBuffer<float3> _SkyShadingOut;

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    const float kSHBasis0 = 0.28209479177387814347f;
    const float kSHBasis1 = 0.48860251190291992159f;

    int probeId = dispatchInfo.globalThreadIndex;

    RngState rngState;
    rngState.Init(uint2((uint)probeId, 0), 1, _SampleId);

    if (_SampleId==0)
    {
        _SkyOcclusionOut[probeId] = float4(0,0,0,0);
        if (_BakeSkyShadingDirection > 0)
            _SkyShadingOut[probeId] = float3(0,0,0);
    }

    UnifiedRT::RayTracingAccelStruct accelStruct = UNITY_GET_RT_ACCEL_STRUCT(_AccelStruct);

    float2 u = float2(rngState.GetFloatSample(0), rngState.GetFloatSample(1));
    float3 rayFirstDirection = SphereSample(u);

    float pathWeight = 4.0f * PI; // 1 / SphereSamplePDF
    float3 normalWS = float3(0,1,0);
    float3 hitPointWS = float3(0,0,0);
    uint rayFlags = 0x0;
    if(_BackFaceCulling != 0)
        rayFlags = UnifiedRT::kRayFlagCullBackFacingTriangles;

    for (int bounceIndex=0; bounceIndex < _MaxBounces+1; bounceIndex++)
    {
        UnifiedRT::Ray ray;
        ray.tMin = 0;
        ray.tMax = FLT_MAX;
        ray.origin = float3(0, 0, 0);
        ray.direction = float3(0, 0, 0);
        UnifiedRT::Hit hitResult;

        if (bounceIndex==0)
        {
            ray.direction = rayFirstDirection;
            ray.origin = _ProbePositions[probeId].xyz;
        }
        else
        {
            u = float2(rngState.GetFloatSample(2*bounceIndex), rngState.GetFloatSample(2*bounceIndex+1));

            SampleDiffuseBrdf(u, normalWS, ray.direction);
            ray.direction = normalize(ray.direction);

            ray.origin = hitPointWS + _OffsetRay * ray.direction;
            float cosTheta = clamp(dot(normalWS, ray.direction),0.f,1.0f);

            if(cosTheta < 0.001f)
                break;

            pathWeight = pathWeight * _AverageAlbedo; // cosTheta * avgAlbedo / PI * PI/(cosTheta) == avgAlbedo
        }

        bool hasHit = false;

        hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, rayFlags);
        hasHit = hitResult.IsValid();

        if (hasHit)
        {
            UnifiedRT::InstanceData instanceInfo = UnifiedRT::GetInstance(accelStruct, hitResult.instanceIndex);
            PTHitGeom hitGeom = (PTHitGeom)0;
            hitGeom = GetHitGeomInfo(accelStruct, instanceInfo, hitResult);
            hitPointWS = hitGeom.worldPosition;
            normalWS = hitGeom.worldNormal;
            if (dot(normalWS, ray.direction) > 0.0f) // flip normal if hitting backface
                normalWS *= -1.0f;
        }
        else
        {
            float norm = pathWeight / (float)_SampleCount;
            // Layout is DC, x, y, z
            float4 tempSH = float4(
                norm * kSHBasis0,
                rayFirstDirection.x * norm * kSHBasis1,
                rayFirstDirection.y * norm * kSHBasis1,
                rayFirstDirection.z * norm * kSHBasis1);

            _SkyOcclusionOut[probeId] += tempSH;
            if(_BakeSkyShadingDirection > 0)
                _SkyShadingOut[probeId] += ray.direction / _SampleCount;

            // break the loop;
            bounceIndex = _MaxBounces + 2;
        }
    }

    // Last sample
    if (_SampleId == _SampleCount - 1)
    {
        // Window L1 coefficients to make sure no value is negative when sampling SH, layout is DC, x, y, z
        float4 SHData = _SkyOcclusionOut[probeId];
        // find main direction for light
        float3 mainDir;
        mainDir.x = SHData.y;
        mainDir.y = SHData.z;
        mainDir.z = SHData.w;
        mainDir = normalize(mainDir);

        // find the value in the opposite direction, which is the lowest value in the SH
        float4 temp2 = float4(kSHBasis0, kSHBasis1 * -mainDir.x, kSHBasis1 * -mainDir.y, kSHBasis1 * -mainDir.z);
        float value = dot(temp2, SHData);
        float windowL1 = 1.0f;

        if (value < 0.0f)
        {
            // find the L1 factor for this value to be null instead of negative
            windowL1 = -(temp2.x * SHData.x) / dot(temp2.yzw, SHData.yzw);
            windowL1 = saturate(windowL1);
        }

        _SkyOcclusionOut[probeId].yzw *= windowL1;

        float radianceToIrradianceFactor = 2.0f / 3.0f;
        // This is a hacky solution for mitigating the radianceToIrradianceFactor based on the previous windowing operation.
        // The 1.125f exponent comes from experimental testing. It's the value that works the best when trying to match a bake and deringing done with the lightmapper, but it has no theoretical explanation.
        // In the future, we should replace these custom windowing and deringing operations with the ones used in the lightmapper to implement a more academical solution.
        _SkyOcclusionOut[probeId].yzw *= lerp(1.0f, radianceToIrradianceFactor, pow(windowL1, 1.125f));
    }
}
