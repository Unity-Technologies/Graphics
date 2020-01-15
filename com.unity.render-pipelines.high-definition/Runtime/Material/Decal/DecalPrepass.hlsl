#ifndef INCLUDE_DECAL_PREPASS
#define INCLUDE_DECAL_PREPASS

void EncodeIntoDecalPrepass(in SurfaceData surfaceData, int decalLayerMask, out float4 outBuffer0)
{
    float2 octNormalWS = PackNormalOctQuadEncode(surfaceData.geomNormalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    outBuffer0 = float4(packNormalWS, asfloat(decalLayerMask));
}

#endif
