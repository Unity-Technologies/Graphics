#ifndef UIVERSAL_DECAL_INPUT_INDLUDED
#define UIVERSAL_DECAL_INPUT_INDLUDED

struct DecalSurfaceData
{
    half4 baseColor;
    half4 normalWS;
    half3 emissive;
    half metallic;
    half occlusion;
    half smoothness;
    half MAOSAlpha;
};
#endif
