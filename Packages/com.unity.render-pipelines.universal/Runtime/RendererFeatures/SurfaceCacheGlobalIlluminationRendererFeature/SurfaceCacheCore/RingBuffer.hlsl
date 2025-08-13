#ifndef RING_BUFFER
#define RING_BUFFER

#include "Common.hlsl"

#if defined(RING_BUFFER_USE_RW_RING_CONFIG_BUFFER)
#define RingConfigBufferType RWStructuredBuffer<uint>
#else
#define RingConfigBufferType StructuredBuffer<uint>
#endif

namespace RingBuffer
{
    static const uint countConfigIndex = 0;
    static const uint startConfigIndex = 1;
    static const uint endConfigIndex = 2;

    struct Config
    {
        uint count;
        uint start;
        uint end;
    };

    uint GetCount(RingConfigBufferType config)
    {
        return config[countConfigIndex];
    }

    Config LoadConfig(RingConfigBufferType buffer, uint offset)
    {
        Config config;
        config.count = buffer[offset + countConfigIndex];
        config.start = buffer[offset + startConfigIndex] % patchCapacity;
        config.end = buffer[offset + endConfigIndex] % patchCapacity;
        return config;
    }

    bool IsPositionOutsideRangeAssumingStartNotEqualEnd(uint start, uint end, uint pos)
    {
        if(start < end)
            return !(start <= pos && pos < end);
        else if(end < start)
            return end <= pos && pos < start;
        else
            return true; // expected to never be taken
    }

    bool IsPositionUnused(Config config, uint index)
    {
        if (config.count == patchCapacity)
            return config.count != patchCapacity;
        else
            return IsPositionOutsideRangeAssumingStartNotEqualEnd(config.start, config.end, index);
    }

    bool IsPositionUnused(RingConfigBufferType buffer, uint bufferOffset, uint index)
    {
        Config config = LoadConfig(buffer, bufferOffset);
        return IsPositionUnused(config, index);
    }
}

#endif
