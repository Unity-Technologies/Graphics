#ifndef UNITY_LEGACY_BUILDING_INCLUDED
#define UNITY_LEGACY_BUILDING_INCLUDED

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/SurfaceData.hlsl"

SurfaceData SurfaceDescriptionToSurfaceData(SurfaceDescription surfaceDescription)
{
    #if _AlphaClip
       half alpha = surfaceDescription.Alpha;
       clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
       half alpha = surfaceDescription.Alpha;
    #else
       half alpha = 1;
    #endif

    #ifdef _SPECULAR_SETUP
        float3 specular = surfaceDescription.Specular;
        float metallic = 1;
    #else
        float3 specular = 0;
        float metallic = surfaceDescription.Metallic;
    #endif

    SurfaceData surface         = (SurfaceData)0;
    surface.albedo              = surfaceDescription.BaseColor;
    surface.metallic            = saturate(metallic);
    surface.specular            = specular;
    surface.smoothness          = saturate(surfaceDescription.Smoothness),
    surface.occlusion           = surfaceDescription.Occlusion,
    surface.emission            = surfaceDescription.Emission,
    surface.alpha               = saturate(alpha);
    surface.clearCoatMask       = 0;
    surface.clearCoatSmoothness = 1;
    return surface;
}

SurfaceOutputStandard BuildStandardSurfaceOutput(SurfaceDescription surfaceDescription, InputData inputData)
{
    SurfaceData surface = SurfaceDescriptionToSurfaceData(surfaceDescription);

    SurfaceOutputStandard o = (SurfaceOutputStandard)0;
    o.Albedo = surface.albedo;
    o.Normal = inputData.normalWS;
    o.Metallic = surface.metallic;
    o.Smoothness = surface.smoothness;
    o.Occlusion = surface.occlusion;
    o.Emission = surface.emission;
    o.Alpha = surface.alpha;
    return o;
}


#endif // UNITY_LEGACY_BUILDING_INCLUDED
