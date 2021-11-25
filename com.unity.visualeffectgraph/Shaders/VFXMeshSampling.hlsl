
#define VERTEXATTRIBUTEFORMAT_FLOAT32   0
#define VERTEXATTRIBUTEFORMAT_FLOAT16   1
#define VERTEXATTRIBUTEFORMAT_UNORM8    2
#define VERTEXATTRIBUTEFORMAT_SNORM8    3
#define VERTEXATTRIBUTEFORMAT_UNORM16   4
#define VERTEXATTRIBUTEFORMAT_SNORM16   5
#define VERTEXATTRIBUTEFORMAT_UINT8     6
#define VERTEXATTRIBUTEFORMAT_SINT8     7
#define VERTEXATTRIBUTEFORMAT_UINT16    8
#define VERTEXATTRIBUTEFORMAT_SINT16    9
#define VERTEXATTRIBUTEFORMAT_UINT32    10
#define VERTEXATTRIBUTEFORMAT_SINT32    11

#define INDEXFORMAT_FORMAT16            0
#define INDEXFORMAT_FORMAT32            1

uint   FetchBuffer(ByteAddressBuffer buffer, int offset)  { return buffer.Load(offset << 2); }
uint2  FetchBuffer2(ByteAddressBuffer buffer, int offset) { return buffer.Load2(offset << 2); }
uint3  FetchBuffer3(ByteAddressBuffer buffer, int offset) { return buffer.Load3(offset << 2); }
uint4  FetchBuffer4(ByteAddressBuffer buffer, int offset) { return buffer.Load4(offset << 2); }

float4 SampleMeshReadFloat(ByteAddressBuffer vertices, uint offset, uint channelFormatAndDimension, uint maxRead)
{
    float4 r = float4(0.0f, 0.0f, 0.0f, 0.0f);
    [branch]
    if (channelFormatAndDimension != -1)
    {
        uint format = channelFormatAndDimension & 0xff;
        uint dimension = (channelFormatAndDimension >> 8) & 0xff;

        if (format == VERTEXATTRIBUTEFORMAT_FLOAT32)
        {
            uint readSize = min(dimension, maxRead);
            uint4 readValue = (uint4)r;

            if      (readSize == 4u)    readValue.xyzw  = FetchBuffer4(vertices, offset);
            else if (readSize == 3u)    readValue.xyz   = FetchBuffer3(vertices, offset);
            else if (readSize == 2u)    readValue.xy    = FetchBuffer2(vertices, offset);
            else                        readValue.x     = FetchBuffer(vertices, offset);

            r = asfloat(readValue);
        }
        else
        {
            //Other format aren't supported yet.
        }
    }
    return r;
}

uint SampleMeshIndex(ByteAddressBuffer indices, uint index, uint indexFormat)
{
    uint r = 0u;
    [branch]
    if (indexFormat == INDEXFORMAT_FORMAT32)
    {
        r = FetchBuffer(indices, index);
    }
    else if(indexFormat == INDEXFORMAT_FORMAT16)
    {
        uint entryIndex = index >> 1u;
        uint entryOffset = index & 1u;

        uint read = FetchBuffer(indices, entryIndex);
        r = entryOffset == 1u ? ((read >> 16) & 0xffff) : read & 0xffff;
    }
    return r;
}

float4 SampleMeshFloat4(ByteAddressBuffer vertices, uint offset, uint channelFormatAndDimension)
{
    return SampleMeshReadFloat(vertices, offset, channelFormatAndDimension, 4u);
}

float3 SampleMeshFloat3(ByteAddressBuffer vertices, uint offset, uint channelFormatAndDimension)
{
    return SampleMeshReadFloat(vertices, offset, channelFormatAndDimension, 3u).xyz;
}

float2 SampleMeshFloat2(ByteAddressBuffer vertices, uint offset, uint channelFormatAndDimension)
{
    return SampleMeshReadFloat(vertices, offset, channelFormatAndDimension, 2u).xy;
}

float SampleMeshFloat(ByteAddressBuffer vertices, uint offset, uint channelFormatAndDimension)
{
    return SampleMeshReadFloat(vertices, offset, channelFormatAndDimension, 1u).x;
}

//Only SampleMeshColor support VERTEXATTRIBUTEFORMAT_UNORM8
float4 SampleMeshColor(ByteAddressBuffer vertices, uint offset, uint channelFormatAndDimension)
{
    float4 r = float4(0.0f, 0.0f, 0.0f, 0.0f);
    [branch]
    if (channelFormatAndDimension != -1)
    {
        float4 colorSRGB = (float4)0.0f;
        uint format = channelFormatAndDimension & 0xff;
        if (format == VERTEXATTRIBUTEFORMAT_UNORM8)
        {
            uint colorByte = FetchBuffer(vertices, offset);
            colorSRGB = float4(uint4(colorByte, colorByte >> 8, colorByte >> 16, colorByte >> 24) & 255) / 255.0f;
        }
        else
        {
            colorSRGB = SampleMeshFloat4(vertices, offset, channelFormatAndDimension);
        }
        r = float4(pow(abs(colorSRGB.rgb), 2.2f), colorSRGB.a); //Approximative SRGBToLinear
    }
    return r;
}
