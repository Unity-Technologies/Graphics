#ifndef UNIVERSAL_SURFACE_INPUT_TYPES_INCLUDED
#define UNIVERSAL_SURFACE_INPUT_TYPES_INCLUDED

// Type declarations only! (To reduce dependencies for interfaces and codegen).

// Must match Universal ShaderGraph master node
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
    half  clearCoatStrength;
    half  clearCoatSmoothness;
};

#endif
