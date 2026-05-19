#ifndef SURFACE_CACHE_PATCH_UTIL
#define SURFACE_CACHE_PATCH_UTIL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
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

    struct PatchCounterSet
    {
        // Layout
        // 0x000000FF: Update count.
        // 0x0000FF00: Rank.
        // 0xFFFF0000: Last access frame.
        uint data;
    };

    struct PatchStatisticsSet
    {
        float3 mean;
        float3 variance;
        PatchCounterSet counters;
    };

    struct VolumeParamSet
    {
        uint spatialResolution;
        float voxelMinSize;
        float3 targetPos;
        StructuredBuffer<int3> cascadeOffsets;
        uint cascadeCount;
    };

    uint ModuloDistance(uint a, uint b, uint modulo)
    {
        uint dif = max(a, b) - min(a, b);
        return min(dif, modulo - dif);
    }

    uint GetFramesSinceLastAccess(uint currentFrameIdx, uint patchLastAccessFrame)
    {
        // Here we take into account that last access frame index is in [0, 2^16-1].
        // We use that the last frame index can never be later than current frame index.
        const uint modulo = 65536; // 2^16
        return ModuloDistance(
            currentFrameIdx % modulo,
            patchLastAccessFrame,
            modulo);
    }

    void Reset(out PatchCounterSet set)
    {
        set.data = 0;
    }

    uint GetUpdateCount(PatchCounterSet set)
    {
        return set.data & 0xFF;
    }

    uint GetRank(PatchCounterSet set)
    {
        return (set.data & 0xFF00) >> 8;
    }

    void SetRank(inout PatchCounterSet set, uint rank)
    {
        set.data = (rank << 8) | (set.data & 0xFFFF00FF);
    }

    uint GetLastAccessFrame(PatchCounterSet set)
    {
        return set.data >> 16;
    }

    void SetUpdateCount(inout PatchCounterSet set, uint updateCount)
    {
        set.data = updateCount | (set.data & 0xFFFFFF00);
    }

    void SetLastAccessFrame(inout PatchCounterSet set, uint lastAccessFrame)
    {
        set.data = (lastAccessFrame << 16) | (set.data & 0xFFFF);
    }

    bool IsEqual(PatchCounterSet a, PatchCounterSet b)
    {
        return a.data == b.data;
    }

    void WriteLastFrameAccess(RWStructuredBuffer<PatchUtil::PatchStatisticsSet> statisticsSets, uint patchIdx, uint frameIdx)
    {
        PatchCounterSet counterSet = statisticsSets[patchIdx].counters;
        SetLastAccessFrame(counterSet, frameIdx);
        statisticsSets[patchIdx].counters = counterSet;
    }

    float GetVoxelSize(float voxelMinSize, uint cascadeIdx)
    {
        return voxelMinSize * (1u << cascadeIdx);
    }

    float2 OctWrap(float2 v)
    {
        return (1.0 - abs(v.yx)) * VECTOR_LOGIC_SELECT(v.xy >= 0.0, 1.0, -1.0);
    }

    float2 SphereToSquare(float3 n)
    {
        n /= (abs(n.x) + abs(n.y) + abs(n.z));
        n.xy = VECTOR_LOGIC_SELECT(n.z >= 0.0, n.xy, OctWrap(n.xy));
        n.xy = n.xy * 0.5 + 0.5;
        return n.xy;
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

        const uint2 angularSquarePos = min(uint2(angularResolution - 1, angularResolution - 1), SphereToSquare(rotatedDirection) * angularResolution);
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
                const float3 centerRelativePositionVolumeSpace = queryPos / cascadeVoxelSize - cascadeOffset;
                resolution.positionVolumeSpace = centerRelativePositionVolumeSpace + halfVolumeSize;
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

    bool ReadHemisphericalIrradiance(PatchIrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, uint spatialResolution, uint cascadeIdx, uint3 volumeSpacePosition, float3 worldNormal, out SphericalHarmonics::RGBL1 resultIrradiance)
    {
        const uint directionIdx = GetDirectionIndex(worldNormal, volumeAngularResolution);
        const uint cellIdx = GetCellIndex(cascadeIdx, volumeSpacePosition, directionIdx, spatialResolution, volumeAngularResolution);

        bool resultBool = false;
        const uint patchIdx = cellPatchIndices[cellIdx];
        resultIrradiance = (SphericalHarmonics::RGBL1)0; // Setting value only to silence shader compilation warning.
        if (patchIdx != invalidPatchIndex)
        {
            resultIrradiance = patchIrradiances[patchIdx];
            resultBool = true;
        }
        return resultBool;
    }

    uint FindPatchIndex(VolumeParamSet volumeParams, CellPatchIndexBufferType cellPatchIndices, float3 worldPosition, float3 worldNormal)
    {
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, volumeParams);
        if (posResolution.isValid())
        {
            const uint directionIdx = GetDirectionIndex(worldNormal, volumeAngularResolution);
            const uint3 positionStorageSpace = ConvertVolumeSpaceToStorageSpace(posResolution.positionVolumeSpace, volumeParams.spatialResolution, volumeParams.cascadeOffsets[posResolution.cascadeIdx]);
            const uint cellIdx = GetCellIndex(posResolution.cascadeIdx, positionStorageSpace, directionIdx, volumeParams.spatialResolution, volumeAngularResolution);
            const uint patchIdx = cellPatchIndices[cellIdx];
            if (patchIdx != invalidPatchIndex)
            {
                return patchIdx;
            }
            else
            {
                return invalidPatchIndex;
            }
        }
        else
        {
            return invalidPatchIndex;
        }
    }

    uint FindPatchIndexAndUpdateLastAccess(VolumeParamSet volumeParams, CellPatchIndexBufferType cellPatchIndices, RWStructuredBuffer<PatchUtil::PatchStatisticsSet> patchStatisticSets, float3 worldPosition, float3 worldNormal, uint frameIdx)
    {
        const uint patchIdx = FindPatchIndex(volumeParams, cellPatchIndices, worldPosition, worldNormal);
        if (patchIdx != invalidPatchIndex)
        {
            WriteLastFrameAccess(patchStatisticSets, patchIdx, frameIdx);
        }
        return patchIdx;
    }

    bool ReadHemisphericalIrradiance(PatchIrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, VolumeParamSet volumeParams, float3 worldPosition, float3 worldNormal, uint startCascadeIdx, out SphericalHarmonics::RGBL1 resultIrradiance)
    {
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, volumeParams, startCascadeIdx);
        bool resultBool = false;

        resultIrradiance = (SphericalHarmonics::RGBL1)0; // Theoretically not required but added to silence a shader compilation warning.

        if (posResolution.isValid())
        {
            const uint3 positionStorageSpace = ConvertVolumeSpaceToStorageSpace(posResolution.positionVolumeSpace, volumeParams.spatialResolution, volumeParams.cascadeOffsets[posResolution.cascadeIdx]);
            resultBool = ReadHemisphericalIrradiance(patchIrradiances, cellPatchIndices, volumeParams.spatialResolution, posResolution.cascadeIdx, positionStorageSpace, worldNormal, resultIrradiance);
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

    float3 ReadPlanarIrradiance(PatchIrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, uint spatialResolution, uint cascadeIdx, uint3 volumeSpacePosition, float3 worldNormal)
    {
        SphericalHarmonics::RGBL1 resultIrradiance;
        bool resultBool = ReadHemisphericalIrradiance(patchIrradiances, cellPatchIndices, spatialResolution, cascadeIdx, volumeSpacePosition, worldNormal, resultIrradiance);
        if (resultBool)
            return EvalIrradiance(resultIrradiance, worldNormal);
        else
            return invalidIrradiance;
    }

    float3 ReadPlanarIrradiance(PatchIrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, VolumeParamSet volumeParams, float3 worldPosition, float3 worldNormal)
    {
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, volumeParams);
        if (posResolution.isValid())
        {
            const uint3 positionStorageSpace = ConvertVolumeSpaceToStorageSpace(posResolution.positionVolumeSpace, volumeParams.spatialResolution, volumeParams.cascadeOffsets[posResolution.cascadeIdx]);
            return ReadPlanarIrradiance(patchIrradiances, cellPatchIndices, volumeParams.spatialResolution, posResolution.cascadeIdx, positionStorageSpace, worldNormal);
        }
        else
        {
            return invalidIrradiance;
        }
    }

    void MarkInvalid(inout SphericalHarmonics::RGBL1 irradiance)
    {
        irradiance.l0 = -1.0f;
    }

    bool IsValid(inout SphericalHarmonics::RGBL1 irradiance)
    {
        return all(irradiance.l0 != -1.0f);
    }

    PatchStatisticsSet InitPatchStatistics(float3 irradianceSeed, uint frameIndex, uint rank)
    {
        PatchStatisticsSet stats;
        stats.mean = irradianceSeed;
        stats.variance = 0;
        Reset(stats.counters);
        SetLastAccessFrame(stats.counters, frameIndex);
        SetRank(stats.counters, rank);

        return stats;
    }
}

#endif
