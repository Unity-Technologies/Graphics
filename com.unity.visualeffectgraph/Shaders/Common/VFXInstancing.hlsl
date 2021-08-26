#ifndef ALIGNED_SYSTEM_CAPACITY
#error Make sure that VFXGlobalInclude is pasted before VFXInstancing.hlsl is included
#endif

uint GetThreadId(uint3 groupId, uint3 groupThreadId, uint nbThreadPerGroup, uint dispatchWidth)
{
    return groupThreadId.x + groupId.x * nbThreadPerGroup + groupId.y * dispatchWidth * nbThreadPerGroup;
}

//Binary search in Inclusive Prefix sum
//Returns the index among the found group
uint BinarySearchPrefixSum(uint id, StructuredBuffer<uint> prefixSum, uint bufferSize, inout uint foundId)
{
    uint left = 0;
    uint right = max(bufferSize, 1) - 1;
    while (left <= right)
    {
        foundId = (int)((left + right) / 2);
        if (id < prefixSum[foundId])
        {
            if (foundId == 0 || id >= prefixSum[max(foundId - 1, 0)])
                break;
            right = foundId - 1;
        }
        else
        {
            left = foundId + 1;
        }
    }

    //return prefixSum[foundId] - (foundId > 0 ? prefixSum[foundId-1] : 0); //group size
    return id - (foundId > 0 ? prefixSum[foundId-1] : 0);
}

uint GetInstanceIndexFromVertexId(uint vertexId)
{
    return 0; //Not implemented
}

uint GetInstanceIndexFromThreadId(uint threadId, uint nbMax)
{
    //nbMax != ALIGNED_SYSTEM_CAPACITY for two reasons :
    // * nbMax can be equal to capacity, not aligned_capacity
    // * nbMax can be m_TotalSpawnCount in the case of immortal particles (without GPU events)
    return threadId / nbMax;
}
uint GetIndexInInstance(uint threadId, uint instanceIndex, uint nbMax)
{
    return threadId - instanceIndex * nbMax;
}

uint GetIndexInAttributeBuffer(uint instanceIndex, uint indexInInstance)
{
    return instanceIndex * ALIGNED_SYSTEM_CAPACITY + indexInInstance;
} //current "index"

