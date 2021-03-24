#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

RW_TEXTURE3D(float4, _RWAPVResL0_L1Rx);
RW_TEXTURE3D(float4, _RWAPVResL1G_L1Ry);
RW_TEXTURE3D(float4, _RWAPVResL1B_L1Rz);


#define AXIS_COUNT 14 // TODO: Generate from C#
#define MAX_ALLOWED_CHUNKS_PER_CELL 32 // Each chunk in the default situation is 8192 probes. 32 Maximum means 262144 probes per cell, which should be enough.

#define MAX_FLOAT4_CHUNKS_IDX 32 / 4

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


#define SHL1 0
#define SHL2 1
#define OUTPUT_TYPE SHL1

#define OUTPUT_IS_SH (OUTPUT_TYPE == SHL1 || OUTPUT_TYPE == SHL2)

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

        return L1Eval + L0;
    }

#endif
};

OutputRepresentation SumOutput(OutputRepresentation o1, float scale1, OutputRepresentation o2, float scale2)
{
    OutputRepresentation output;
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

    return output;
}

OutputRepresentation SumOutput(OutputRepresentation o1, OutputRepresentation o2)
{
    return SumOutput(o1, 1.0f, o2, 1.0f);
}


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
    outVal.r = ((packedVal >> 0)  & 255) / 255.0f;
    outVal.g = ((packedVal >> 8)  & 255) / 255.0f;
    outVal.b = ((packedVal >> 16) & 255) / 255.0f;

    outVal.a = ((packedVal >> 24) & 255) / 255.0f;
    outVal.a *= _MaxNeighbourRayDist * sqrt(3.0f);

    return outVal;
}

float3 UnpackNormal(uint packedVal)
{
    float3 N888;
    N888.r = ((packedVal >> 0)  & 255) / 255.0f;
    N888.g = ((packedVal >> 8)  & 255) / 255.0f;
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

float3 EncodeSH(float l0, float3 l1)
{
    if (l0 < 1e-3f) return 0.0f;

    l1 = saturate(l1);

    const float l1scale = 2; // 3/(2*sqrt(3)) * 2
    return l1 / (l0 * l1scale * 2.0f) + 0.5f;
}

// TODO_FCC: Have this in a common place
float3 DecodeSH_(float l0, float3 l1)
{
    if (l0 < 1e-3f) return 0.0f;
    // TODO: We're working on irradiance instead of radiance coefficients
    //       Add safety margin 2 to avoid out-of-bounds values
    const float l1scale = 2; // 3/(2*sqrt(3)) * 2

    return (l1 - 0.5f) * 2.0f * l1scale * l0;
}

// For now we only output to APV SH L1, but this function must be modified if a different representation is needed.
void WriteToOutput(OutputRepresentation outputSpace, uint probeIndex, bool encode)
{
    uint3 indexInPool = ProbeIndexToTexLocation(probeIndex);

    float3 L1_R = float3(outputSpace.L1_0.x, outputSpace.L1_1.x, outputSpace.L1_2.x);
    float3 L1_G = float3(outputSpace.L1_0.y, outputSpace.L1_1.y, outputSpace.L1_2.y);
    float3 L1_B = float3(outputSpace.L1_0.z, outputSpace.L1_1.z, outputSpace.L1_2.z);

    if (encode)
    {
        L1_R = EncodeSH(outputSpace.L0.x, L1_R);
        L1_G = EncodeSH(outputSpace.L0.y, L1_G);
        L1_B = EncodeSH(outputSpace.L0.z, L1_B);
    }

    if (AnyIsInf(outputSpace.L0) || AnyIsNaN(outputSpace.L0))
    {
        L1_R = 0;
        L1_G = 0;
        L1_B = 0;
        outputSpace.L0 = 0;
    }

    _RWAPVResL0_L1Rx[indexInPool] = float4(outputSpace.L0.xyz, L1_R.x);
    _RWAPVResL1G_L1Ry[indexInPool] = float4(L1_G.xyz, L1_R.y);
    _RWAPVResL1B_L1Rz[indexInPool] = float4(L1_B.xyz, L1_R.z);
}

void WriteToOutput(OutputRepresentation outputSpace, uint probeIndex)
{
    WriteToOutput(outputSpace, probeIndex, true);
}

void SamplesToRepresentation(inout OutputRepresentation output, float4 L0_L1Rx, float4 L1G_L1Ry, float4 L1B_L1Rz, bool decode)
{
    output.L0 = L0_L1Rx.xyz;
    float3 l1_R = float3(L0_L1Rx.w, L1G_L1Ry.w, L1B_L1Rz.w);
    float3 l1_G = L1G_L1Ry.xyz;
    float3 l1_B = L1B_L1Rz.xyz;

    if (decode)
    {
        //// decode the L1 coefficients
        l1_R = DecodeSH_(output.L0.r, l1_R);
        l1_G = DecodeSH_(output.L0.g, l1_G);
        l1_B = DecodeSH_(output.L0.b, l1_B);
    }

    output.L1_0 = float3(l1_R.x, l1_G.x, l1_B.x);
    output.L1_1 = float3(l1_R.y, l1_G.y, l1_B.y);
    output.L1_2 = float3(l1_R.z, l1_G.z, l1_B.z);
}

void SamplesToRepresentation(inout OutputRepresentation output, float4 L0_L1Rx, float4 L1G_L1Ry, float4 L1B_L1Rz)
{
    SamplesToRepresentation(output, L0_L1Rx, L1G_L1Ry, L1B_L1Rz, true);
}

// -------------------------------------------------------------
// Output utils
// -------------------------------------------------------------

// Constants from SetSHEMapConstants function in the Stupid Spherical Harmonics Tricks paper:
// http://www.ppsloan.org/publications/StupidSH36.pdf
//  [SH basis coeff] * [clamped cosine convolution factor]
#define fC0 (rsqrt(PI * 4.0) * rsqrt(PI * 4.0))  // Equivalent (0.282095 * (1.0 / (2.0 * sqrtPI)))
#define fC1 (rsqrt(PI * 4.0 / 3.0) * rsqrt(PI * 3.0)) // Equivalent to (0.488603 * (sqrt ( 3.0) / ( 3.0 * sqrtPI)))

void AddToOutputRepresentation(float3 value, float3 direction, inout OutputRepresentation output)
{
    static const float ConvolveCosineLobeBandFactor[] = { fC0, -fC1, fC1, -fC1 };

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
}
