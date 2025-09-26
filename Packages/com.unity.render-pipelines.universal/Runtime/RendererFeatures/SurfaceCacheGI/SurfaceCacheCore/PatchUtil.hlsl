#ifndef SURFACE_CACHE_PATCH_UTIL
#define SURFACE_CACHE_PATCH_UTIL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "VectorLogic.hlsl"
#include "Common.hlsl"
#include "RingBuffer.hlsl"

#if defined(PATCH_UTIL_USE_RW_IRRADIANCE_BUFFER)
#define IrradianceBufferType RWStructuredBuffer<SphericalHarmonics::RGBL1>
#else
#define IrradianceBufferType StructuredBuffer<SphericalHarmonics::RGBL1>
#endif

#if defined(PATCH_UTIL_USE_RW_CELL_INDEX_BUFFER)
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
    static const uint gridCellAngularResolution = 4; // Must match C# side.
    static const float3 invalidIrradiance = float3(-1, -1, -1);
    static const uint updateMax = 32;

    struct PatchGeometry
    {
        float3 position;
        float3 normal;
    };

    struct PatchStatisticsSet
    {
        float3 mean;
        float3 variance;
    };

    struct PatchCounterSet
    {
        uint data;
    };

    void Reset(inout PatchCounterSet set)
    {
        set.data = 0;
    }

    uint GetUpdateCount(PatchCounterSet set)
    {
        return set.data & 0xFFFF;
    }

    uint GetLastAccessFrame(PatchCounterSet set)
    {
        return set.data >> 16;
    }

    void SetUpdateCount(inout PatchCounterSet set, uint updateCount)
    {
        set.data = updateCount | (set.data & 0xFFFF0000);
    }

    void SetLastAccessFrame(inout PatchCounterSet set, uint lastAccessFrame)
    {
        set.data = (lastAccessFrame << 16) | (set.data & 0xFFFF);
    }

    bool IsEqual(PatchCounterSet a, PatchCounterSet b)
    {
        return a.data == b.data;
    }

    void WriteLastFrameAccess(RWStructuredBuffer<PatchUtil::PatchCounterSet> counterSets, uint patchIdx, uint frameIdx)
    {
        PatchCounterSet counterSet = counterSets[patchIdx];
        SetLastAccessFrame(counterSet, frameIdx);
        counterSets[patchIdx] = counterSet;
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
        n.xy = n.z >= 0.0 ? n.xy : OctWrap(n.xy);
        n.xy = n.xy * 0.5 + 0.5;
        return n.xy;
    }

    struct VolumePositionResolution
    {
        uint cascadeIdx;
        uint3 positionGridSpace;

        void markInvalid()
        {
            positionGridSpace = UINT_MAX;
        }

        bool isValid()
        {
            return all(positionGridSpace != UINT_MAX);
        }
    };

    uint GetCellIndex(uint cascadeIdx, uint3 positionStorageSpace, uint directionIndex, uint gridSize, uint angularResolution)
    {
        const uint angularResolutionSquared = angularResolution * angularResolution;
        const uint gridSizeSquared = gridSize * gridSize;

        const uint cellsPerCascade = gridSizeSquared * gridSize * angularResolutionSquared;
        const uint withinCascadeIdx = angularResolutionSquared * (positionStorageSpace.x * gridSizeSquared + positionStorageSpace.y * gridSize + positionStorageSpace.z) + directionIndex;
        return cellsPerCascade * cascadeIdx + withinCascadeIdx;
    }

    uint GetDirectionIndex(float3 direction, uint angularResolution)
    {
        // To avoid discontinuities near the cardinal axis directions, we apply an arbitrary rotation.
        // This is based on the assumption that surfaces oriented along the cardinal axis directions
        // are most likely in a scene compared to other directions.
        const float3x3 arbitraryRotation = float3x3(
            float3(0.34034f, -0.30925f, 0.888f),
            float3(-0.30925f, 0.85502f, 0.41629f),
            float3(-0.888f, -0.41629f, 0.19536f));
        const float3 rotatedDirection = mul(arbitraryRotation, direction);

        const uint2 angularSquarePos = min(uint2(3, 3), SphereToSquare(rotatedDirection) * angularResolution);
        return angularSquarePos.y * angularResolution + angularSquarePos.x;
    }

    // Unlike the regular HLSL % operator where both operands must both be signed or unsigned,
    // this function additionally supports the case where the first argument is negative and
    // the second argument is positive.
    uint3 SignedIntegerModulo(int3 x, uint modulus)
    {
        const uint3 remainder = uint3(abs(x)) % modulus;
        return VECTOR_LOGIC_SELECT(x < 0 && remainder != 0, modulus - remainder, remainder);
    }

    uint3 ConvertGridSpaceToStorageSpace(uint3 posGridSpace, uint gridSize, int3 cascadeOffset)
    {
        return SignedIntegerModulo(int3(posGridSpace) + cascadeOffset, gridSize);
    }

    uint3 ConvertStorageSpaceToGridSpace(uint3 posStorageSpace, uint gridSize, int3 cascadeOffset)
    {
        return SignedIntegerModulo(int3(posStorageSpace) - cascadeOffset, gridSize);
    }

    bool IsInsideCascade(float3 gridTargetPos, float3 queryPos, float cascadeVoxelSize, uint gridSize)
    {
        const float3 dif = gridTargetPos - queryPos;
        const float difSquaredLength = dot(dif, dif);
        // We subtract 0.5 here to account for the fact that the Grid Target Pos can move up to
        // 0.499... voxel sizes away from the cascade center in any dimension without causing the
        // cascade to move.
        const float threshold = cascadeVoxelSize * (float(gridSize) * 0.5f - 0.5f);
        const float squaredThreshold = threshold * threshold;
        return difSquaredLength < squaredThreshold;
    }

    VolumePositionResolution ResolveVolumePosition(float3 queryPos, float3 gridTargetPos, uint gridSize, StructuredBuffer<int3> cascadeOffsets, uint cascadeCount, float voxelMinSize, uint startCascadeIdx = 0)
    {
        VolumePositionResolution resolution = (VolumePositionResolution)0; // Zero initialization is strictly not required but this silences a shader compiler warning.

        resolution.markInvalid();
        const float halfGridSize = float(gridSize) * 0.5f;
        [unroll(cascadeMax)]
        for (uint cascadeIdx = startCascadeIdx; cascadeIdx < cascadeCount; ++cascadeIdx)
        {
            const float cascadeVoxelSize = GetVoxelSize(voxelMinSize, cascadeIdx);
            if (IsInsideCascade(gridTargetPos, queryPos, cascadeVoxelSize, gridSize))
            {
                const int3 cascadeOffset = cascadeOffsets[cascadeIdx];
                const float3 centerRelativePositionSpatialGridSpace = queryPos / cascadeVoxelSize - cascadeOffset;
                resolution.positionGridSpace = centerRelativePositionSpatialGridSpace + halfGridSize;
                resolution.cascadeIdx = cascadeIdx;
                break;
            }
        }

        return resolution;
    }

    int ResolveCascadeIndex(float3 gridTargetPos, float3 queryPos, uint gridSize, uint cascadeCount, float voxelMinSize)
    {
        int result = -1;
        [unroll(cascadeMax)]
        for (uint cascadeIdx = 0; cascadeIdx < cascadeCount; ++cascadeIdx)
        {
            const float cascadeVoxelSize = GetVoxelSize(voxelMinSize, cascadeIdx);
            if (IsInsideCascade(gridTargetPos, queryPos, cascadeVoxelSize, gridSize))
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

    PatchIndexResolutionResult ResolvePatchIndex(RWStructuredBuffer<uint> ringConfigBuffer, uint ringConfigOffset, RWStructuredBuffer<uint> cellPatchIndices, RWStructuredBuffer<uint> cellAllocationMarks, uint cellIdx)
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

    bool ReadHemisphericalIrradiance(IrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, uint gridSize, uint cascadeIdx, uint3 gridSpacePosition, float3 worldNormal, out SphericalHarmonics::RGBL1 resultIrradiance)
    {
        const uint directionIdx = GetDirectionIndex(worldNormal, gridCellAngularResolution);
        const uint cellIdx = GetCellIndex(cascadeIdx, gridSpacePosition, directionIdx, gridSize, gridCellAngularResolution);

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

    uint FindPatchIndex(float3 gridTargetPos, StructuredBuffer<uint> cellPatchIndices, uint gridSize, StructuredBuffer<int3> cascadeOffsets, uint cascadeCount, float voxelMinSize, float3 worldPosition, float3 worldNormal)
    {
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, gridTargetPos, gridSize, cascadeOffsets, cascadeCount, voxelMinSize);
        if (posResolution.isValid())
        {
            const uint directionIdx = GetDirectionIndex(worldNormal, gridCellAngularResolution);
            const uint3 positionStorageSpace = ConvertGridSpaceToStorageSpace(posResolution.positionGridSpace, gridSize, cascadeOffsets[posResolution.cascadeIdx]);
            const uint cellIdx = GetCellIndex(posResolution.cascadeIdx, positionStorageSpace, directionIdx, gridSize, gridCellAngularResolution);
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

    uint FindPatchIndexAndUpdateLastAccess(float3 gridTargetPos, StructuredBuffer<uint> cellPatchIndices, uint gridSize, StructuredBuffer<int3> cascadeOffsets, RWStructuredBuffer<PatchUtil::PatchCounterSet> patchCounterSets, uint cascadeCount, float voxelMinSize, float3 worldPosition, float3 worldNormal, uint frameIdx)
    {
        const uint patchIdx = FindPatchIndex(gridTargetPos, cellPatchIndices, gridSize, cascadeOffsets, cascadeCount, voxelMinSize,worldPosition, worldNormal);
        if (patchIdx != invalidPatchIndex)
        {
            WriteLastFrameAccess(patchCounterSets, patchIdx, frameIdx);
        }
        return patchIdx;
    }

    bool ReadHemisphericalIrradiance(IrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, uint gridSize, StructuredBuffer<int3> cascadeOffsets, float3 cascadeFocusPos, uint cascadeCount, float voxelMinSize, float3 worldPosition, float3 worldNormal, uint startCascadeIdx, out SphericalHarmonics::RGBL1 resultIrradiance)
    {
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, cascadeFocusPos, gridSize, cascadeOffsets, cascadeCount, voxelMinSize, startCascadeIdx);
        bool resultBool = false;

        resultIrradiance = (SphericalHarmonics::RGBL1)0; // Theoretically not required but added to silence a shader compilation warning.

        if (posResolution.isValid())
        {
            const uint3 positionStorageSpace = ConvertGridSpaceToStorageSpace(posResolution.positionGridSpace, gridSize, cascadeOffsets[posResolution.cascadeIdx]);
            resultBool = ReadHemisphericalIrradiance(patchIrradiances, cellPatchIndices, gridSize, posResolution.cascadeIdx, positionStorageSpace, worldNormal, resultIrradiance);
        }

        return resultBool;
    }

    bool ReadHemisphericalIrradiance(IrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, uint gridSize, StructuredBuffer<int3> cascadeOffsets, float3 cascadeFocusPos, uint cascadeCount, float voxelMinSize, float3 worldPosition, float3 worldNormal, out SphericalHarmonics::RGBL1 resultIrradiance)
    {
        const uint conservativeStartCascadeIdx = 0;
        return ReadHemisphericalIrradiance(
            patchIrradiances,
            cellPatchIndices,
            gridSize,
            cascadeOffsets,
            cascadeFocusPos,
            cascadeCount,
            voxelMinSize,
            worldPosition,
            worldNormal,
            conservativeStartCascadeIdx,
            resultIrradiance);
    }

    float3 ReadPlanarIrradiance(IrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, uint gridSize, uint cascadeIdx, uint3 gridSpacePosition, float3 worldNormal)
    {
        SphericalHarmonics::RGBL1 resultIrradiance;
        bool resultBool = ReadHemisphericalIrradiance(patchIrradiances, cellPatchIndices, gridSize, cascadeIdx, gridSpacePosition, worldNormal, resultIrradiance);
        if (resultBool)
            return max(0, SphericalHarmonics::Eval(resultIrradiance, worldNormal));
        else
            return invalidIrradiance;
    }

    float3 ReadPlanarIrradiance(float3 gridTargetPos, IrradianceBufferType patchIrradiances, CellPatchIndexBufferType cellPatchIndices, uint gridSize, StructuredBuffer<int3> cascadeOffsets, uint cascadeCount, float voxelMinSize, float3 worldPosition, float3 worldNormal)
    {
        VolumePositionResolution posResolution = ResolveVolumePosition(worldPosition, gridTargetPos, gridSize, cascadeOffsets, cascadeCount, voxelMinSize);
        if (posResolution.isValid())
        {
            const uint3 positionStorageSpace = ConvertGridSpaceToStorageSpace(posResolution.positionGridSpace, gridSize, cascadeOffsets[posResolution.cascadeIdx]);
            return ReadPlanarIrradiance(patchIrradiances, cellPatchIndices, gridSize, posResolution.cascadeIdx, positionStorageSpace, worldNormal);
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
}

#endif
