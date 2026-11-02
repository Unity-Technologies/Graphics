#ifndef UNIFIED_RT_VERTEX_ATTRIBUTE_DECODE_HLSL
#define UNIFIED_RT_VERTEX_ATTRIBUTE_DECODE_HLSL

#define VERTEX_ATTRIBUTE_FORMAT_FLOAT32 0
#define VERTEX_ATTRIBUTE_FORMAT_FLOAT16 1
#define VERTEX_ATTRIBUTE_FORMAT_UNORM8 2
#define VERTEX_ATTRIBUTE_FORMAT_SNORM8 3
#define VERTEX_ATTRIBUTE_FORMAT_UNORM16 4
#define VERTEX_ATTRIBUTE_FORMAT_SNORM16 5
#define VERTEX_ATTRIBUTE_FORMAT_UINT8 6
#define VERTEX_ATTRIBUTE_FORMAT_SINT8 7
#define VERTEX_ATTRIBUTE_FORMAT_UINT16 8
#define VERTEX_ATTRIBUTE_FORMAT_SINT16 9
#define VERTEX_ATTRIBUTE_FORMAT_UINT32 10
#define VERTEX_ATTRIBUTE_FORMAT_SINT32 11

uint GetVertexAttributeComponentSize(uint format)
{
    switch (format)
    {
        case VERTEX_ATTRIBUTE_FORMAT_FLOAT16:
        case VERTEX_ATTRIBUTE_FORMAT_UNORM16:
        case VERTEX_ATTRIBUTE_FORMAT_SNORM16:
        case VERTEX_ATTRIBUTE_FORMAT_UINT16:
        case VERTEX_ATTRIBUTE_FORMAT_SINT16:
            return 2;
        case VERTEX_ATTRIBUTE_FORMAT_UNORM8:
        case VERTEX_ATTRIBUTE_FORMAT_SNORM8:
        case VERTEX_ATTRIBUTE_FORMAT_UINT8:
        case VERTEX_ATTRIBUTE_FORMAT_SINT8:
            return 1;
        default:
            return 4;
    }
}

int SignExtendVertexAttributeComponent(uint bits, uint bitCount)
{
    uint shift = 32 - bitCount;
    return asint(bits << shift) >> shift;
}

float DecodeVertexAttributeComponent(uint bits, uint format)
{
    switch (format)
    {
        case VERTEX_ATTRIBUTE_FORMAT_FLOAT16:
            return f16tof32(bits);
        case VERTEX_ATTRIBUTE_FORMAT_UNORM8:
            return bits / 255.0f;
        case VERTEX_ATTRIBUTE_FORMAT_SNORM8:
            return max(SignExtendVertexAttributeComponent(bits, 8) / 127.0f, -1.0f);
        case VERTEX_ATTRIBUTE_FORMAT_UNORM16:
            return bits / 65535.0f;
        case VERTEX_ATTRIBUTE_FORMAT_SNORM16:
            return max(SignExtendVertexAttributeComponent(bits, 16) / 32767.0f, -1.0f);
        case VERTEX_ATTRIBUTE_FORMAT_UINT8:
        case VERTEX_ATTRIBUTE_FORMAT_UINT16:
        case VERTEX_ATTRIBUTE_FORMAT_UINT32:
            return (float)bits;
        case VERTEX_ATTRIBUTE_FORMAT_SINT8:
            return (float)SignExtendVertexAttributeComponent(bits, 8);
        case VERTEX_ATTRIBUTE_FORMAT_SINT16:
            return (float)SignExtendVertexAttributeComponent(bits, 16);
        case VERTEX_ATTRIBUTE_FORMAT_SINT32:
            return (float)asint(bits);
        default:
            return asfloat(bits);
    }
}

float4 LoadVertexAttribute(ByteAddressBuffer buffer, uint byteAddress, uint format, uint dimension)
{
    uint componentSize = GetVertexAttributeComponentSize(format);
    uint componentMask = componentSize == 4 ? 0xFFFFFFFF : (1u << (componentSize * 8)) - 1;

    float4 result = 0;
    [unroll]
    for (uint i = 0; i < 4; ++i)
    {
        if (i < dimension)
        {
            uint componentByteOffset = i * componentSize;
            uint word = buffer.Load(byteAddress + (componentByteOffset & ~3u));
            uint bits = (word >> ((componentByteOffset & 3u) * 8)) & componentMask;
            result[i] = DecodeVertexAttributeComponent(bits, format);
        }
    }
    return result;
}

#endif
