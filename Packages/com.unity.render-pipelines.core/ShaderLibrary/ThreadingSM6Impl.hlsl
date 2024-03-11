#ifndef THREADING_SM6_IMPL
#define THREADING_SM6_IMPL

namespace Threading
{
    // Currently we only cover scalar types as at the time of writing this utility library we only needed emulation for those.
    // Support for vector types is currently not there but can be added as needed (and this comment removed).
    groupshared uint g_Scratch[THREADING_BLOCK_SIZE];

    uint Wave::GetIndex() { return indexW; }

    void Wave::Init(uint groupIndex)
    {
        indexG = groupIndex;
        indexW = indexG / GetLaneCount();
    }

    // Note: The HLSL intrinsics should be correctly replaced by console-specific intrinsics by our API library.
    #define DEFINE_API_FOR_TYPE(TYPE)                                                     \
        bool Wave::AllEqual(TYPE v)                 { return WaveActiveAllEqual(v);     } \
        TYPE Wave::Product(TYPE v)                  { return WaveActiveProduct(v);      } \
        TYPE Wave::Sum(TYPE v)                      { return WaveActiveSum(v);          } \
        TYPE Wave::Max(TYPE v)                      { return WaveActiveMax(v);          } \
        TYPE Wave::Min(TYPE v)                      { return WaveActiveMin(v);          } \
        TYPE Wave::InclusivePrefixSum (TYPE v)      { return WavePrefixSum(v) + v;      } \
        TYPE Wave::InclusivePrefixProduct (TYPE v)  { return WavePrefixProduct(v) * v;  } \
        TYPE Wave::PrefixSum(TYPE v)                { return WavePrefixSum(v);          } \
        TYPE Wave::PrefixProduct(TYPE v)            { return WavePrefixProduct(v);      } \
        TYPE Wave::ReadLaneAt(TYPE v, uint i)       { return WaveReadLaneAt(v, i);      } \
        TYPE Wave::ReadLaneFirst(TYPE v)            { return WaveReadLaneFirst(v);      } \

    // Currently just support scalars.
    DEFINE_API_FOR_TYPE(uint)
    DEFINE_API_FOR_TYPE(int)
    DEFINE_API_FOR_TYPE(float)

    // The following intrinsics need only be declared once.
    uint  Wave::GetLaneCount()          { return WaveGetLaneCount();     }
    uint  Wave::GetLaneIndex()          { return WaveGetLaneIndex();     }
    bool  Wave::IsFirstLane()           { return WaveIsFirstLane();      }
    bool  Wave::AllTrue(bool v)         { return WaveActiveAllTrue(v);   }
    bool  Wave::AnyTrue(bool v)         { return WaveActiveAnyTrue(v);   }
    uint4 Wave::Ballot(bool v)          { return WaveActiveBallot(v);    }
    uint  Wave::CountBits(bool v)       { return WaveActiveCountBits(v); }
    uint  Wave::PrefixCountBits(bool v) { return WavePrefixCountBits(v); }
    uint  Wave::And(uint v)             { return WaveActiveBitAnd(v);    }
    uint  Wave::Or (uint v)             { return WaveActiveBitOr(v);     }
    uint  Wave::Xor(uint v)             { return WaveActiveBitXor(v);    }

#define EMULATED_GROUP_REDUCE(TYPE, OP) \
    GroupMemoryBarrierWithGroupSync(); \
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
    GroupMemoryBarrierWithGroupSync(); \
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
    GroupMemoryBarrierWithGroupSync(); \
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
        return THREADING_BLOCK_SIZE / WaveGetLaneCount();
    }

    #define DEFINE_API_FOR_TYPE_GROUP(TYPE)                                                                                                                                                       \
        bool Group::AllEqual(TYPE v)                  { return AllTrue(ReadThreadFirst(v) == v);                                                                                                } \
        TYPE Group::Product(TYPE v)                   { EMULATED_GROUP_REDUCE(TYPE, *)                                                                                                          } \
        TYPE Group::Sum(TYPE v)                       { EMULATED_GROUP_REDUCE(TYPE, +)                                                                                                          } \
        TYPE Group::Max(TYPE v)                       { EMULATED_GROUP_REDUCE_CMP(TYPE, max)                                                                                                    } \
        TYPE Group::Min(TYPE v)                       { EMULATED_GROUP_REDUCE_CMP(TYPE, min)                                                                                                    } \
        TYPE Group::InclusivePrefixSum (TYPE v)       { return PrefixSum(v) + v;                                                                                                                } \
        TYPE Group::InclusivePrefixProduct (TYPE v)   { return PrefixProduct(v) * v;                                                                                                            } \
        TYPE Group::PrefixSum (TYPE v)                { EMULATED_GROUP_PREFIX(TYPE, +, (TYPE)0)                                                                                                 } \
        TYPE Group::PrefixProduct (TYPE v)            { EMULATED_GROUP_PREFIX(TYPE, *, (TYPE)1)                                                                                                 } \
        TYPE Group::ReadThreadAt(TYPE v, uint i)      { GroupMemoryBarrierWithGroupSync(); g_Scratch[groupIndex] = asuint(v); GroupMemoryBarrierWithGroupSync(); return as##TYPE(g_Scratch[i]); } \
        TYPE Group::ReadThreadFirst(TYPE v)           { return ReadThreadAt(v, 0u);                                                                                                             } \
        TYPE Group::ReadThreadShuffle(TYPE v, uint i) { return ReadThreadAt(v, i);                                                                                                              } \

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

        GroupMemoryBarrierWithGroupSync();

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
