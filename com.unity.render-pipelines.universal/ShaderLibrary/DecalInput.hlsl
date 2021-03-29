#ifndef UIVERSAL_DECAL_INPUT_INDLUDED
#define UIVERSAL_DECAL_INPUT_INDLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

struct DecalSurfaceData
{
    real4 baseColor;
    real4 normalWS;
    real4 mask;
    real3 emissive;
    real2 MAOSBlend;
};
#endif
