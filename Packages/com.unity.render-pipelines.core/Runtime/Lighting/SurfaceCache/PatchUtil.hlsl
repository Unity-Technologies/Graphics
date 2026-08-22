#ifndef SURFACE_CACHE_PATCH_UTIL
#define SURFACE_CACHE_PATCH_UTIL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "VectorLogic.hlsl"
#include "Common.hlsl"
#include "RingBuffer.hlsl"

#if defined(PATCH_UTIL_USE_RW_PATCH_IRRADIANCE_BUFFER)
#define PatchIrradianceBufferType RWStructuredBuffer<SphericalHarmonics::RGBL1>
#else
#define PatchIrradianceBufferType StructuredBuffer<SphericalHarmonics::RGBL1>
#endif

#if defined(PATCH_UTIL_USE_RW_PATCH_GEOMETRY_BUFFER)
#define PatchGeometryBufferType RWStructuredBuffer<PatchUtil::PatchGeometry>
#else
#define PatchGeometryBufferType StructuredBuffer<PatchUtil::PatchGeometry>
#endif

#if defined(PATCH_UTIL_USE_RW_PATCH_CELL_INDEX_BUFFER)
#define PatchCellIndexBufferType RWStructuredBuffer<uint>
#else
#define PatchCellIndexBufferType StructuredBuffer<uint>
#endif

#if defined(PATCH_UTIL_USE_RW_CELL_PATCH_INDEX_BUFFER)
#define CellPatchIndexBufferType RWStructuredBuffer<uint>
#else
#define CellPatchIndexBufferType StructuredBuffer<uint>
#endif

#if defined(PATCH_UTIL_USE_RW_CELL_ALLOCATION_MARK_BUFFER)
#define CellAllocationMarkBufferType RWStructuredBuffer<uint>
#else
#define CellAllocationMarkBufferType StructuredBuffer<uint>
#endif

namespace PatchUtil
{
    static const uint invalidPatchIndex = UINT_MAX; // Must match C# side.
    static const uint invalidCellIndex = UINT_MAX; // Must match C# side.
    static const uint volumeAngularResolution = 4; // Must match C# side.
    static const float3 invalidIrradiance = float3(-1, -1, -1);
    static const uint updateMax = 32;
    static const uint evictionThreshold = 60 * 4;

    struct PatchGeometry
    {
        float3 position;
        float3 normal;
    };

    struct PatchRequestState
    {
        // Layout
        // 0x000000FF: Rank.
        // 0x0000FF00: Unused.
        // 0xFFFF0000: Heartbeat.
        uint data;
    };

    struct PatchStatisticsSet
    {
        float3 mean;
        float3 variance;
        uint updateCount;
        PatchRequestState requestState;
    };

    struct VolumeParamSet
    {
        uint spatialResolution;
        float voxelMinSize;
        float3 targetPos;
        StructuredBuffer<int3> cascadeOffsets;
        uint cascadeCount;
        bool patchWarping;
    };

    uint ModuloDistance(uint a, uint b, uint modulo)
    {
        uint dif = max(a, b) - min(a, b);
        return min(dif, modulo - dif);
    }

    uint GetFramesSinceHeartbeat(uint currentFrameIdx, uint patchHeartbeat)
    {
        // Here we take into account that heartbeat frame index is in [0, 2^16-1].
        // We use that the last frame index can never be later than current frame index.
        const uint modulo = 65536; // 2^16
        return ModuloDistance(
            currentFrameIdx % modulo,
            patchHeartbeat,
            modulo);
    }

    void Reset(out PatchRequestState state)
    {
        state.data = 0;
    }

    uint GetRank(PatchRequestState state)
    {
        return state.data & 0xFF;
    }

    uint GetHeartbeat(PatchRequestState state)
    {
        return state.data >> 16;
    }

    void SetHeartbeat(inout PatchRequestState state, uint heartbeat)
    {
        state.data = (heartbeat << 16) | (state.data & 0xFFFF);
    }

    void SetRank(inout PatchRequestState state, uint rank)
    {
        state.data = rank | (state.data & 0xFFFFFF00);
    }

    bool IsEqual(PatchRequestState a, PatchRequestState b)
    {
        return a.data == b.data;
    }

    void UpdateHeartbeat(RWStructuredBuffer<PatchUtil::PatchStatisticsSet> statisticsSets, uint patchIdx, uint frameIdx)
    {
        PatchRequestState reqState = statisticsSets[patchIdx].requestState;
        SetHeartbeat(reqState, frameIdx);
        statisticsSets[patchIdx].requestState = reqState;
    }

    float GetVoxelSize(float voxelMinSize, uint cascadeIdx)
    {
        return voxelMinSize * (1u << cascadeIdx);
    }

    // Surfaces sitting exactly on a voxel boundary (e.g. a floor at y = 0) flicker because
    // floating-point noise flips neighboring samples between the two bordering cells. We displace
    // the position by a continuous, world-anchored wave before quantizing, so nearby samples (and
    // the same point across frames) resolve to a consistent voxel. Each axis is displaced by a
    // function of the *other* two, so axis-aligned planes still vary across their surface, and the
    // wave is quasi-periodic (a sum of incommensurate sines) to avoid collapsing into a regular
    // lattice. This mitigates rather than fully fixes the ambiguity.
    // See https://history.siggraph.org/learning/advances-in-spatial-hashing-a-pragmatic-approach-towards-robust-real-time-light-transport-simulation-by-gautron/
    float QuasiPeriodicWave(float t)
    {
        const float3 waves = sin(float3(t, 2.19f * t + 1.7f, 3.73f * t + 4.2f));
        return (waves.x + waves.y + waves.z) * (1.0f / 3.0f);
    }

    float3 GetVoxelWarpOffset(float3 queryPos, float voxelSize)
    {
        const float waveStrength = 0.01f;
        const float waveFrequency = 0.01f;
        // Base frequency in radians per voxel, with a different irrational multiplier per axis.
        const float3 axisFrequency = waveFrequency * float3(1.0f, 1.41421356f, 1.73205081f);
        const float3 phase = queryPos / voxelSize * axisFrequency;
        const float3 wave = float3(QuasiPeriodicWave(phase.x), QuasiPeriodicWave(phase.y), QuasiPeriodicWave(phase.z));
        const float3 offset = float3(wave.y + wave.z, wave.z + wave.x, wave.x + wave.y);
        // Each QuasiPeriodicWave is in [-1, 1], so their sum is in [-2, 2]; 0.5 keeps the max displacement at waveStrength voxels.
        return offset * (0.5f * waveStrength * voxelSize);
    }

    struct VolumePositionResolution
    {
        uint cascadeIdx;
        uint3 positionVolumeSpace;

        void markInvalid()
        {
            positionVolumeSpace = UINT_MAX;
        }

        bool isValid()
        {
            return all(positionVolumeSpace != UINT_MAX);
        }
    };

    uint GetCellIndex(uint cascadeIdx, uint3 positionStorageSpace, uint directionIndex, uint spatialResolution, uint angularResolution)
    {
        const uint angularResolutionSquared = angularResolution * angularResolution;
        const uint spatialResolutionSquared = spatialResolution * spatialResolution;

        const uint cellsPerCascade = spatialResolutionSquared * spatialResolution * angularResolutionSquared;
        const uint withinCascadeIdx = angularResolutionSquared * (positionStorageSpace.x * spatialResolutionSquared + positionStorageSpace.y * spatialResolution + positionStorageSpace.z) + directionIndex;
        return cellsPerCascade * cascadeIdx + withinCascadeIdx;
    }

    uint GetDirectionIndex(float3 direction, uint angularResolution)
    {
        // To avoid discontinuities near the cardinal axis directions, we apply an arbitrary rotation.
        // This is based on the assumption that surfaces oriented along the cardinal axis directions
        // are more likely in a scene compared to other directions.
        const float3x3 arbitraryRotation = float3x3(
            float3(0.34034f, -0.30925f, 0.888f),
            float3(-0.30925f, 0.85502f, 0.41629f),
            float3(-0.888f, -0.41629f, 0.19536f));
        const float3 rotatedDirection = mul(arbitraryRotation, direction);

        const uint2 angularSquarePos = min(uint2(angularResolution - 1, angularResolution - 1), OctahedralSphereToSquare(rotatedDirection) * angularResolution);
        return angularSquarePos.y * angularResolution + angularSquarePos.x;
    }

    // Unlike the regular HLSL % operator where both operands must both be signed or unsigned,
    // this function additionally supports the case where the first argument is negative and
    // the second argument is positive.
    uint3 SignedIntegerModulo(int3 x, uint modulus)
    {
        const uint3 remainder = uint3(abs(x)) % modulus;
        return VECTOR_LOGIC_SELECT(VECTOR_LOGIC_AND(x < 0, remainder != 0), modulus - remainder, remainder);
    }

    uint3 ConvertVolumeSpaceToStorageSpace(uint3 posVolSpace, uint spatialResolution, int3 cascadeOffset)
    {
        return SignedIntegerModulo(int3(posVolSpace) + cascadeOffset, spatialResolution);
    }

    uint3 ConvertStorageSpaceToVolumeSpace(uint3 posStorageSpace, uint spatialResolution, int3 cascadeOffset)
    {
        return SignedIntegerModulo(int3(posStorageSpace) - cascadeOffset, spatialResolution);
    }

    bool IsInsideCascade(float3 volumeTargetPos, float3 queryPos, float cascadeVoxelSize, uint volumeSpatialResolution)
    {
        const float3 dif = volumeTargetPos - queryPos;
        const float difSquaredLength = dot(dif, dif);
        // We subtract 0.5 here to account for the fact that the Volume Target Pos can move up to
        // 0.499... voxel sizes away from the cascade center in any dimension without causing the
        // cascade to move.
        const float threshold = cascadeVoxelSize * (float(volumeSpatialResolution) * 0.5f - 0.5f);
        const float squaredThreshold = threshold * threshold;
        return difSquaredLength < squaredThreshold;
    }

    VolumePositionResolution ResolveVolumePosition(float3 queryPos, VolumeParamSet volumeParams, uint startCascadeIdx = 0)
    {
        VolumePositionResolution resolution = (VolumePositionResolution)0; // Zero initialization is strictly not required but this silences a shader compiler warning.
        resolution.markInvalid();
        const float halfVolumeSize = float(volumeParams.spatialResolution) * 0.5f;

        for (uint cascadeIdx = startCascadeIdx; cascadeIdx < volumeParams.cascadeCount; ++cascadeIdx)
        {
            const float cascadeVoxelSize = GetVoxelSize(volumeParams.voxelMinSize, cascadeIdx);
            if (IsInsideCascade(volumeParams.targetPos, queryPos, cascadeVoxelSize, volumeParams.spatialResolution))
            {
                const int3 cascadeOffset = volumeParams.cascadeOffsets[cascadeIdx];
                float3 warpedQueryPos = queryPos;
                if (volumeParams.patchWarping)
                    warpedQueryPos += GetVoxelWarpOffset(queryPos, cascadeVoxelSize);
                const float3 centerRelativePositionVolumeSpace = warpedQueryPos / cascadeVoxelSize - cascadeOffset;
                const int3 positionVolumeSpaceSigned = int3(centerRelativePositionVolumeSpace + halfVolumeSize);
                resolution.positionVolumeSpace = uint3(clamp(positionVolumeSpaceSigned, 0, int(volumeParams.spatialResolution) - 1));
                resolution.cascadeIdx = cascadeIdx;
                break;
            }
        }

        return resolution;
    }

    int ResolveCascadeIndex(float3 volumeTargetPos, float3 queryPos, uint volumeSpatialResolution, uint volumeCascadeCount, float volumeVoxelMinSize)
    {
        int result = -1;
        for (uint cascadeIdx = 0; cascadeIdx < volumeCascadeCount; ++cascadeIdx)
        {
            const float cascadeVoxelSize = GetVoxelSize(volumeVoxelMinSize, cascadeIdx);
            if (IsInsideCascade(volumeTargetPos, queryPos, cascadeVoxelSize, volumeSpatialResolution))
            {
                result = cascadeIdx;
                break;
            }
        }
        return result;
    }

    static const uint patchIndexResolutionCodeLookup = 0;
    static const uint patchIndexResolutionCodeAllocationSuccess = 1;
    static const uint patchIndexResolutionCodeAllocationFailure = 2;

    struct PatchIndexResolutionResult
    {
        uint code;
        uint patchIdx;
    };

    PatchIndexResolutionResult ResolvePatchIndex(
        RWStructuredBuffer<uint> ringConfigBuffer,
        uint ringConfigOffset,
        RWStructuredBuffer<uint> cellPatchIndices,
        RWStructuredBuffer<uint> patchCellIndices,
        RWStructuredBuffer<uint> cellAllocationMarks,
        uint cellIdx)
    {
        PatchIndexResolutionResult result;
        result.patchIdx = invalidPatchIndex;

        uint existingPatchIndex = cellPatchIndices[cellIdx];
        if (existingPatchIndex != invalidPatchIndex)
        {
            result.patchIdx = existingPatchIndex;
            result.code = patchIndexResolutionCodeLookup;
        }
        else
        {
            result.code = patchIndexResolutionCodeAllocationFailure;

            uint existingAllocationMark;
            InterlockedExchange(cellAllocationMarks[cellIdx], 1, existingAllocationMark);
            if (existingAllocationMark == 0)
            {
                uint countBeforeAllocation;
                InterlockedAdd(ringConfigBuffer[ringConfigOffset + RingBuffer::countConfigIndex], 1, countBeforeAllocation);
                if (countBeforeAllocation < patchCapacity)
                {
                    uint newPatchIdx;
                    InterlockedAdd(ringConfigBuffer[ringConfigOffset + RingBuffer::endConfigIndex], 1, newPatchIdx);
                    newPatchIdx %= patchCapacity; // Here we exploit the requirement that UINT_MAX is a multiple of patchCapacity.

                    result.code = patchIndexResolutionCodeAllocationSuccess;
                    result.patchIdx = newPatchIdx;
                    cellPatchIndices[cellIdx] = newPatchIdx;
                    patchCellIndices[newPatchIdx] = cellIdx;
                }
                else
                {
                    // Allocation failed, no room. Backing out.
                    ringConfigBuffer[ringConfigOffset + RingBuffer::countConfigIndex] = patchCapacity;
                    cellAllocationMarks[cellIdx] = 0;
                }
            }
        }

        return result;
    }

    void MarkInvalid(inout SphericalHarmonics::RGBL1 irradiance)
    {
        irradiance.l0 = -1.0f;
    }

    bool IsValid(in SphericalHarmonics::RGBL1 irradiance)
    {
        return all(irradiance.l0 != -1.0f);
    }

    static const uint patchIndexLookupCodeInvalidPatchIndex = invalidPatchIndex;
    static const uint patchIndexLookupCodeOutsideVolume = UINT_MAX - 1;
    struct PatchIndexLookupResult
    {
        uint patchIdxOrCode;
    };

    uint HasValidPatchIndex(in PatchIndexLookupResult result)
    {
        return result.patchIdxOrCode != patchIndexLookupCodeInvalidPatchIndex && result.patchIdxOrCode != patchIndexLookupCodeOutsideVolume;
    }

    // You should only call this if you are sure the passed-in PatchIndexLookupResult
    // contains a patch index.
    uint GetPatchIndex(in PatchIndexLookupResult result)
    {
        return result.patchIdxOrCode;
    }

    uint LookupPatchIndex(VolumeParamSet volumeParams, CellPatchIndexBufferType cellPatchIndices, uint3 posVolSpace, uint directionIdx, uint cascadeIdx)
    {
        const uint3 positionStorageSpace = ConvertVolumeSpaceToStorageSpace(posVolSpace, volumeParams.spatialResolution, volumeParams.cascadeOffsets[cascadeIdx]);
        const uint cellIdx = GetCellIndex(cascadeIdx, positionStorageSpace, directionIdx, volumeParams.spatialResolution, volumeAngularResolution);
        const uint patchIdx = cellPatchIndices[cellIdx];
        return patchIdx;
    }

    PatchIndexLookupResult LookupPatchIndex(VolumeParamSet volumeParams, CellPatchIndexBufferType cellPatchIndices, float3 worldPosition, float3 worldNormal)
    {
        PatchIndexLookupResult result;
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, volumeParams);
        UNITY_OUT_OF_BOUNDS_BRANCH
        if (posResolution.isValid())
        {
            const uint directionIdx = GetDirectionIndex(worldNormal, volumeAngularResolution);
            result.patchIdxOrCode = LookupPatchIndex(volumeParams, cellPatchIndices, posResolution.positionVolumeSpace, directionIdx, posResolution.cascadeIdx);
        }
        else
        {
            result.patchIdxOrCode = patchIndexLookupCodeOutsideVolume;
        }
        return result;
    }

    bool ReadHemisphericalIrradiance(PatchIrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, VolumeParamSet volumeParams, float3 worldPosition, float3 worldNormal, uint startCascadeIdx, out SphericalHarmonics::RGBL1 resultIrradiance)
    {
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, volumeParams, startCascadeIdx);
        bool resultBool = false;

        resultIrradiance = (SphericalHarmonics::RGBL1)0; // Theoretically not required but added to silence a shader compilation warning.

        if (posResolution.isValid())
        {
            const uint directionIdx = GetDirectionIndex(worldNormal, volumeAngularResolution);
            const uint patchIdx = LookupPatchIndex(volumeParams, cellPatchIndices, posResolution.positionVolumeSpace, directionIdx, posResolution.cascadeIdx);
            UNITY_OUT_OF_BOUNDS_BRANCH
            if (patchIdx != invalidPatchIndex)
            {
                resultIrradiance = patchIrradiances[patchIdx];
                resultBool = true;
            }
        }

        return resultBool;
    }

    bool ReadHemisphericalIrradiance(PatchIrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, VolumeParamSet volumeParams, float3 worldPosition, float3 worldNormal, out SphericalHarmonics::RGBL1 resultIrradiance)
    {
        const uint conservativeStartCascadeIdx = 0;
        return ReadHemisphericalIrradiance(
            patchIrradiances,
            cellPatchIndices,
            volumeParams,
            worldPosition,
            worldNormal,
            conservativeStartCascadeIdx,
            resultIrradiance);
    }

    float3 EvalIrradiance(SphericalHarmonics::RGBL1 irradiance, float3 normal)
    {
        return max(0, SphericalHarmonics::Eval(irradiance, normal));
    }

    PatchStatisticsSet InitPatchStatistics(float3 irradianceSeed, uint frameIndex, uint rank)
    {
        PatchStatisticsSet stats;
        stats.mean = irradianceSeed;
        stats.variance = 0;
        stats.updateCount = 0;
        Reset(stats.requestState);
        SetHeartbeat(stats.requestState, frameIndex);
        SetRank(stats.requestState, rank);
        return stats;
    }
}

#endif
