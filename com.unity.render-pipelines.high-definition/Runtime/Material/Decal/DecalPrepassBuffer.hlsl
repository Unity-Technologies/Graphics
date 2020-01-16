#ifndef INCLUDE_DECAL_PREPASS
#define INCLUDE_DECAL_PREPASS

void EncodeIntoDecalPrepass(in SurfaceData surfaceData, uint decalLayerMask, out float4 outBuffer0)
{
    float2 octNormalWS = PackNormalOctQuadEncode(surfaceData.geomNormalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    outBuffer0 = float4(packNormalWS, asfloat(decalLayerMask));
}

void DecodeFromDecalPrepass(float4 buffer0, out float3 geomNormalWS, out uint decalLayerMask)
{
    float3 packNormalWS = buffer0.xyz;
    float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
    geomNormalWS = UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);

    decalLayerMask = asuint(buffer0.w);
}

#endif
