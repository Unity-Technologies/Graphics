#ifndef THREADING
#define THREADING

///
/// Compute Shader Threading Utilities
///
/// This file is intended to provide a portable implementation of the wave-level operations in DirectX Shader Model 6.0.
///
/// The functions in this file will automatically resolve to native intrinsics when possible.
/// A fallback groupshared memory implementation is used when native support is not available.
///
/// Usage:
///
/// To use this file, define all required preprocessor symbols and then include this file in your compute shader.
///
/// Required Preprocessor Symbols:
///
/// THREADING_BLOCK_SIZE
/// - The size of the compute shader's flattened thread group size
///
/// Optional Preprocessor Symbols:
///
/// THREADING_WAVE_SIZE
/// - The size of a wave within the compute shader
/// - This symbol MUST be defined when authoring shader code that requires a specific wave size for correctness!
///
/// THREADING_FORCE_WAVE_EMULATION
/// - If defined, forces usage of the fallback groupshared memory implementation
///

#ifndef THREADING_BLOCK_SIZE
#error THREADING_BLOCK_SIZE must be defined as the flattened thread group size.
#endif

// The emulation path is automatically enabled when we're running on hardware that doesn't meet minimum requirements.
//
// In order to use the non-emulated path, the current device must have native support for wave-level operations.
// If THREADING_WAVE_SIZE is provided, then the device's wave size must also match the size specified by THREADING_WAVE_SIZE.
//
// The emulation path can also be forced on via the THREADING_FORCE_WAVE_EMULATION preprocessor symbol for debug/testing purposes.
#define _THREADING_IS_HW_SUPPORTED (defined(UNITY_HW_SUPPORTS_WAVE) && (!defined(THREADING_WAVE_SIZE) || (defined(UNITY_HW_WAVE_SIZE) && (UNITY_HW_WAVE_SIZE == THREADING_WAVE_SIZE))))
#define _THREADING_ENABLE_WAVE_EMULATION (!_THREADING_IS_HW_SUPPORTED || defined(THREADING_FORCE_WAVE_EMULATION))
#define _THREADING_GROUP_BALLOT_DWORDS ((THREADING_BLOCK_SIZE + 31u) / 32u)

namespace Threading
{
    struct Wave
    {
        // Unfortunately 'private' is a reserved keyword in HLSL.
        uint indexG;
        uint indexW;
#if _THREADING_ENABLE_WAVE_EMULATION
        uint indexL;
        uint offset; // Per-wave offset into LDS scratch space.
#endif

        uint GetIndex();

        void Init(uint groupIndex);

        #define DECLARE_API_FOR_TYPE(TYPE) \
            bool AllEqual(TYPE v); \
            TYPE Product(TYPE v); \
            TYPE Sum(TYPE v); \
            TYPE Max(TYPE v); \
            TYPE Min(TYPE v); \
            TYPE InclusivePrefixSum(TYPE v); \
            TYPE InclusivePrefixProduct(TYPE v); \
            TYPE PrefixSum(TYPE v); \
            TYPE PrefixProduct(TYPE v); \
            TYPE ReadLaneAt(TYPE v, uint i); \
            TYPE ReadLaneFirst(TYPE v); \

        // Currently just support scalars.
        DECLARE_API_FOR_TYPE(uint)
        DECLARE_API_FOR_TYPE(int)
        DECLARE_API_FOR_TYPE(float)

        // The following intrinsics need only be declared once.
        uint  GetLaneCount();
        uint  GetLaneIndex();
        bool  IsFirstLane();
        bool  AllTrue(bool v);
        bool  AnyTrue(bool v);
        uint4 Ballot(bool v);
        uint  CountBits(bool v);
        uint  PrefixCountBits(bool v);
        uint  And(uint v);
        uint  Or(uint v);
        uint  Xor(uint v);
    };

    struct GroupBallot
    {
        uint dwords[_THREADING_GROUP_BALLOT_DWORDS];

        uint CountBits()
        {
            uint result = 0;

            [unroll]
            for (uint dwordIndex = 0; dwordIndex < _THREADING_GROUP_BALLOT_DWORDS; ++dwordIndex)
            {
                result += countbits(dwords[dwordIndex]);
            }

            return result;
        }
    };

    struct Group
    {
        uint  groupIndex  : SV_GroupIndex;
        uint3 groupID     : SV_GroupID;
        uint3 dispatchID  : SV_DispatchThreadID;

        Wave GetWave()
        {
            Wave wave;
            {
                wave = (Wave)0;
                wave.Init(groupIndex);
            }
            return wave;
        }

        // Lane remap which is safe for both portability (different wave sizes up to 128) and for 2D wave reductions.
        //  6543210
        //  =======
        //  ..xx..x
        //  yy..yy.
        // Details,
        //  LANE TO 8x16 MAPPING
        //  ====================
        //  00 01 08 09 10 11 18 19
        //  02 03 0a 0b 12 13 1a 1b
        //  04 05 0c 0d 14 15 1c 1d
        //  06 07 0e 0f 16 17 1e 1f
        //  20 21 28 29 30 31 38 39
        //  22 23 2a 2b 32 33 3a 3b
        //  24 25 2c 2d 34 35 3c 3d
        //  26 27 2e 2f 36 37 3e 3f
        //  .......................
        //  ... repeat the 8x8 ....
        //  .... pattern, but .....
        //  .... for 40 to 7f .....
        //  .......................
        // NOTE: This function is only intended to be used with one dimensional thread groups
        uint2 RemapLaneTo8x16()
        {
            // Note the BFIs used for MSBs have "strange offsets" due to leaving space for the LSB bits replaced in the BFI.
            return uint2(BitFieldInsert(1u, groupIndex, BitFieldExtract(groupIndex, 2u, 3u)),
                BitFieldInsert(3u, BitFieldExtract(groupIndex, 1u, 2u), BitFieldExtract(groupIndex, 3u, 4u)));
        }

        uint GetWaveCount();

        #define DECLARE_API_FOR_TYPE_GROUP(TYPE) \
            bool AllEqual(TYPE v); \
            TYPE Product(TYPE v); \
            TYPE Sum(TYPE v); \
            TYPE Max(TYPE v); \
            TYPE Min(TYPE v); \
            TYPE InclusivePrefixSum(TYPE v); \
            TYPE InclusivePrefixProduct(TYPE v); \
            TYPE PrefixSum(TYPE v); \
            TYPE PrefixProduct(TYPE v); \
            TYPE ReadThreadAt(TYPE v, uint i); \
            TYPE ReadThreadFirst(TYPE v); \
            TYPE ReadThreadShuffle(TYPE v, uint i); \

        // Currently just support scalars.
        DECLARE_API_FOR_TYPE_GROUP(uint)
        DECLARE_API_FOR_TYPE_GROUP(int)
        DECLARE_API_FOR_TYPE_GROUP(float)

        // The following intrinsics need only be declared once.
        uint  GetThreadCount();
        uint  GetThreadIndex();
        bool  IsFirstThread();
        bool  AllTrue(bool v);
        bool  AnyTrue(bool v);
        GroupBallot Ballot(bool v);
        uint  CountBits(bool v);
        uint  PrefixCountBits(bool v);
        uint  And(uint v);
        uint  Or(uint v);
        uint  Xor(uint v);
    };
}

#if _THREADING_ENABLE_WAVE_EMULATION
    #include "ThreadingEmuImpl.hlsl"
#else
    #include "ThreadingSM6Impl.hlsl"
#endif

#endif
