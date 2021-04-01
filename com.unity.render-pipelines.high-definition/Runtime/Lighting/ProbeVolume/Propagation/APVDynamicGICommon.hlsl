#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#ifndef DECODE_SH
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
#endif

#define SHL1 0
#define SHL2 1
#define OUTPUT_TYPE SHL1

#define OUTPUT_IS_SH (OUTPUT_TYPE == SHL1 || OUTPUT_TYPE == SHL2)



RW_TEXTURE3D(float4, _RWAPVResL0_L1Rx);
RW_TEXTURE3D(float4, _RWAPVResL1G_L1Ry);
RW_TEXTURE3D(float4, _RWAPVResL1B_L1Rz);

#if OUTPUT_TYPE == SHL2
// For L2
RW_TEXTURE3D(float4, _RWAPVResL2_0);
RW_TEXTURE3D(float4, _RWAPVResL2_1);
RW_TEXTURE3D(float4, _RWAPVResL2_2);
RW_TEXTURE3D(float4, _RWAPVResL2_3);
#endif

#define AXIS_COUNT 14 // TODO: Generate from C#
#define MAX_ALLOWED_CHUNKS_PER_CELL 32 // Each chunk in the default situation is 8192 probes. 32 Maximum means 262144 probes per cell, which should be enough.

#define MAX_FLOAT4_CHUNKS_IDX MAX_ALLOWED_CHUNKS_PER_CELL / 4

CBUFFER_START(APVDynamicGI)
float4 _DynamicGIParams0;
float4 _DynamicGIParams1;
float4 _RayAxis[AXIS_COUNT];
float4 _ChunkIndices[MAX_FLOAT4_CHUNKS_IDX];
CBUFFER_END


#define _MaxNeighbourRayDist    _DynamicGIParams0.x
#define _MinValidNeighbourDist  _DynamicGIParams0.y
#define _RayBias                _DynamicGIParams0.z
#define _ProbeCount             (uint)_DynamicGIParams0.w
#define _PoolDimensions         (uint3)_DynamicGIParams1.xyz
#define _PoolWidth              (uint)_DynamicGIParams1.x
#define _ChunkSize              (uint)_DynamicGIParams1.w




// -------------------------------------------------------------
// Input data utils
// -------------------------------------------------------------


uint GetChunkIndex(uint probeIndex)
{
    uint chunkIndex = probeIndex / _ChunkSize;
    // A bit of a yikes. TODO: Do better.
    return (uint)_ChunkIndices[chunkIndex / 4][chunkIndex % 4];
}

// TODO: Can be optimized and simplified a fair bit. Compiler should be able to help, but let's help it instead.
uint3 ProbeIndexToTexLocation(uint probeIndex)
{
    uint3 outIndex = 0;

    uint chunkFlatIdx = GetChunkIndex(probeIndex);
    uint2 chunkStart = 0;
    chunkStart.x = chunkFlatIdx % _PoolWidth;
    chunkStart.y = chunkFlatIdx / (4 * _PoolWidth);
    uint indexInChunk = probeIndex % _ChunkSize;
    // They are laid out in memory as 4x4x4 blocks (bricks)
    uint brickIdx = indexInChunk / 64;
    uint indexInBrick = indexInChunk % 64;
    uint2 brickStart = uint2(chunkStart.x + brickIdx * 4, chunkStart.y * 4);

    outIndex.z = indexInBrick / 16;

    uint indexInSlice = indexInBrick % 16;
    outIndex.x = brickStart.x + (indexInSlice % 4);
    outIndex.y = brickStart.y + (indexInSlice / 4);

    return outIndex;
}

float4 UnpackAlbedoAndDistance(uint packedVal)
{
    float4 outVal;
    outVal.r = ((packedVal >> 0) & 255) / 255.0f;
    outVal.g = ((packedVal >> 8) & 255) / 255.0f;
    outVal.b = ((packedVal >> 16) & 255) / 255.0f;

    outVal.a = ((packedVal >> 24) & 255) / 255.0f;
    outVal.a *= _MaxNeighbourRayDist * sqrt(3.0f);

    return outVal;
}

float3 UnpackNormal(uint packedVal)
{
    float3 N888;
    N888.r = ((packedVal >> 0) & 255) / 255.0f;
    N888.g = ((packedVal >> 8) & 255) / 255.0f;
    N888.b = ((packedVal >> 16) & 255) / 255.0f;

    float2 octNormalWS = Unpack888ToFloat2(N888);

    return UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);
}

float4 UnpackAxis(uint packedVal)
{
    // Info is in most significant 8 bit
    uint data = (packedVal >> 24);

    const float diagonalDist = sqrt(3.0f);
    const float diagonal = rcp(diagonalDist);


    // Get if is diagonal or primary axis
    uint axisType = data & 64;
    int z = (int)((data >> 4) & 3);
    int y = (int)((data >> 2) & 3);
    int x = (int)(data & 3);

    const float channelVal = axisType == 0 ? 1 : diagonal;

    return float4((x - 1) * channelVal, (y - 1) * channelVal, (z - 1) * channelVal, axisType == 0 ? 1 : diagonalDist);
}

void UnpackIndicesAndValidity(uint packedVal, out uint probeIndex, out uint axisIndex, out float validity)
{
    // 5 bits for axis Index
    axisIndex = packedVal & 31;
    validity = ((packedVal >> 5) & 255) / 255.0f;
    probeIndex = (packedVal >> 13) & 524287;
}

// -------------------------------------------------------------
// Output related utils
// -------------------------------------------------------------

// Wrap it in a struct to be able to swap in and out methodology without having to change function signatures
struct OutputRepresentation
{
#if OUTPUT_IS_SH

    float3 L0;
    float3 L1_0; // -1
    float3 L1_1; //  0
    float3 L1_2; //  1
#if OUTPUT_TYPE == SHL2
    float3 L2_0; // -2
    float3 L2_1; // -1
    float3 L2_2; //  0
    float3 L2_3; //  1
    float3 L2_4; //  2
#endif


    float3 Evaluate(float3 direction)
    {
        float4 shAr = float4(L1_0.r, L1_1.r, L1_2.r, L0.r);
        float4 shAg = float4(L1_0.g, L1_1.g, L1_2.g, L0.g);
        float4 shAb = float4(L1_0.b, L1_1.b, L1_2.b, L0.b);
        float3 L1Eval = SHEvalLinearL1(direction, shAr.xyz, shAg.xyz, shAb.xyz);

        float3 output = L0;
        output += L1Eval;

#if OUTPUT_TYPE == SHL2
        output += SHEvalLinearL2(direction, float4(L2_0.r, L2_1.r, L2_2.r, L2_3.r),
                                            float4(L2_0.g, L2_1.g, L2_2.g, L2_3.g),
                                            float4(L2_0.b, L2_1.b, L2_2.b, L2_3.b),
                                            float4(L2_4, 1.0f));
#endif
        return output;
    }

#endif
};

OutputRepresentation SumOutput(OutputRepresentation o1, float scale1, OutputRepresentation o2, float scale2)
{
    OutputRepresentation output;
#if OUTPUT_IS_SH
    output.L0 = o1.L0 * scale1 + o2.L0 * scale2;
    output.L1_0 = o1.L1_0 * scale1 + o2.L1_0 * scale2;
    output.L1_1 = o1.L1_1 * scale1 + o2.L1_1 * scale2;
    output.L1_2 = o1.L1_2 * scale1 + o2.L1_2 * scale2;
#if OUTPUT_TYPE == SHL2
    output.L2_0 = o1.L2_0 * scale1 + o2.L2_0 * scale2;
    output.L2_1 = o1.L2_1 * scale1 + o2.L2_1 * scale2;
    output.L2_2 = o1.L2_2 * scale1 + o2.L2_2 * scale2;
    output.L2_3 = o1.L2_3 * scale1 + o2.L2_3 * scale2;
    output.L2_4 = o1.L2_4 * scale1 + o2.L2_4 * scale2;
#endif

#endif

    return output;
}

OutputRepresentation SumOutput(OutputRepresentation o1, OutputRepresentation o2)
{
    return SumOutput(o1, 1.0f, o2, 1.0f);
}

#if OUTPUT_IS_SH

float3 EncodeSHL1(float l0, float3 l1)
{
    if (l0 < 1e-3f) return 0.0f;
    l1 = saturate(l1);
    const float l1scale = 2; // 3/(2*sqrt(3)) * 2
    return l1 / (l0 * l1scale * 2.0f) + 0.5f;
}

float4 EncodeSHL2(float l0, float4 l2, inout float l2_c)
{
    if (l0 < 1e-3f) return 0.0f;
    l2 = saturate(l2);
    float l2scale = 3.5777088f; // 4/sqrt(5) * 2

    l2_c = l2_c / (l0 * l2scale * 2.0f) + 0.5f;
    return l2 / (l0 * l2scale * 2.0f) + 0.5f;
}


#endif


// For now we only output to APV SH L1, but this function must be modified if a different representation is needed.
void WriteToOutput(OutputRepresentation outputSpace, uint probeIndex, bool encode)
{
    uint3 indexInPool = ProbeIndexToTexLocation(probeIndex);

#if OUTPUT_IS_SH

    float3 L1_R = float3(outputSpace.L1_0.x, outputSpace.L1_1.x, outputSpace.L1_2.x);
    float3 L1_G = float3(outputSpace.L1_0.y, outputSpace.L1_1.y, outputSpace.L1_2.y);
    float3 L1_B = float3(outputSpace.L1_0.z, outputSpace.L1_1.z, outputSpace.L1_2.z);

#if OUTPUT_TYPE == SHL2
    float4 L2_R = float4(outputSpace.L2_0.r, outputSpace.L2_1.r, outputSpace.L2_2.r, outputSpace.L2_3.r);
    float4 L2_G = float4(outputSpace.L2_0.g, outputSpace.L2_1.g, outputSpace.L2_2.g, outputSpace.L2_3.g);
    float4 L2_B = float4(outputSpace.L2_0.b, outputSpace.L2_1.b, outputSpace.L2_2.b, outputSpace.L2_3.b);
    float4 L2_C = float4(outputSpace.L2_4, 1.0f);
#endif

    if (encode)
    {
        L1_R = EncodeSHL1(outputSpace.L0.x, L1_R);
        L1_G = EncodeSHL1(outputSpace.L0.y, L1_G);
        L1_B = EncodeSHL1(outputSpace.L0.z, L1_B);

#if OUTPUT_TYPE == SHL2
        L2_R = EncodeSHL2(outputSpace.L0.r, L2_R, L2_C.r);
        L2_G = EncodeSHL2(outputSpace.L0.g, L2_G, L2_C.g);
        L2_B = EncodeSHL2(outputSpace.L0.b, L2_B, L2_C.b);
#endif
    }

    if (AnyIsInf(outputSpace.L0) || AnyIsNaN(outputSpace.L0))
    {
        L1_R = 0;
        L1_G = 0;
        L1_B = 0;
        outputSpace.L0 = 0;
#if OUTPUT_TYPE == SHL2
        L2_R = 0;
        L2_G = 0;
        L2_B = 0;
        L2_C = 0;
#endif
    }

    _RWAPVResL0_L1Rx[indexInPool] = float4(outputSpace.L0.xyz, L1_R.x);
    _RWAPVResL1G_L1Ry[indexInPool] = float4(L1_G.xyz, L1_R.y);
    _RWAPVResL1B_L1Rz[indexInPool] = float4(L1_B.xyz, L1_R.z);
#if OUTPUT_TYPE == SHL2
    _RWAPVResL2_0[indexInPool] = L2_R;
    _RWAPVResL2_1[indexInPool] = L2_G;
    _RWAPVResL2_2[indexInPool] = L2_B;
    _RWAPVResL2_3[indexInPool] = L2_C;
#endif

#endif
}

void WriteToOutput(OutputRepresentation outputSpace, uint probeIndex)
{
    WriteToOutput(outputSpace, probeIndex, true);
}

void SamplesToRepresentation(inout OutputRepresentation output, float4 L0_L1Rx, float4 L1G_L1Ry, float4 L1B_L1Rz,
#if OUTPUT_TYPE == SHL2
    float4 L2_R, float4 L2_G, float4 L2_B, L2_C,
#endif

    bool decode)
{
    output.L0 = L0_L1Rx.xyz;
    float3 l1_R = float3(L0_L1Rx.w, L1G_L1Ry.w, L1B_L1Rz.w);
    float3 l1_G = L1G_L1Ry.xyz;
    float3 l1_B = L1B_L1Rz.xyz;

    if (decode)
    {
        //// decode the L1 coefficients
        l1_R = DecodeSH(output.L0.r, l1_R);
        l1_G = DecodeSH(output.L0.g, l1_G);
        l1_B = DecodeSH(output.L0.b, l1_B);
#if OUTPUT_TYPE == SHL2
        DecodeSH_L2(output.L0, L2_R, L2_G, L2_B, L2_C);
#endif
    }

    output.L1_0 = float3(l1_R.x, l1_G.x, l1_B.x);
    output.L1_1 = float3(l1_R.y, l1_G.y, l1_B.y);
    output.L1_2 = float3(l1_R.z, l1_G.z, l1_B.z);
#if OUTPUT_TYPE == SHL2
    output.L2_0 = float3(L2_R.x, L2_G.x, L2_B.x);
    output.L2_1 = float3(L2_R.y, L2_G.y, L2_B.y);
    output.L2_2 = float3(L2_R.z, L2_G.z, L2_B.z);
    output.L2_3 = float3(L2_R.w, L2_G.w, L2_B.w);
    output.L2_4 = L2_C;

#endif
}

void SamplesToRepresentation(inout OutputRepresentation output, float4 L0_L1Rx, float4 L1G_L1Ry, float4 L1B_L1Rz
#if OUTPUT_TYPE == SHL2
    , float4 L2_R, float4 L2_G, float4 L2_B, float4 L2_C
#endif
)
{
    SamplesToRepresentation(output, L0_L1Rx, L1G_L1Ry, L1B_L1Rz,
#if OUTPUT_TYPE == SHL2
        L2_R, L2_G, L2_B, L2_C,
#endif
        true);
}



// -------------------------------------------------------------
// Output utils
// -------------------------------------------------------------

// Constants from SetSHEMapConstants function in the Stupid Spherical Harmonics Tricks paper:
// http://www.ppsloan.org/publications/StupidSH36.pdf
//  [SH basis coeff] * [clamped cosine convolution factor]
#define fC0 (rsqrt(PI * 4.0) * rsqrt(PI * 4.0))  // Equivalent (0.282095 * (1.0 / (2.0 * sqrtPI)))
#define fC1 (rsqrt(PI * 4.0 / 3.0) * rsqrt(PI * 3.0)) // Equivalent to (0.488603 * (sqrt ( 3.0) / ( 3.0 * sqrtPI)))
#define fC2 (rsqrt(PI * 4.0 / 15.0) * rsqrt(PI * 64.0 / 15.0)) // Equivalent to (1.092548 * (sqrt (15.0) / ( 8.0 * sqrtPI)))
#define fC3 (rsqrt(PI * 16.0 / 5.0) * rsqrt(PI * 256.0 / 5.0)) // Equivalent to (0.315392 * (sqrt ( 5.0) / (16.0 * sqrtPI)))
#define fC4 (rsqrt(PI * 16.0 / 15.0) * rsqrt(PI * 256.0 / 15.0)) // Equivalent to  (0.546274 * 0.5 * (sqrt (15.0) / ( 8.0 * sqrtPI)))

void AddToOutputRepresentation(float3 value, float3 direction, inout OutputRepresentation output)
{

#if OUTPUT_IS_SH

    static const float ConvolveCosineLobeBandFactor[] = { fC0, -fC1, fC1, -fC1, fC2, -fC2, fC3, -fC2, fC4 };

    const float kNormalization = 2.9567930857315701067858823529412f; // 16*kPI/17

    float weight = kNormalization;

    float3 L0 = value * ConvolveCosineLobeBandFactor[0] * weight;
    float3 L1_0 = -direction.y * value * ConvolveCosineLobeBandFactor[1] * weight;
    float3 L1_1 = direction.z * value * ConvolveCosineLobeBandFactor[2] * weight;
    float3 L1_2 = -direction.x * value * ConvolveCosineLobeBandFactor[3] * weight;

    output.L0 += L0;
    output.L1_0 += L1_0;
    output.L1_1 += L1_1;
    output.L1_2 += L1_2;


#if OUTPUT_TYPE == SHL2

    float3 L2_0 = direction.x * direction.y * value * ConvolveCosineLobeBandFactor[4] * weight;
    float3 L2_1 = -direction.y * direction.z * value * ConvolveCosineLobeBandFactor[5] * weight;
    float3 L2_2 = (3.0 * direction.z * direction.z - 1.0f) * value * ConvolveCosineLobeBandFactor[6] * weight;
    float3 L2_3 = -direction.x * direction.z * value * ConvolveCosineLobeBandFactor[7] * weight;
    float3 L2_4 = (direction.x * direction.x - direction.y * direction.y) * value * ConvolveCosineLobeBandFactor[8] * weight;

    output.L2_0 += L2_0;
    output.L2_1 += L2_1;
    output.L2_2 += L2_2;
    output.L2_3 += L2_3;
    output.L2_4 += L2_4;
#endif

#endif
}
