#define PATCH_UTIL_USE_RW_IRRADIANCE_BUFFER

#include "Common.hlsl"
#include "PathTracing.hlsl"
#include "Estimation.hlsl"
#include "RingBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/PseudoRandom.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"

struct Sample
{
    float3 direction;
};

struct Reservoir
{
    Sample sample;
    float weightSum;

    void Init()
    {
        sample = (Sample)0;
        weightSum = 0.0f;
    }

    void Update(in Sample newSample, float weight, float u)
    {
        weightSum += weight;
        if (u * weightSum < weight)
            sample = newSample;
    }
};

StructuredBuffer<uint> _RingConfigBuffer;
RWStructuredBuffer<SphericalHarmonics::RGBL1> _PatchIrradiances;
RWStructuredBuffer<PatchUtil::PatchStatisticsSet> _PatchStatistics;
StructuredBuffer<PatchUtil::PatchGeometry> _PatchGeometries;
StructuredBuffer<uint> _CellPatchIndices;
StructuredBuffer<int3> _CascadeOffsets;
RWStructuredBuffer<SphericalHarmonics::ScalarL2> _PatchAccumulatedLuminances;
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
uint _MultiBounce;
uint _CandidateCount;
float _TargetFunctionUpdateWeight;
uint _RingConfigOffset;
float _ShortHysteresis;
float3 _VolumeTargetPos;
float _VolumeVoxelMinSize;
float _MaterialAtlasTexelSize; // The size of 1 texel in the atlases above
float _AlbedoBoost;
float3 _DirectionalLightDirection;
float3 _DirectionalLightIntensity;

void ProcessAndStoreLuminanceSample(RWStructuredBuffer<SphericalHarmonics::ScalarL2> patchLuminances, uint patchIdx, SphericalHarmonics::ScalarL2 luminanceSample, float updateWeight)
{
    const SphericalHarmonics::ScalarL2 oldLuminance = patchLuminances[patchIdx];
    SphericalHarmonics::ScalarL2 output = SphericalHarmonics::Lerp(oldLuminance, luminanceSample, updateWeight);
    patchLuminances[patchIdx] = output;
}

float TargetFunction(Sample sample, SphericalHarmonics::ScalarL2 accumulatedPatchLuminance)
{
    float luminance = SphericalHarmonics::Eval(accumulatedPatchLuminance, sample.direction);
    if (luminance < FLT_EPSILON)
    {
        luminance = 0.05 * accumulatedPatchLuminance.l0 * SphericalHarmonics::y0 + FLT_EPSILON;
    }
    return luminance;
}

SphericalHarmonics::ScalarL2 EstimateFromLuminanceSample(float luminanceSample, float3 rayDirection)
{
    SphericalHarmonics::ScalarL2 estimate;
    estimate.l0 = luminanceSample * SphericalHarmonics::y0;
    estimate.l1s[0] = luminanceSample * (SphericalHarmonics::y1Constant * rayDirection.y);
    estimate.l1s[1] = luminanceSample * (SphericalHarmonics::y1Constant * rayDirection.z);
    estimate.l1s[2] = luminanceSample * (SphericalHarmonics::y1Constant * rayDirection.x);
    estimate.l2s[0] = luminanceSample * (SphericalHarmonics::y20Constant * rayDirection.x * rayDirection.y);
    estimate.l2s[1] = luminanceSample * (SphericalHarmonics::y21Constant * rayDirection.y * rayDirection.z);
    estimate.l2s[2] = luminanceSample * (SphericalHarmonics::y22Constant * (3.0f * rayDirection.z * rayDirection.z - 1.0f));
    estimate.l2s[3] = luminanceSample * (SphericalHarmonics::y23Constant * rayDirection.x * rayDirection.z);
    estimate.l2s[4] = luminanceSample * (0.5 * SphericalHarmonics::y24Constant * (rayDirection.x * rayDirection.x - rayDirection.y * rayDirection.y));
    return estimate;
}

SphericalHarmonics::RGBL1 EstimateFromSampleAndWeight(float3 radianceSample, float3 rayDirection, float weight)
{
    SphericalHarmonics::RGBL1 estimate;
    estimate.l0 = radianceSample * SphericalHarmonics::y0;
    estimate.l1s[0] = radianceSample * SphericalHarmonics::y1Constant * rayDirection.y;
    estimate.l1s[1] = radianceSample * SphericalHarmonics::y1Constant * rayDirection.z;
    estimate.l1s[2] = radianceSample * SphericalHarmonics::y1Constant * rayDirection.x;
    SphericalHarmonics::MulMut(estimate, weight);
    return estimate;
}

void Estimate(UnifiedRT::DispatchInfo dispatchInfo)
{
    uint patchIdx = dispatchInfo.dispatchThreadID.x;

    if (!RingBuffer::IsPositionInUse(_RingConfigBuffer, _RingConfigOffset, patchIdx))
        return;

    uint candidateCount = _CandidateCount;
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_RayTracingAccelerationStructure);

    Reservoir reservoir;
    reservoir.Init();

    QrngPcg4D rng;
    rng.Init(uint2(patchIdx, 0), _FrameIdx);

    const PatchUtil::PatchGeometry patchGeo = _PatchGeometries[patchIdx];
    const SphericalHarmonics::ScalarL2 accumulatedPatchLuminance = _PatchAccumulatedLuminances[patchIdx];

    for(uint candidateIdx = 0; candidateIdx < candidateCount; ++candidateIdx)
    {
        Sample sample;
        sample.direction = UniformHemisphereSample(float2(rng.GetFloat(0), rng.GetFloat(1)), patchGeo.normal);
        float invCandidateWeight = 2.0f * PI;
        float candidateWeight = TargetFunction(sample, accumulatedPatchLuminance) * invCandidateWeight;
        reservoir.Update(sample, candidateWeight, rng.GetFloat(2));

        rng.NextSample();
    }

    const float outputWeight = reservoir.weightSum / (TargetFunction(reservoir.sample, accumulatedPatchLuminance) * candidateCount);

    UnifiedRT::Ray ray;
    ray.direction = reservoir.sample.direction;
    ray.origin = OffsetRayOrigin(patchGeo.position, patchGeo.normal);
    ray.tMin = 0;
    ray.tMax = FLT_MAX;

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

    const float3 radianceSample = IncomingEnviromentAndDirectionalBounceAndMultiBounceRadiance(
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

    // If we hit the backface of a water-tight piece of geometry we do nothing. This is to prevent accumulating "false" darkness
    // which can give artifacts if a patch reappears after temporarily being inside moving geometry.
    // If we hit the backface of geometry which is not water tight, then this most likely a user authoring problem.
    if (all(radianceSample != invalidRadiance))
    {
        float luminance = dot(radianceSample, float3(0.2126f, 0.7152f, 0.0722f));
        SphericalHarmonics::ScalarL2 luminanceEstimate = EstimateFromLuminanceSample(luminance, reservoir.sample.direction);
        ProcessAndStoreLuminanceSample(_PatchAccumulatedLuminances, patchIdx, luminanceEstimate, _TargetFunctionUpdateWeight);

        const SphericalHarmonics::RGBL1 estimate = EstimateFromSampleAndWeight(radianceSample, reservoir.sample.direction, outputWeight);
        ProcessAndStoreRadianceSample(_PatchIrradiances, _PatchStatistics, patchIdx, estimate, _ShortHysteresis);
    }
}
