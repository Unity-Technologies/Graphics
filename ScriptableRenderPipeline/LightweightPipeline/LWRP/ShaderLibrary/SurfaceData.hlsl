#ifndef LIGHTWEIGHT_SURFACE_DATA_INCLUDED
#define LIGHTWEIGHT_SURFACE_DATA_INCLUDED

// Must match Lightweigth ShaderGraph master node
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
};

#endif // LIGHTWEIGHT_SURFACE_DATA_INCLUDED
