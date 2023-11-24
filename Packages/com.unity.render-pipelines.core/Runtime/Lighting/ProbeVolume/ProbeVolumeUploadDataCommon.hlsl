#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeReferenceVolume.Streaming.cs.hlsl"

ByteAddressBuffer _ScratchBuffer;

float4 UintToFloat4(uint input)
{
    float4 result;
    result.x = (input & 0x000000FF) / 255.0f;
    result.y = ((input >> 8) & 0x000000FF) / 255.0f;
    result.z = ((input >> 16) & 0x000000FF) / 255.0f;
    result.w = ((input >> 24) & 0x000000FF) / 255.0f;

    return result;
}

// Extract two FP16 rgba values encoded in an uint4
void ExtractFP16(uint4 input, out float4 value0, out float4 value1)
{
    float4 temp0 = f16tof32(input);
    float4 temp1 = f16tof32(input >> 16);

    value0.xz = temp0.xy;
    value0.yw = temp1.xy;
    value1.xz = temp0.zw;
    value1.yw = temp1.zw;
}

void ExtractByte(uint4 input, out float4 value0, out float4 value1, out float4 value2, out float4 value3)
{
    value0 = UintToFloat4(input.x);
    value1 = UintToFloat4(input.y);
    value2 = UintToFloat4(input.z);
    value3 = UintToFloat4(input.w);
}

void ExtractByte(uint input, out float4 value)
{
    value = UintToFloat4(input);
}

void getProbeLocationAndOffsets(uint chunkIndex, uint chunkProbeIndex, out float3 baseProbe, out float3 loc, out float3 probe1Offset, out float3 probe2Offset, out float3 probe3Offset)
{
    baseProbe.z = chunkProbeIndex / _ProbeCountInChunkSlice;
    uint indexInSlice = chunkProbeIndex - baseProbe.z * _ProbeCountInChunkSlice;
    baseProbe.y = indexInSlice / _ProbeCountInChunkLine;
    baseProbe.x = indexInSlice - baseProbe.y * _ProbeCountInChunkLine;

    uint3 dstChunk = _ScratchBuffer.Load4(chunkIndex * 16).xyz; // *16 because 4 int per chunk.
    loc = dstChunk + baseProbe;
    probe1Offset = uint3(1, 0, 0);
    probe2Offset = uint3(2, 0, 0);
    probe3Offset = uint3(3, 0, 0);
}