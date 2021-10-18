#ifndef VISIBILITY_HLSL
#define VISIBILITY_HLSL

TEXTURE2D_X_UINT(_VisibilityTexture);

struct VisibilityData
{
    uint DOTSInstanceIndex;
    uint primitiveID;
};

#define InvalidVisibilityData 0xffffffff

float3 DebugVisIndexToRGB(uint index)
{
    if (index == 0)
        return float3(0, 0, 0);

    // Xorshift*32
    // Based on George Marsaglia's work: http://www.jstatsoft.org/v08/i14/paper
    uint value = index;
    value ^= value << 13;
    value ^= value >> 17;
    value ^= value << 5;

    float H = float(value & 511) / 511.0;

    //standard hue to HSV
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

uint packVisibilityData(in VisibilityData data)
{
    uint packedData = 0;
    packedData |= (data.DOTSInstanceIndex & 0xffff);
    packedData |= (data.primitiveID & 0xffff) << 16;
    return packedData;
}

void unpackVisibilityData(uint packedVisData, out VisibilityData data)
{
    data.DOTSInstanceIndex = (packedVisData & 0xffff);
    data.primitiveID = packedVisData >> 16;
}

#endif
