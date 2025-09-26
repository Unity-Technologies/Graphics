bool GetBit(uint data, uint bitIndex)
{
    return ((data & (1 << bitIndex)) != 0);
}

float DecodeBitsToInt(uint data, int bitOffset, int bitCount)
{
    uint mask = (1u << bitCount) - 1u;        // Create bitCount-width mask (e.g., 0b000111 for 3 bits)
    uint value = (data >> bitOffset) & mask;  // Shift down and apply mask
    return (int)value;
}

float4 DecodeUintToFloat4(uint data)
{
    float a = ((data >> 24) & 0xFF) / 255.0;
    float r = ((data >> 16) & 0xFF) / 255.0;
    float g = ((data >> 8) & 0xFF) / 255.0;
    float b = (data & 0xFF) / 255.0;

    return float4(r, g, b, a);
}

uint GetData()
{
    return unity_RendererUserValue;
}
