#ifndef __VFX_THREAD_DATA
#define __VFX_THREAD_DATA

#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBufferUint.hlsl"

// Undo the macros of the same names from the legacy VFXInstancing.hlsl, brought in by VFXCommon.hlsl.
#undef instancingCurrentCount
#undef instancingActiveCount
#undef instancingBatchSize
#undef instancingCurrentOffset
#undef instancingActiveIndirectOffset
#undef instancingPrefixSumOffset

StructuredBuffer<uint> instancingIndirectAndActiveIndirect;
StructuredBuffer<uint> instancingPrefixSum;

CBUFFER_START(instancingConstantsBuffer)
    float4 instancingConstants;
    uint2 instancingBufferOffsets;
    uint dispatchWidth;
CBUFFER_END

#if defined(SHADER_STAGE_VERTEX)
UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float, _InstanceIndex)
    UNITY_DEFINE_INSTANCED_PROP(float, _InstanceActiveIndex)
UNITY_INSTANCING_BUFFER_END(PerInstance)
#endif


struct InstancingData
{
    uint currentCount;
    uint activeCount;
    uint batchSize;

    uint currentOffset;
    uint activeIndirectOffset;
    uint prefixSumOffset;

    uint currentIndex;
    uint activeIndex;
    uint instanceIndex;

    void InitFromConstants()
    {
        currentCount = asuint(instancingConstants.x);
        activeCount = asuint(instancingConstants.y);
        batchSize = asuint(instancingConstants.z);
        currentOffset = asuint(instancingConstants.w);
        activeIndirectOffset = instancingBufferOffsets.x;
        prefixSumOffset = instancingBufferOffsets.y;
    }

    void ResolveInstanceIndices()
    {
        activeIndex = currentIndex;
        [branch]
        if (currentCount < activeCount)
        {
            VFXStructuredBuffer_uint currentToActive;
            currentToActive.Init(instancingIndirectAndActiveIndirect, activeIndirectOffset, currentCount);
            currentToActive.LoadData(activeIndex, currentIndex);
        }

        instanceIndex = activeIndex;
        [branch]
        if (activeCount < batchSize)
        {
            VFXStructuredBuffer_uint activeToBatch;
            activeToBatch.Init(instancingIndirectAndActiveIndirect, 0u, batchSize);
            activeToBatch.LoadData(instanceIndex, activeIndex);
        }
    }

    uint InitVariable(uint localThreadIndex)
    {
        InitFromConstants();
        VFXStructuredBuffer_uint countsPrefixSum;
        countsPrefixSum.Init(instancingPrefixSum, prefixSumOffset + currentOffset, currentCount);
        uint index;
        currentIndex = BinarySearchPrefixSum(localThreadIndex, countsPrefixSum, index) + currentOffset;
        ResolveInstanceIndices();
        return index;
    }

    uint InitFixed(uint localThreadIndex, uint fixedInstancingStride)
    {
        InitFromConstants();
        uint index = localThreadIndex;
        currentIndex = currentOffset;

        #if defined(SHADER_STAGE_VERTEX) && !defined(UNITY_INSTANCING_ENABLED)
        // One VFX instance per draw: localThreadIndex is already the particle index.
        #else
        uint localInstanceIndex = min(localThreadIndex / fixedInstancingStride, currentCount - 1u);
        index = localThreadIndex - localInstanceIndex * fixedInstancingStride;
        currentIndex = localInstanceIndex + currentOffset;
        #endif

        #if defined(SHADER_STAGE_VERTEX)
        #ifdef UNITY_INSTANCING_ENABLED
        unity_InstanceID = currentIndex;
        #endif
        activeIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
        instanceIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceIndex));
        #else
        ResolveInstanceIndices();
        #endif

        return index;
    }
};


struct ThreadData
{
    InstancingData instancingData;
    uint index;

    void InitVariable(uint localThreadIndex)
    {
        index = instancingData.InitVariable(localThreadIndex);
    }

    void InitFixed(uint localThreadIndex, uint fixedInstancingStride)
    {
        index = instancingData.InitFixed(localThreadIndex, fixedInstancingStride);
    }
};

#endif //__VFX_THREAD_DATA
