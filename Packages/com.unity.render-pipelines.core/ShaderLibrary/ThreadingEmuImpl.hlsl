#ifndef THREADING_EMU_IMPL
#define THREADING_EMU_IMPL

// If the user didn't specify a wave size, we assume that their code is "wave size independent" and that they don't
// care which size is actually used. In this case, we automatically select an arbitrary size for them since the
// emulation logic depends on having *some* known size.
#ifndef THREADING_WAVE_SIZE
#define THREADING_WAVE_SIZE 32
#endif

namespace Threading
{
    // Currently we only cover scalar types as at the time of writing this utility library we only needed emulation for those.
    // Support for vector types is currently not there but can be added as needed (and this comment removed).
    groupshared uint g_Scratch[THREADING_BLOCK_SIZE];

#define EMULATED_WAVE_REDUCE(TYPE, OP) \
    g_Scratch[indexG] = asuint(v); \
    GroupMemoryBarrierWithGroupSync(); \
    [unroll] \
    for (uint s = THREADING_WAVE_SIZE / 2u; s > 0u; s >>= 1u) \
    { \
        if (indexL < s) \
            g_Scratch[indexG] = asuint(as##TYPE(g_Scratch[indexG]) OP as##TYPE(g_Scratch[indexG + s])); \
        GroupMemoryBarrierWithGroupSync(); \
    } \
    return as##TYPE(g_Scratch[offset]); \

#define EMULATED_WAVE_REDUCE_CMP(TYPE, OP) \
    g_Scratch[indexG] = asuint(v); \
    GroupMemoryBarrierWithGroupSync(); \
    [unroll] \
    for (uint s = THREADING_WAVE_SIZE / 2u; s > 0u; s >>= 1u) \
    { \
        if (indexL < s) \
            g_Scratch[indexG] = asuint(OP(as##TYPE(g_Scratch[indexG]), as##TYPE(g_Scratch[indexG + s]))); \
        GroupMemoryBarrierWithGroupSync(); \
    } \
    return as##TYPE(g_Scratch[offset]); \

#define EMULATED_WAVE_PREFIX(TYPE, OP, FILL_VALUE) \
    g_Scratch[indexG] = asuint(v); \
    GroupMemoryBarrierWithGroupSync(); \
    [unroll] \
    for (uint s = 1u; s < THREADING_WAVE_SIZE; s <<= 1u) \
    { \
        TYPE nv = FILL_VALUE; \
        if (indexL >= s) \
        { \
            nv = as##TYPE(g_Scratch[indexG - s]); \
        } \
        nv = as##TYPE(g_Scratch[indexG]) OP nv; \
        GroupMemoryBarrierWithGroupSync(); \
        g_Scratch[indexG] = asuint(nv); \
        GroupMemoryBarrierWithGroupSync(); \
    } \
    TYPE result = FILL_VALUE; \
    if (indexL > 0u) \
        result = as##TYPE(g_Scratch[indexG - 1]); \
    return result; \

    uint Wave::GetIndex() { return indexW; }

    void Wave::Init(uint groupIndex)
    {
        indexG = groupIndex;
        indexW = indexG / THREADING_WAVE_SIZE;
        indexL = indexG & (THREADING_WAVE_SIZE - 1);
        offset = indexW * THREADING_WAVE_SIZE;
    }

    // WARNING:
    // These emulated functions do not emulate the execution mask.
    // So they WILL produce incorrect results if you have divergent lanes.

    #define DEFINE_API_FOR_TYPE(TYPE)                                                                                                                             \
        bool Wave::AllEqual(TYPE v)                 { bool isEqual = (ReadLaneFirst(v) == v); GroupMemoryBarrierWithGroupSync(); return AllTrue(isEqual);       } \
        TYPE Wave::Product(TYPE v)                  { EMULATED_WAVE_REDUCE(TYPE, *)                                                                             } \
        TYPE Wave::Sum(TYPE v)                      { EMULATED_WAVE_REDUCE(TYPE, +)                                                                             } \
        TYPE Wave::Max(TYPE v)                      { EMULATED_WAVE_REDUCE_CMP(TYPE, max)                                                                       } \
        TYPE Wave::Min(TYPE v)                      { EMULATED_WAVE_REDUCE_CMP(TYPE, min)                                                                       } \
        TYPE Wave::InclusivePrefixSum (TYPE v)      { return PrefixSum(v) + v;                                                                                  } \
        TYPE Wave::InclusivePrefixProduct (TYPE v)  { return PrefixProduct(v) * v;                                                                              } \
        TYPE Wave::PrefixSum (TYPE v)               { EMULATED_WAVE_PREFIX(TYPE, +, (TYPE)0)                                                                    } \
        TYPE Wave::PrefixProduct (TYPE v)           { EMULATED_WAVE_PREFIX(TYPE, *, (TYPE)1)                                                                    } \
        TYPE Wave::ReadLaneAt(TYPE v, uint i)       { g_Scratch[indexG] = asuint(v); GroupMemoryBarrierWithGroupSync(); return as##TYPE(g_Scratch[offset + i]); } \
        TYPE Wave::ReadLaneFirst(TYPE v)            { return ReadLaneAt(v, 0u);                                                                                 } \
        TYPE Wave::ReadLaneShuffle(TYPE v, uint i)  { return ReadLaneAt(v, i);                                                                                  } \

    // Currently just support scalars.
    DEFINE_API_FOR_TYPE(uint)
    DEFINE_API_FOR_TYPE(int)
    DEFINE_API_FOR_TYPE(float)

    // The following emulated functions need only be declared once.
    uint  Wave::GetLaneCount()          { return THREADING_WAVE_SIZE;   }
    uint  Wave::GetLaneIndex()          { return indexL;                }
    bool  Wave::IsFirstLane()           { return indexL == 0u;          }
    bool  Wave::AllTrue(bool v)         { return And(v) != 0u;          }
    bool  Wave::AnyTrue(bool v)         { return Or (v) != 0u;          }
    uint  Wave::PrefixCountBits(bool v) { return PrefixSum((uint)v);    }
    uint  Wave::And(uint v)             { EMULATED_WAVE_REDUCE(uint, &) }
    uint  Wave::Or (uint v)             { EMULATED_WAVE_REDUCE(uint, |) }
    uint  Wave::Xor(uint v)             { EMULATED_WAVE_REDUCE(uint, ^) }

    uint4 Wave::Ballot(bool v)
    {
        uint indexDw = indexL % 32u;
        uint offsetDw = (indexL / 32u) * 32u;
        uint indexScratch = offset + offsetDw + indexDw;

        g_Scratch[indexG] = v << indexDw;

        GroupMemoryBarrierWithGroupSync();

        [unroll]
        for (uint s = min(THREADING_WAVE_SIZE / 2u, 16u); s > 0u; s >>= 1u)
        {
            if (indexDw < s)
                g_Scratch[indexScratch] = g_Scratch[indexScratch] | g_Scratch[indexScratch + s];

            GroupMemoryBarrierWithGroupSync();
        }

        uint4 result = uint4(g_Scratch[offset], 0, 0, 0);

#if THREADING_WAVE_SIZE > 32
        result.y = g_Scratch[offset + 32];
#endif

#if THREADING_WAVE_SIZE > 64
        result.z = g_Scratch[offset + 64];
#endif

#if THREADING_WAVE_SIZE > 96
        result.w = g_Scratch[offset + 96];
#endif

        return result;
    }

    uint Wave::CountBits(bool v)
    {
        uint4 ballot = Ballot(v);

        uint result = countbits(ballot.x);

#if THREADING_WAVE_SIZE > 32
        result += countbits(ballot.y);
#endif

#if THREADING_WAVE_SIZE > 64
        result += countbits(ballot.z);
#endif

#if THREADING_WAVE_SIZE > 96
        result += countbits(ballot.w);
#endif

        return result;
    }

#define EMULATED_GROUP_REDUCE(TYPE, OP) \
    g_Scratch[groupIndex] = asuint(v); \
    GroupMemoryBarrierWithGroupSync(); \
    [unroll] \
    for (uint s = THREADING_BLOCK_SIZE / 2u; s > 0u; s >>= 1u) \
    { \
        if (groupIndex < s) \
            g_Scratch[groupIndex] = asuint(as##TYPE(g_Scratch[groupIndex]) OP as##TYPE(g_Scratch[groupIndex + s])); \
        GroupMemoryBarrierWithGroupSync(); \
    } \
    return as##TYPE(g_Scratch[0]); \

#define EMULATED_GROUP_REDUCE_CMP(TYPE, OP) \
    g_Scratch[groupIndex] = asuint(v); \
    GroupMemoryBarrierWithGroupSync(); \
    [unroll] \
    for (uint s = THREADING_BLOCK_SIZE / 2u; s > 0u; s >>= 1u) \
    { \
        if (groupIndex < s) \
            g_Scratch[groupIndex] = asuint(OP(as##TYPE(g_Scratch[groupIndex]), as##TYPE(g_Scratch[groupIndex + s]))); \
        GroupMemoryBarrierWithGroupSync(); \
    } \
    return as##TYPE(g_Scratch[0]); \

#define EMULATED_GROUP_PREFIX(TYPE, OP, FILL_VALUE) \
    g_Scratch[groupIndex] = asuint(v); \
    GroupMemoryBarrierWithGroupSync(); \
    [unroll] \
    for (uint s = 1u; s < THREADING_BLOCK_SIZE; s <<= 1u) \
    { \
        TYPE nv = FILL_VALUE; \
        if (groupIndex >= s) \
        { \
            nv = as##TYPE(g_Scratch[groupIndex - s]); \
        } \
        nv = as##TYPE(g_Scratch[groupIndex]) OP nv; \
        GroupMemoryBarrierWithGroupSync(); \
        g_Scratch[groupIndex] = asuint(nv); \
        GroupMemoryBarrierWithGroupSync(); \
    } \
    TYPE result = FILL_VALUE; \
    if (groupIndex > 0u) \
        result = as##TYPE(g_Scratch[groupIndex - 1]); \
    return result; \

    uint Group::GetWaveCount()
    {
        return THREADING_BLOCK_SIZE / THREADING_WAVE_SIZE;
    }

    #define DEFINE_API_FOR_TYPE_GROUP(TYPE)                                                                                                                     \
        bool Group::AllEqual(TYPE v)                  { bool isEqual = (ReadThreadFirst(v) == v); GroupMemoryBarrierWithGroupSync(); return AllTrue(isEqual); } \
        TYPE Group::Product(TYPE v)                   { EMULATED_GROUP_REDUCE(TYPE, *)                                                                        } \
        TYPE Group::Sum(TYPE v)                       { EMULATED_GROUP_REDUCE(TYPE, +)                                                                        } \
        TYPE Group::Max(TYPE v)                       { EMULATED_GROUP_REDUCE_CMP(TYPE, max)                                                                  } \
        TYPE Group::Min(TYPE v)                       { EMULATED_GROUP_REDUCE_CMP(TYPE, min)                                                                  } \
        TYPE Group::InclusivePrefixSum (TYPE v)       { return PrefixSum(v) + v;                                                                              } \
        TYPE Group::InclusivePrefixProduct (TYPE v)   { return PrefixProduct(v) * v;                                                                          } \
        TYPE Group::PrefixSum (TYPE v)                { EMULATED_GROUP_PREFIX(TYPE, +, (TYPE)0)                                                               } \
        TYPE Group::PrefixProduct (TYPE v)            { EMULATED_GROUP_PREFIX(TYPE, *, (TYPE)1)                                                               } \
        TYPE Group::ReadThreadAt(TYPE v, uint i)      { g_Scratch[groupIndex] = asuint(v); GroupMemoryBarrierWithGroupSync(); return as##TYPE(g_Scratch[i]);  } \
        TYPE Group::ReadThreadFirst(TYPE v)           { return ReadThreadAt(v, 0u);                                                                           } \
        TYPE Group::ReadThreadShuffle(TYPE v, uint i) { return ReadThreadAt(v, i);                                                                            } \

    // Currently just support scalars.
    DEFINE_API_FOR_TYPE_GROUP(uint)
    DEFINE_API_FOR_TYPE_GROUP(int)
    DEFINE_API_FOR_TYPE_GROUP(float)

    // The following emulated functions need only be declared once.
    uint  Group::GetThreadCount()        { return THREADING_BLOCK_SIZE;   }
    uint  Group::GetThreadIndex()        { return groupIndex;             }
    bool  Group::IsFirstThread()         { return groupIndex == 0u;       }
    bool  Group::AllTrue(bool v)         { return And(v) != 0u;           }
    bool  Group::AnyTrue(bool v)         { return Or (v) != 0u;           }
    uint  Group::PrefixCountBits(bool v) { return PrefixSum((uint)v);     }
    uint  Group::And(uint v)             { EMULATED_GROUP_REDUCE(uint, &) }
    uint  Group::Or (uint v)             { EMULATED_GROUP_REDUCE(uint, |) }
    uint  Group::Xor(uint v)             { EMULATED_GROUP_REDUCE(uint, ^) }

    GroupBallot Group::Ballot(bool v)
    {
        uint indexDw = groupIndex % 32u;
        uint offsetDw = (groupIndex / 32u) * 32u;
        uint indexScratch = offsetDw + indexDw;

        g_Scratch[groupIndex] = v << indexDw;

        GroupMemoryBarrierWithGroupSync();

        [unroll]
        for (uint s = min(THREADING_BLOCK_SIZE / 2u, 16u); s > 0u; s >>= 1u)
        {
            if (indexDw < s)
                g_Scratch[indexScratch] = g_Scratch[indexScratch] | g_Scratch[indexScratch + s];

            GroupMemoryBarrierWithGroupSync();
        }

        GroupBallot ballot = (GroupBallot)0;

        // Explicitly mark this loop as "unroll" to avoid warnings about assigning to an array reference
        [unroll]
        for (uint dwordIndex = 0; dwordIndex < _THREADING_GROUP_BALLOT_DWORDS; ++dwordIndex)
        {
            ballot.dwords[dwordIndex] = g_Scratch[dwordIndex * 32];
        }

        return ballot;
    }

    uint Group::CountBits(bool v)
    {
        return Ballot(v).CountBits();
    }
}
#endif
