#ifndef UNIVERSAL_LIGHT_INCLUDED
#define UNIVERSAL_LIGHT_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ExposureFunctions.hlsl"

// Abstraction over Light shading data.
struct Light
{
    half3   direction;
    URP_LIGHT_ACCUM3 color;
    float   distanceAttenuation; // full-float precision required on some platforms
    half    shadowAttenuation;
    uint    layerMask;
};

#endif // UNIVERSAL_LIGHT_INCLUDED
