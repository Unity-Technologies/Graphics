#define PATCH_UTIL_USE_RW_PATCH_IRRADIANCE_BUFFER

#include "Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/QuasiRandom.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "PathTracing.hlsl"
#include "PatchAllocationRequest.hlsl"
#include "RingBuffer.hlsl"
#include "PatchUtil.hlsl"
#include "TemporalFiltering.hlsl"
#include "PunctualLights.hlsl"

RWStructuredBuffer<SphericalHarmonics::RGBL1> _PatchIrradiances;
RWStructuredBuffer<PatchUtil::PatchStatisticsSet> _PatchStatistics;
RWStructuredBuffer<PatchAllocationRequest> _PatchAllocationRequests;
RWStructuredBuffer<uint> _PatchAllocationRequestCount;
StructuredBuffer<uint> _RingConfigBuffer;
StructuredBuffer<PatchUtil::PatchGeometry> _PatchGeometries;
StructuredBuffer<uint> _CellPatchIndices;
StructuredBuffer<int3> _VolumeCascadeOffsets;
StructuredBuffer<MaterialPool::MaterialEntry> _MaterialEntries;
StructuredBuffer<PunctualLightSample> _PunctualLightSamples;
StructuredBuffer<PunctualLight> _PunctualLights;
Texture2DArray _AlbedoTextures;
Texture2DArray _EmissionTextures;
SamplerState sampler_EmissionTextures;
SamplerState sampler_AlbedoTextures;
TextureCube<float3> _EnvironmentCubemap;
SamplerState sampler_EnvironmentCubemap;
UNIFIED_RT_DECLARE_ACCEL_STRUCT(_RayTracingAccelerationStructure);

uint _PunctualLightCount;
uint _FrameIdx;
uint _VolumeSpatialResolution;
uint _VolumeCascadeCount;
float _VolumeVoxelMinSize;
uint _MultiBounce;
uint _SampleCount;
float _ShortHysteresis;
uint _RingConfigOffset;
float3 _VolumeTargetPos;
float _MaterialAtlasTexelSize; // The size of 1 texel in the atlases above
float _AlbedoBoost;
float _EnvironmentIntensityMultiplier;
float _EmissiveTriangleIntensityMultiplier;
uint _PunctualLightSampleCount;
float3 _DirectionalLightDirection;
float3 _DirectionalLightIntensity;
uint _BouncePatchAllocation;

void ProcessAndStoreRadianceSample(RWStructuredBuffer<SphericalHarmonics::RGBL1> patchIrradiances, RWStructuredBuffer<PatchUtil::PatchStatisticsSet> patchStatistics, uint patchIdx, SphericalHarmonics::RGBL1 radianceSample, float shortHysteresis)
{
    SphericalHarmonics::CosineConvolve(radianceSample);

    PatchUtil::PatchStatisticsSet oldStats = patchStatistics[patchIdx];
    const uint oldUpdateCount = PatchUtil::GetUpdateCount(oldStats.counters);
    const uint newUpdateCount = min(oldUpdateCount + 1, PatchUtil::updateMax);

    const SphericalHarmonics::RGBL1 oldIrradiance = patchIrradiances[patchIdx];

    float shortIrradianceUpdateWeight;
    if (oldUpdateCount == 0)
        shortIrradianceUpdateWeight = 0;
    else
        shortIrradianceUpdateWeight = min(1.0f - rcp(oldUpdateCount), shortHysteresis);

    const float3 newL0ShortIrradiance = lerp(radianceSample.l0, oldStats.mean, shortIrradianceUpdateWeight);
    const float3 varianceSample = (radianceSample.l0 - newL0ShortIrradiance) * (radianceSample.l0 - oldStats.mean);
    const float3 newVariance = lerp(varianceSample, oldStats.variance, shortHysteresis);

    SphericalHarmonics::RGBL1 output = FilterTemporallyVarianceGuided(shortHysteresis, newUpdateCount, newVariance, newL0ShortIrradiance, radianceSample, oldIrradiance);

    patchIrradiances[patchIdx] = output;

    PatchUtil::PatchStatisticsSet newStats;
    newStats.mean = newL0ShortIrradiance;
    newStats.variance = newVariance;
    newStats.counters = oldStats.counters;
    PatchUtil::SetUpdateCount(newStats.counters, newUpdateCount);
    patchStatistics[patchIdx] = newStats;
}

void ProjectAndAccumulate(inout SphericalHarmonics::RGBL1 accumulator, float3 sampleRadiance, float3 sampleDirection)
{
    accumulator.l0 += sampleRadiance * SphericalHarmonics::y0;
    accumulator.l1s[0] += sampleRadiance * SphericalHarmonics::y1Constant * sampleDirection.y;
    accumulator.l1s[1] += sampleRadiance * SphericalHarmonics::y1Constant * sampleDirection.z;
    accumulator.l1s[2] += sampleRadiance * SphericalHarmonics::y1Constant * sampleDirection.x;
}

float GetAdditionalRayOffset(float volumeVoxelMinSize)
{
    // We currently use the OffsetRayOrigin() heuristic to offset ray origins to avoid self-intersections. While this
    // helps for Surface Cache it is not enough since the ray origins are relatively imprecise because they are derived
    // from output of the rasterizer (as opposed to being reconstructed via barycentrics).
    // We fix this by adding an additional offset. This offset is a percentage of the min voxel size to keep it
    // somewhat proportional to the scene scale.
    return volumeVoxelMinSize * 0.001;
}

void SamplePunctualLightBounceRadiance(
    inout QrngKronecker2D rng,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    UnifiedRT::DispatchInfo dispatchInfo,
    StructuredBuffer<PunctualLight> lights,
    StructuredBuffer<PunctualLightSample> punctualLightSamples,
    uint punctualLightSampleCount,
    float volumeVoxelMinSize,
    PatchUtil::PatchGeometry patchGeo,
    inout SphericalHarmonics::RGBL1 accumulator,
    inout bool gotValidSamples)
{
    SphericalHarmonics::RGBL1 radianceAccumulator = (SphericalHarmonics::RGBL1)0;

    uint validSampleCount = 0;
    for(uint sampleIdx = 0; sampleIdx < _SampleCount; ++sampleIdx)
    {
        // Using `sample` as a variable name causes compilation errors on PS5.
        PunctualLightBounceRadianceSample sample_ = SamplePunctualLightBounceRadiance(
            dispatchInfo,
            accelStruct,
            lights,
            punctualLightSamples,
            punctualLightSampleCount,
            rng.GetSample(0).x,
            patchGeo.position,
            patchGeo.normal,
            GetAdditionalRayOffset(volumeVoxelMinSize));

        if (!sample_.IsValid())
            continue;

        validSampleCount++;
        ProjectAndAccumulate(radianceAccumulator, sample_.radianceOverDensity, sample_.direction);

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
    bool enablePatchAllocation,
    uint frameIndex,
    inout QrngKronecker2D rng,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    UnifiedRT::DispatchInfo dispatchInfo,
    MaterialPoolParamSet matPoolParams,
    PatchUtil::PatchGeometry patchGeo,
    RWStructuredBuffer<PatchUtil::PatchStatisticsSet> patchStatistics,
    CellPatchIndexBufferType cellPatchIndices,
    PatchUtil::VolumeParamSet volumeParams,
    inout SphericalHarmonics::RGBL1 accumulator,
    inout bool gotValidSamples)
{
    UnifiedRT::Ray ray;
    ray.origin = OffsetRayOrigin(patchGeo.position, patchGeo.normal, GetAdditionalRayOffset(volumeParams.voxelMinSize));
    ray.tMin = 0;
    ray.tMax = FLT_MAX;

    SphericalHarmonics::RGBL1 radianceAccumulator = (SphericalHarmonics::RGBL1)0;

    uint validSampleCount = 0;
    for(uint sampleIdx = 0; sampleIdx < _SampleCount; ++sampleIdx)
    {
        ray.direction = UniformHemisphereSample(rng.GetSample(0), patchGeo.normal);
        const float3 radiance = IncomingEnvironmentAndDirectionalBounceAndMultiBounceRadiance(
            dispatchInfo,
            accelStruct,
            ray,
            matPoolParams,
            _DirectionalLightDirection,
            _DirectionalLightIntensity,
            _MultiBounce,
            _EnvironmentCubemap,
            _EnvironmentIntensityMultiplier,
            _EmissiveTriangleIntensityMultiplier,
            sampler_EnvironmentCubemap,
            _PatchIrradiances,
            patchStatistics,
            _PatchAllocationRequests,
            _PatchAllocationRequestCount,
            cellPatchIndices,
            volumeParams,
            enablePatchAllocation,
            frameIndex);

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
    QrngKronecker2D rng;

    const PatchUtil::PatchGeometry patchGeo = _PatchGeometries[patchIdx];
    bool enablePatchAllocation;
    if (_BouncePatchAllocation)
        enablePatchAllocation = (PatchUtil::GetRank(_PatchStatistics[patchIdx].counters) == 0);
    else
        enablePatchAllocation = false;

    MaterialPoolParamSet matPoolParams;
    matPoolParams.materialEntries = _MaterialEntries;
    matPoolParams.albedoTextures = _AlbedoTextures;
    matPoolParams.emissionTextures = _EmissionTextures;
    matPoolParams.emissionSampler = sampler_EmissionTextures;
    matPoolParams.albedoSampler = sampler_AlbedoTextures;
    matPoolParams.atlasTexelSize = _MaterialAtlasTexelSize;
    matPoolParams.albedoBoost = _AlbedoBoost;

    PatchUtil::VolumeParamSet volumeParams;
    volumeParams.spatialResolution = _VolumeSpatialResolution;
    volumeParams.voxelMinSize = _VolumeVoxelMinSize;
    volumeParams.targetPos = _VolumeTargetPos;
    volumeParams.cascadeOffsets = _VolumeCascadeOffsets;
    volumeParams.cascadeCount = _VolumeCascadeCount;

    SphericalHarmonics::RGBL1 radianceSampleMean = (SphericalHarmonics::RGBL1)0;
    bool gotValidSamples = false;

    const uint patchIdxHash = LowBiasHash32(patchIdx);
    const uint sampleOffset = _FrameIdx * _SampleCount;

    rng.Init(patchIdxHash, sampleOffset);
    SampleEnvironmentAndDirectionalBounceAndMultiBounceRadiance(
        enablePatchAllocation,
        _FrameIdx,
        rng,
        accelStruct,
        dispatchInfo,
        matPoolParams,
        patchGeo,
        _PatchStatistics,
        _CellPatchIndices,
        volumeParams,
        radianceSampleMean,
        gotValidSamples);

    if (_PunctualLightCount != 0)
    {
        rng.Init(patchIdxHash, sampleOffset);
        SamplePunctualLightBounceRadiance(
            rng,
            accelStruct,
            dispatchInfo,
            _PunctualLights,
            _PunctualLightSamples,
            _PunctualLightSampleCount,
            _VolumeVoxelMinSize,
            patchGeo,
            radianceSampleMean,
            gotValidSamples);
    }

    if (gotValidSamples)
        ProcessAndStoreRadianceSample(_PatchIrradiances, _PatchStatistics, patchIdx, radianceSampleMean, _ShortHysteresis);
}
