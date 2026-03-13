#include "ShaderApiReflectionSupport.hlsl"

uint GetData()
{
    return unity_RendererUserValue;
}

///<funchints>
///     <sg:ProviderKey>RSUVGetBit</sg:ProviderKey>
///     <sg:DisplayName>RSUV Get Bit</sg:DisplayName>
///     <sg:SearchName>RSUV Get Bit</sg:SearchName>
///     <sg:SearchCategory>RSUV</sg:SearchCategory>
///</funchints>
///<paramhints name = "bitOffset">
///    <sg:int/>
///    <sg:Default>0</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
bool GetBit(int bitOffset)
{
    return ((GetData() & (1 << bitOffset)) != 0);
}

///<funchints>
///     <sg:ProviderKey>RSUVGetInt</sg:ProviderKey>
///     <sg:DisplayName>RSUV Get Int</sg:DisplayName>
///     <sg:SearchName>RSUV Get Int</sg:SearchName>
///     <sg:SearchCategory>RSUV</sg:SearchCategory>
///</funchints>
///<paramhints name = "bitOffset">
///    <sg:int/>
///    <sg:Default>0</sg:Default>
///</paramhints>
///<paramhints name = "bitCount">
///    <sg:int/>
///    <sg:Default>1</sg:Default>
///</paramhints>
UNITY_EXPORT_REFLECTION
float DecodeBitsToInt(int bitOffset, int bitCount)
{
    uint mask = (1u << bitCount) - 1u;        // Create bitCount-width mask (e.g., 0b000111 for 3 bits)
    uint value = (GetData() >> bitOffset) & mask;  // Shift down and apply mask
    return (int)value;
}

///<funchints>
///     <sg:ProviderKey>RSUVGetFloat4</sg:ProviderKey>
///     <sg:DisplayName>RSUV Get Float4</sg:DisplayName>
///     <sg:SearchName>RSUV Get Float4</sg:SearchName>
///     <sg:SearchCategory>RSUV</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION
float4 DecodeUintToFloat4()
{
    uint data = GetData();
    float a = ((data >> 24) & 0xFF) / 255.0;
    float r = ((data >> 16) & 0xFF) / 255.0;
    float g = ((data >> 8) & 0xFF) / 255.0;
    float b = (data & 0xFF) / 255.0;

    return float4(r, g, b, a);
}

///<funchints>
///     <sg:ProviderKey>RSUVGetFloat</sg:ProviderKey>
///     <sg:DisplayName>RSUV Get Float</sg:DisplayName>
///     <sg:SearchName>RSUV Get Float</sg:SearchName>
///     <sg:SearchCategory>RSUV</sg:SearchCategory>
///</funchints>
UNITY_EXPORT_REFLECTION
float DecodeUintToFloat()
{
    uint data = GetData();
    return asfloat(data);
}
