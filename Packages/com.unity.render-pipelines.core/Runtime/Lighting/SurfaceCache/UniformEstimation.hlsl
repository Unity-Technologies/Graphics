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
StructuredBuffer<MaterialPool::MaterialEntry> _MaterialEntries;
Texture2DArray<float4> _AlbedoTextures;
Texture2DArray<float4> _TransmissionTextures;
Texture2DArray<float4> _EmissionTextures;
SamplerState sampler_EmissionTextures;
SamplerState sampler_AlbedoTextures;
SamplerState sampler_TransmissionTextures;
TextureCube<float3> _EnvironmentCubemap;
SamplerState sampler_EnvironmentCubemap;
UNIFIED_RT_DECLARE_ACCEL_STRUCT(_RayTracingAccelerationStructure);

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
float3 _DirectionalLightDirection;
float3 _DirectionalLightIntensity;

void Estimate(UnifiedRT::DispatchInfo dispatchInfo)
{
    uint patchIdx = dispatchInfo.dispatchThreadID.x;

    if (!RingBuffer::IsPositionInUse(_RingConfigBuffer, _RingConfigOffset, patchIdx))
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_RayTracingAccelerationStructure);

    QrngKronecker rng;
    rng.Init(uint2(patchIdx, 0), _FrameIdx * _SampleCount);

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

        // If we hit the backface of a water-tight piece of geometry we skip. This is to prevent accumulating "false" darkness
        // which can give artifacts if a patch reappears after temporarily being inside moving geometry.
        // If we hit the backface of geometry which is not water tight, then this most likely a user authoring problem.
        if (all(radianceSample == invalidRadianceSample))
            continue;

        validSampleCount++;

        radianceAccumulator.l0 += radianceSample * SphericalHarmonics::y0;
        radianceAccumulator.l1s[0] += radianceSample * SphericalHarmonics::y1Constant * ray.direction.y;
        radianceAccumulator.l1s[1] += radianceSample * SphericalHarmonics::y1Constant * ray.direction.z;
        radianceAccumulator.l1s[2] += radianceSample * SphericalHarmonics::y1Constant * ray.direction.x;

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
