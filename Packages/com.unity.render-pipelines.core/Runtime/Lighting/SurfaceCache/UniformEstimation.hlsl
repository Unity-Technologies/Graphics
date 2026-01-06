#define PATCH_UTIL_USE_RW_IRRADIANCE_BUFFER

#include "Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/QuasiRandom.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "PathTracing.hlsl"
#include "Estimation.hlsl"
#include "RingBuffer.hlsl"
#include "PunctualLights.hlsl"

StructuredBuffer<uint> _RingConfigBuffer;
RWStructuredBuffer<SphericalHarmonics::RGBL1> _PatchIrradiances;
StructuredBuffer<PatchUtil::PatchGeometry> _PatchGeometries;
RWStructuredBuffer<PatchUtil::PatchStatisticsSet> _PatchStatistics;
StructuredBuffer<uint> _CellPatchIndices;
StructuredBuffer<int3> _CascadeOffsets;
StructuredBuffer<MaterialPool::MaterialEntry> _MaterialEntries;
StructuredBuffer<PunctualLightSample> _PunctualLightSamples;
Texture2DArray _AlbedoTextures;
Texture2DArray _TransmissionTextures;
Texture2DArray _EmissionTextures;
SamplerState sampler_EmissionTextures;
SamplerState sampler_AlbedoTextures;
SamplerState sampler_TransmissionTextures;
TextureCube<float3> _EnvironmentCubemap;
SamplerState sampler_EnvironmentCubemap;
UNIFIED_RT_DECLARE_ACCEL_STRUCT(_RayTracingAccelerationStructure);

uint _PunctualLightCount;
uint _FrameIdx;
uint _VolumeSpatialResolution;
uint _CascadeCount;
float _VolumeVoxelMinSize;
uint _MultiBounce;
uint _SampleCount;
float _ShortHysteresis;
uint _RingConfigOffset;
float3 _VolumeTargetPos;
float _MaterialAtlasTexelSize; // The size of 1 texel in the atlases above
float _AlbedoBoost;
uint _PunctualLightSampleCount;
float3 _DirectionalLightDirection;
float3 _DirectionalLightIntensity;

void ProjectAndAccumulate(inout SphericalHarmonics::RGBL1 accumulator, float3 sample, float3 direction)
{
    accumulator.l0 += sample * SphericalHarmonics::y0;
    accumulator.l1s[0] += sample * SphericalHarmonics::y1Constant * direction.y;
    accumulator.l1s[1] += sample * SphericalHarmonics::y1Constant * direction.z;
    accumulator.l1s[2] += sample * SphericalHarmonics::y1Constant * direction.x;
}

void SamplePunctualLightBounceRadiance(
    inout QrngKronecker rng,
    uint patchIdx,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    UnifiedRT::DispatchInfo dispatchInfo,
    PatchUtil::PatchGeometry patchGeo,
    inout SphericalHarmonics::RGBL1 accumulator,
    inout bool gotValidSamples)
{
    rng.Init(patchIdx, _FrameIdx * _SampleCount);
    SphericalHarmonics::RGBL1 radianceAccumulator = (SphericalHarmonics::RGBL1)0;

    uint validSampleCount = 0;
    for(uint sampleIdx = 0; sampleIdx < _SampleCount; ++sampleIdx)
    {
        PunctualLightBounceRadianceSample sample = SamplePunctualLightBounceRadiance(
            dispatchInfo,
            accelStruct,
            _PunctualLightSamples,
            _PunctualLightSampleCount,
            rng.GetFloat(0),
            patchGeo.position,
            patchGeo.normal);

        if (!sample.IsValid())
            continue;

        validSampleCount++;
        ProjectAndAccumulate(radianceAccumulator, sample.radianceOverDensity, sample.direction);

        rng.NextSample();
    }

    if (validSampleCount != 0)
    {
        gotValidSamples = true;
        const float normalizationFactor = rcp(validSampleCount);
        SphericalHarmonics::AddMut(accumulator, SphericalHarmonics::MulPure(radianceAccumulator, normalizationFactor));
    }
}

void SampleEnvironmentAndDirectionalBounceAndMultiBounceRadiance(
    inout QrngKronecker rng,
    uint patchIdx,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    UnifiedRT::DispatchInfo dispatchInfo,
    MaterialPoolParamSet matPoolParams,
    PatchUtil::PatchGeometry patchGeo,
    inout SphericalHarmonics::RGBL1 accumulator,
    inout bool gotValidSamples)
{
    rng.Init(patchIdx, _FrameIdx * _SampleCount);

    UnifiedRT::Ray ray;
    ray.origin = OffsetRayOrigin(patchGeo.position, patchGeo.normal);
    ray.tMin = 0;
    ray.tMax = FLT_MAX;

    SphericalHarmonics::RGBL1 radianceAccumulator = (SphericalHarmonics::RGBL1)0;

    uint validSampleCount = 0;
    for(uint sampleIdx = 0; sampleIdx < _SampleCount; ++sampleIdx)
    {
        ray.direction = UniformHemisphereSample(float2(rng.GetFloat(0), rng.GetFloat(1)), patchGeo.normal);
        const float3 radiance = IncomingEnviromentAndDirectionalBounceAndMultiBounceRadiance(
            dispatchInfo,
            accelStruct,
            ray,
            matPoolParams,
            _DirectionalLightDirection,
            _DirectionalLightIntensity,
            _MultiBounce,
            _EnvironmentCubemap,
            sampler_EnvironmentCubemap,
            _PatchIrradiances,
            _CellPatchIndices,
            _VolumeSpatialResolution,
            _CascadeOffsets,
            _VolumeTargetPos,
            _CascadeCount,
            _VolumeVoxelMinSize);

        if (all(radiance == invalidRadiance))
            continue;

        validSampleCount++;
        ProjectAndAccumulate(radianceAccumulator, radiance, ray.direction);

        rng.NextSample();
    }

    if (validSampleCount != 0)
    {
        gotValidSamples = true;
        const float reciprocalDensity = 2.0f * PI;
        const float normalizationFactor = reciprocalDensity * rcp(validSampleCount);
        SphericalHarmonics::AddMut(accumulator, SphericalHarmonics::MulPure(radianceAccumulator, normalizationFactor));
    }
}

void Estimate(UnifiedRT::DispatchInfo dispatchInfo)
{
    uint patchIdx = dispatchInfo.dispatchThreadID.x;

    if (!RingBuffer::IsPositionInUse(_RingConfigBuffer, _RingConfigOffset, patchIdx))
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_RayTracingAccelerationStructure);
    QrngKronecker rng;

    const PatchUtil::PatchGeometry patchGeo = _PatchGeometries[patchIdx];

    MaterialPoolParamSet matPoolParams;
    matPoolParams.materialEntries = _MaterialEntries;
    matPoolParams.albedoTextures = _AlbedoTextures;
    matPoolParams.transmissionTextures = _TransmissionTextures;
    matPoolParams.emissionTextures = _EmissionTextures;
    matPoolParams.emissionSampler = sampler_EmissionTextures;
    matPoolParams.albedoSampler = sampler_AlbedoTextures;
    matPoolParams.transmissionSampler = sampler_TransmissionTextures;
    matPoolParams.atlasTexelSize = _MaterialAtlasTexelSize;
    matPoolParams.albedoBoost = _AlbedoBoost;

    SphericalHarmonics::RGBL1 radianceSampleMean = (SphericalHarmonics::RGBL1)0;
    bool gotValidSamples = false;

    SampleEnvironmentAndDirectionalBounceAndMultiBounceRadiance(
        rng,
        patchIdx,
        accelStruct,
        dispatchInfo,
        matPoolParams,
        patchGeo,
        radianceSampleMean,
        gotValidSamples);

    if (_PunctualLightCount != 0)
    {
        SamplePunctualLightBounceRadiance(
            rng,
            patchIdx,
            accelStruct,
            dispatchInfo,
            patchGeo,
            radianceSampleMean,
            gotValidSamples);
    }

    if (gotValidSamples)
        ProcessAndStoreRadianceSample(_PatchIrradiances, _PatchStatistics, patchIdx, radianceSampleMean, _ShortHysteresis);
}
