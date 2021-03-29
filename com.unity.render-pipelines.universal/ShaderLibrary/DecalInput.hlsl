#ifndef UIVERSAL_DECAL_INPUT_INDLUDED
#define UIVERSAL_DECAL_INPUT_INDLUDED

//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

struct DecalSurfaceData
{
    half4 baseColor;
    half4 normalWS;
    half4 mask;
    half3 emissive;
    half2 MAOSBlend;
};
#endif
