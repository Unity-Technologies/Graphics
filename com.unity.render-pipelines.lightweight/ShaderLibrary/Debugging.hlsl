
#ifndef LIGHTWEIGHT_DEBUGGING_INCLUDED
#define LIGHTWEIGHT_DEBUGGING_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"

#define DEBUG_UNLIT 1
#define DEBUG_DIFFUSE 2
#define DEBUG_SPECULAR 3
#define DEBUG_ALPHA 4
#define DEBUG_SMOOTHNESS 5
#define DEBUG_OCCLUSION 6
#define DEBUG_EMISSION 7
#define DEBUG_NORMAL_WORLD_SPACE 8
#define DEBUG_NORMAL_TANGENT_SPACE 9
#define DEBUG_LIGHTING_COMPLEXITY 10
int _DebugMaterialIndex;

#define DEBUG_LIGHTING_SHADOW_CASCADES 1
#define DEBUG_LIGHTING_LIGHT_ONLY 2
#define DEBUG_LIGHTING_LIGHT_DETAIL 3
#define DEBUG_LIGHTING_REFLECTIONS 4
#define DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS 5
int _DebugLightingIndex;

struct DebugData
{
    SurfaceData surfaceData;
    InputData inputData;
};

#if 0
// TODO: Set of colors that should still provide contrast for the Color-blind
const half4 Purple = half4(156.0 / 255.0, 79.0 / 255.0, 255.0 / 255.0, 1.0); // #9C4FFF 
const half4 Red = half4(203.0 / 255.0, 48.0 / 255.0, 34.0 / 255.0, 1.0) ; // #CB3022
const half4 Green = half4(8.0 / 255.0, 215.0 / 255.0, 139.0 / 255.0, 1.0) ; // #08D78B
const half4 YellowGreen = half4(151.0 / 255.0, 209.0 / 255.0, 61.0 / 255.0, 1.0) ; // #97D13D
const half4 Blue = half4(75.0 / 255.0, 146.0 / 255.0, 243.0 / 255.0, 1.0) ; // #4B92F3
const half4 OrangeBrown = half4(219.0 / 255.0, 119.0 / 255.0, 59.0 / 255.0, 1.0) ; // #4B92F3
const half4 Gray = half4(174.0 / 255.0, 174.0 / 255.0, 174.0 / 255.0, 1.0) ; // #AEAEAE   
#endif

half4 GetShadowCascadeColor(float4 shadowCoord, float3 positionWS)
{
    Light mainLight = GetMainLight(shadowCoord);
    half cascadeIndex = ComputeCascadeIndex(positionWS);

    half4 cascadeColors[] =
    {
        half4(0.1, 0.1, 0.9, 1.0), // blue
        half4(0.1, 0.9, 0.1, 1.0), // green
        half4(0.9, 0.9, 0.1, 1.0), // yellow
        half4(0.9, 0.1, 0.1, 1.0), // red
    };

    return cascadeColors[cascadeIndex];
}

half3 ShadowCascadeColor(DebugData debugData)
{
    InputData inputData = debugData.inputData;
    SurfaceData surfaceData = debugData.surfaceData;
    half4 shadowCascadeColor = GetShadowCascadeColor(inputData.shadowCoord, inputData.positionWS);

    // part adapted from LightweightFragmentPBR:

    BRDFData brdfData;
    InitializeBRDFData(shadowCascadeColor.rgb, 0.0, 0.0, 1.0, surfaceData.alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord);
    mainLight.color = shadowCascadeColor.rgb;
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

    half3 debugColor = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
    debugColor += LightingPhysicallyBased(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS);

    return debugColor;
}

sampler2D _DebugNumberTexture;
half4 LightingComplexity(InputData inputData)
{
    half4 lut[5] = {
            half4(0, 1, 0, 0),
            half4(0.25, 0.75, 0, 0),
            half4(0.498, 0.5019, 0.0039, 0),
            half4(0.749, 0.247, 0, 0),
            half4(1, 0, 0, 0)
    };

    // Assume a main light and add 1 to the additional lights.
    unsigned int numLights = clamp(GetAdditionalLightsCount()+1, 0, 4);
    half4 fc = lut[numLights];

    float4 clipPos = TransformWorldToHClip(inputData.positionWS);
    float2 ndc = saturate((clipPos.xy / clipPos.w) * 0.5 + 0.5);

#if UNITY_UV_STARTS_AT_TOP
    if(_ProjectionParams.x < 0)
        ndc.y = 1.0 - ndc.y;
#endif

    const float invNumChar = 1.0 / 10.0f;
    ndc.x *= 5.0;
    ndc.y *= 15.0;
    ndc.x = fmod(ndc.x, invNumChar) + (numLights * invNumChar);

    fc *= tex2D(_DebugNumberTexture, ndc.xy);

    return fc;
}

SurfaceData CalculateSurfaceDataForDebug(SurfaceData surfaceData)
{
    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY || _DebugLightingIndex == DEBUG_LIGHTING_LIGHT_DETAIL)
    {
        surfaceData.albedo = half3(1.0h, 1.0h, 1.0h);
        surfaceData.metallic = 0.0;
        surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
        surfaceData.smoothness = 0.0;
        surfaceData.occlusion = 0.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    }
    
    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS)
    {
        surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
    }
    
    if (_DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS)
    {
        surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
        surfaceData.smoothness = 1.0;
    }
    
    if (_DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS)
    {
        surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
        surfaceData.metallic = 1.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    }
    
    return surfaceData;
}

half4 CalculateColorForDebug(DebugData debugData)
{
    SurfaceData surfaceData = debugData.surfaceData;
    InputData inputData = debugData.inputData;
    half4 color = half4(0.0, 0.0, 0.0, 1.0);
    BRDFData brdfData;
    
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
    
    // Debug materials...
    if (_DebugMaterialIndex == DEBUG_UNLIT)
        color.rgb = surfaceData.albedo;
        
    if (_DebugMaterialIndex == DEBUG_DIFFUSE)
        color.rgb = brdfData.diffuse;
        
    if (_DebugMaterialIndex == DEBUG_SPECULAR)
        color.rgb = brdfData.specular;
    
    if (_DebugMaterialIndex == DEBUG_ALPHA)
        color.rgb = (1.0 - surfaceData.alpha).xxx;
    
    if (_DebugMaterialIndex == DEBUG_SMOOTHNESS)
        color.rgb = surfaceData.smoothness.xxx;
    
    if (_DebugMaterialIndex == DEBUG_OCCLUSION)
        color.rgb = surfaceData.occlusion.xxx;
    
    if (_DebugMaterialIndex == DEBUG_EMISSION)
        color.rgb = surfaceData.emission;
        
    if (_DebugMaterialIndex == DEBUG_NORMAL_WORLD_SPACE)
        color.rgb = inputData.normalWS.xyz * 0.5 + 0.5;
        
    if (_DebugMaterialIndex == DEBUG_NORMAL_TANGENT_SPACE)
        color.rgb = surfaceData.normalTS.xyz * 0.5 + 0.5;

    if (_DebugMaterialIndex == DEBUG_LIGHTING_COMPLEXITY)
        color = LightingComplexity(inputData);

    // Debug lighting...
    if (_DebugLightingIndex == DEBUG_LIGHTING_SHADOW_CASCADES)
    {
        color.rgb = ShadowCascadeColor(debugData);
        color.a = surfaceData.alpha;
    }

    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY
     || _DebugLightingIndex == DEBUG_LIGHTING_LIGHT_DETAIL
     || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS
     || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS)
    {
        color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
    }
    
    return color;
}

#endif
