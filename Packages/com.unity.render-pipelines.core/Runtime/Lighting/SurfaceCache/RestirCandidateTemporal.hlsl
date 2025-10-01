#include "Common.hlsl"
#include "RingBuffer.hlsl"
#include "PathTracing.hlsl"
#include "RestirEstimation.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/PseudoRandom.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"

StructuredBuffer<uint> _RingConfigBuffer;
StructuredBuffer<SphericalHarmonics::RGBL1> _PatchIrradiances;
StructuredBuffer<PatchUtil::PatchGeometry> _PatchGeometries;
StructuredBuffer<uint> _CellPatchIndices;
StructuredBuffer<int3> _CascadeOffsets;
RWStructuredBuffer<Realization> _PatchRealizations;
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
uint _GridSize;
uint _CascadeCount;
float _VoxelMinSize;
uint _MultiBounce;
float _ConfidenceCap;
uint _ValidationFrameInterval;
uint _RingConfigOffset;
float3 _GridTargetPos;
float _MaterialAtlasTexelSize; // The size of 1 texel in the atlases above
float _AlbedoBoost;
float3 _DirectionalLightDirection;
float3 _DirectionalLightIntensity;

void GenerateCandidateAndResampleTemporally(UnifiedRT::DispatchInfo dispatchInfo)
{
    uint patchIdx = dispatchInfo.dispatchThreadID.x;

    if (RingBuffer::IsPositionUnused(_RingConfigBuffer, _RingConfigOffset, patchIdx))
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_RayTracingAccelerationStructure);
    const Realization oldRealization = _PatchRealizations[patchIdx];
    Realization newRealization = (Realization)0; // Initializing only to silence shader warning.

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

    const bool isCandidateFrame = _FrameIdx % _ValidationFrameInterval != _ValidationFrameInterval - 1;

    // We expect this branch to have relatively low divergence because almost all realizations either
    // have a non-zero weight or they have been allocated this frame. Therefore, realizations that are
    // nearby in the buffer should almost always agree on whether their weight is zero or not.
    if (isCandidateFrame || oldRealization.weight == 0.0f)
    {
        Reservoir reservoir;

        QrngPcg4D rng;
        const uint candidateFrameIdx = _FrameIdx - _FrameIdx / _ValidationFrameInterval;
        rng.Init(uint2(patchIdx, 0), candidateFrameIdx);

        {
            const PatchUtil::PatchGeometry patchGeo = _PatchGeometries[patchIdx];

            UnifiedRT::Ray ray;
            ray.origin = OffsetRayOrigin(patchGeo.position, patchGeo.normal);
            ray.direction = UniformHemisphereSample(float2(rng.GetFloat(0), rng.GetFloat(1)), patchGeo.normal);
            ray.tMin = 0;
            ray.tMax = FLT_MAX;

            UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, UnifiedRT::kRayFlagNone);
            Sample sample = (Sample)0; // Initializing value only to silence shader compilation warning.
            sample.rayOrigin = ray.origin;
            sample.rayDirection = ray.direction;
            if (hitResult.IsValid())
            {
                const UnifiedRT::InstanceData hitInstance = UnifiedRT::GetInstance(hitResult.instanceID);
                const SurfaceGeometry hitGeo = FetchSurfaceGeometry(hitInstance, hitResult);
                const MaterialPool::MaterialProperties mat = MaterialPool::LoadMaterialProperties(
                    _MaterialEntries,
                    _AlbedoTextures,
                    sampler_AlbedoTextures,
                    _TransmissionTextures,
                    sampler_TransmissionTextures,
                    _EmissionTextures,
                    sampler_EmissionTextures,
                    _AlbedoBoost,
                    _MaterialAtlasTexelSize,
                    hitInstance.userMaterialID,
                    hitGeo.uv0,
                    hitGeo.uv1);

                sample.sampleType = SAMPLE_TYPE_HIT;
                sample.hitPointOrDirection = hitGeo.position;

                if (!hitResult.isFrontFace)
                {
                    // If we hit the backface of a water-tight piece of geometry we do nothing. This is to prevent accumulating "false" darkness
                    // which can give artifacts if a patch reappears after temporarily being inside moving geometry.
                    // If we hit the backface of geometry which is not water tight, then this most likely a user authoring problem.
                    return;
                }
                else
                {
                    sample.radiance = SampleOutgoingRadianceAssumingLambertianBrdf(
                            hitGeo.position,
                            hitGeo.normal,
                            dispatchInfo,
                            accelStruct,
                            _DirectionalLightDirection,
                            _DirectionalLightIntensity,
                            _MultiBounce,
                            _PatchIrradiances,
                            _CellPatchIndices,
                            _GridSize,
                            _CascadeOffsets,
                            _GridTargetPos,
                            _CascadeCount,
                            _VoxelMinSize,
                            mat.baseColor,
                            mat.emissive);
                }
            }
            else
            {
                sample.sampleType = SAMPLE_TYPE_ENV;
                sample.radiance = _EnvironmentCubemap.SampleLevel(sampler_EnvironmentCubemap, ray.direction, 0);
                sample.hitPointOrDirection = ray.direction;
            }

            float invCandidateWeight = 2.0f * PI;
            float candidateWeight = TargetFunction(sample) * invCandidateWeight;
            reservoir.InitWithSample(sample, 1.0f, candidateWeight);
        }

        {
            float cappedConfidence = min(oldRealization.confidence, _ConfidenceCap);
            float candidateWeight = TargetFunction(oldRealization.sample) * oldRealization.weight * cappedConfidence;
            reservoir.Update(oldRealization.sample, cappedConfidence, candidateWeight, rng.GetFloat(2));
        }

        newRealization = CreateRealizationFromReservoir(reservoir);
    }
    else
    {
        UnifiedRT::Ray ray;
        ray.origin = oldRealization.sample.rayOrigin;
        ray.direction = oldRealization.sample.rayDirection;
        ray.tMin = 0;
        ray.tMax = FLT_MAX;

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
            _GridSize,
            _CascadeOffsets,
            _GridTargetPos,
            _CascadeCount,
            _VoxelMinSize);

        if (all(radianceSample != invalidRadianceSample))
        {
            newRealization = oldRealization;
            if (all(radianceSample + oldRealization.sample.radiance != 0.0f))
            {
                newRealization.confidence *= max(0, 1.0f - length(abs(radianceSample - oldRealization.sample.radiance) / (radianceSample + oldRealization.sample.radiance)) / length(float3(1,1,1)));
            }
        }
    }

    _PatchRealizations[patchIdx] = newRealization;
}
