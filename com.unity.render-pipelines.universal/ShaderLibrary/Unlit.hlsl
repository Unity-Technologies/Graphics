
#ifndef UNLIT_INCLUDED
#define UNLIT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

half4 UniversalFragmentUnlit(InputData inputData, SurfaceData surfaceData)
{
    #if defined(_ALPHAPREMULTIPLY_ON)
    surfaceData.albedo *= surfaceData.alpha;
    #endif

    #if defined(_DEBUG_SHADER)
    half4 debugColor;

    if(CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    {
        return debugColor;
    }
    #endif

    return half4(surfaceData.albedo, surfaceData.alpha);
}

// TODO: Legacy code - is it safe to remove this?
half4 UniversalFragmentUnlit(InputData inputData, half3 color, half alpha)
{
    SurfaceData surfaceData;

    surfaceData.albedo = color;
    surfaceData.alpha = alpha;
    surfaceData.emission = half3(0, 0, 0);
    surfaceData.metallic = 0;
    surfaceData.occlusion = 1;
    surfaceData.smoothness = 1;
    surfaceData.specular = half3(0, 0, 0);
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = half3(0, 0, 1);

    return UniversalFragmentUnlit(inputData, surfaceData);
}

#endif
