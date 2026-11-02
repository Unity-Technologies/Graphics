#ifndef UNIVERSAL_LIGHT_INCLUDED
#define UNIVERSAL_LIGHT_INCLUDED

// Abstraction over Light shading data.
struct Light
{
    half3   direction;
    half3   color;
    float   distanceAttenuation; // full-float precision required on some platforms
    half    shadowAttenuation;
    uint    layerMask;
};

#endif // UNIVERSAL_LIGHT_INCLUDED
