#ifndef CUSTOM_TEXTURE_GTRAPH
#define CUSTOM_TEXTURE_GTRAPH

float4 SRGBToLinear( float4 c ) { return c; }
float3 SRGBToLinear( float3 c ) { return c; }
float4 LinearToSRGB( float4 c ) { return c; }
float3 LinearToSRGB( float3 c ) { return c; }

// Unpack normal as DXT5nm (1, y, 1, x) or BC5 (x, y, 0, 1)
// Note neutral texture like "bump" is (0, 0, 1, 1) to work with both plain RGB normal and DXT5nm/BC5
float3 UnpackNormalMapRGAG(float4 packednormal)
{
    // This do the trick
    packednormal.x *= packednormal.w;

    float3 normal;
    normal.xy = packednormal.xy * 2 - 1;
    normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

inline float3 UnpackNormal(float4 packednormal)
{
#if defined(UNITY_NO_DXT5nm)
    return packednormal.xyz * 2 - 1;
#else
    return UnpackNormalMapRGAG(packednormal);
#endif
}

#endif // CUSTOM_TEXTURE_GTRAPH
