#define PATCH_UTIL_USE_RW_IRRADIANCE_BUFFER

#include "Common.hlsl"
#include "PathTracing.hlsl"
#include "Estimation.hlsl"
#include "RingBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/QuasiRandom.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"

StructuredBuffer<uint> _RingConfigBuffer;
RWStructuredBuffer<SphericalHarmonics::RGBL1> _PatchIrradiances;
StructuredBuffer<PatchUtil::PatchGeometry> _PatchGeometries;
RWStructuredBuffer<PatchUtil::PatchStatisticsSet> _PatchStatistics;
RWStructuredBuffer<PatchUtil::PatchCounterSet> _PatchCounterSets;
StructuredBuffer<uint> _CellPatchIndices;
StructuredBuffer<int3> _CascadeOffsets;

UNIFIED_RT_DECLARE_ACCEL_STRUCT(g_SceneAccelStruct);

StructuredBuffer<PTLight> g_LightList;

uint _FrameIdx;
uint _GridSize;
uint _CascadeCount;
float _VoxelMinSize;
uint _MultiBounce;
uint _SampleCount;
float _ShortHysteresis;
uint _RingConfigOffset;
float3 _GridTargetPos;

TextureCube<float3>             g_EnvTex;
SamplerState                    sampler_g_EnvTex;

void Estimate(UnifiedRT::DispatchInfo dispatchInfo)
{
    uint patchIdx = dispatchInfo.dispatchThreadID.x;

    if (RingBuffer::IsPositionUnused(_RingConfigBuffer, _RingConfigOffset, patchIdx))
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    QrngKronecker rng;
    rng.Init(uint2(patchIdx, 0), _FrameIdx * _SampleCount);

    const PatchUtil::PatchGeometry patchGeo = _PatchGeometries[patchIdx];

    UnifiedRT::Ray ray;
    ray.origin = OffsetRayOrigin(patchGeo.position, patchGeo.normal);
    ray.tMin = 0;
    ray.tMax = FLT_MAX;

    SphericalHarmonics::RGBL1 radianceAccumulator = (SphericalHarmonics::RGBL1)0;

    uint validSampleCount = 0;
    for(uint sampleIdx = 0; sampleIdx < _SampleCount; ++sampleIdx)
    {
        ray.direction = UniformHemisphereSample(float2(rng.GetFloat(0), rng.GetFloat(1)), patchGeo.normal);
        const float3 radianceSample = SampleIncomingRadianceAssumingLambertianBrdf(
            dispatchInfo,
            accelStruct,
            ray,
            g_LightList,
            _MultiBounce,
            g_EnvTex,
            sampler_g_EnvTex,
            _PatchIrradiances,
            _CellPatchIndices,
            _GridSize,
            _CascadeOffsets,
            _GridTargetPos,
            _CascadeCount,
            _VoxelMinSize);

        // If we hit the backface of a water-tight piece of geometry we skip. This is to prevent accumulating "false" darkness
        // which can give artifacts if a patch reappears after temporarily being inside moving geometry.
        // If we hit the backface of geometry which is not water tight, then this most likely a user authoring problem.
        if (all(radianceSample == invalidRadianceSample))
            continue;

        validSampleCount++;

        radianceAccumulator.l0 += radianceSample * SphericalHarmonics::y0;
        radianceAccumulator.l1s[0] += radianceSample * SphericalHarmonics::y1Constant * ray.direction.x;
        radianceAccumulator.l1s[1] += radianceSample * SphericalHarmonics::y1Constant * ray.direction.z;
        radianceAccumulator.l1s[2] += radianceSample * SphericalHarmonics::y1Constant * ray.direction.y;

        rng.NextSample();
    }

    if (validSampleCount != 0)
    {
        // Normalize MC estimate.
        const float pi2 = 2.0f * PI; // This is the inverse density of uniform hemisphere sampling.
        const float normalizationFactor = pi2 / (float)validSampleCount;
        const SphericalHarmonics::RGBL1 radianceSampleMean = SphericalHarmonics::MulPure(radianceAccumulator, normalizationFactor);
        ProcessAndStoreRadianceSample(_PatchIrradiances, _PatchStatistics, _PatchCounterSets, patchIdx, radianceSampleMean, _ShortHysteresis);
    }
}
