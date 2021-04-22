
#ifndef UNLIT_INCLUDED
#define UNLIT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

half4 UniversalFragmentUnlit(InputData inputData, SurfaceData surfaceData)
{
    half3 albedo = surfaceData.albedo;

    #if defined(_ALPHAPREMULTIPLY_ON)
    albedo *= surfaceData.alpha;
    #endif

    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return debugColor;
    }
    #endif

    half4 finalColor = half4(albedo + surfaceData.emission, surfaceData.alpha);

    return finalColor;
}

// Deprecated: Use the version which takes "SurfaceData" instead of passing all of these arguments.
half4 UniversalFragmentUnlit(InputData inputData, half3 color, half alpha)
{
    SurfaceData surfaceData;

    surfaceData.albedo = color;
    surfaceData.alpha = alpha;
    surfaceData.emission = 0;
    surfaceData.metallic = 0;
    surfaceData.occlusion = 1;
    surfaceData.smoothness = 1;
    surfaceData.specular = 0;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = half3(0, 0, 1);

    return UniversalFragmentUnlit(inputData, surfaceData);
}

#endif
