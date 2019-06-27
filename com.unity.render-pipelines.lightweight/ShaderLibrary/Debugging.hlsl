
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
#define DEBUG_LOD 11

int _DebugMaterialIndex;

#define DEBUG_LIGHTING_SHADOW_CASCADES 1
#define DEBUG_LIGHTING_LIGHT_ONLY 2
#define DEBUG_LIGHTING_LIGHT_DETAIL 3
#define DEBUG_LIGHTING_REFLECTIONS 4
#define DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS 5
int _DebugLightingIndex;

#define DEBUG_PBR_LIGHTING_ENABLE_GI 0
#define DEBUG_PBR_LIGHTING_ENABLE_PBR_LIGHTING 1
#define DEBUG_PBR_LIGHTING_ENABLE_ADDITIONAL_LIGHTS 2
#define DEBUG_PBR_LIGHTING_ENABLE_VERTEX_LIGHTING 3
#define DEBUG_PBR_LIGHTING_ENABLE_EMISSION 4
int _DebugPBRLightingMask;

#define DEBUG_VALIDATION_NONE 0
//#define DEBUG_VALIDATION_HIGHLIGHT_NAN_INFINITE_NEGATIVE 1
//#define DEBUG_VALIDATION_HIGHLIGHT_OUTSIDE_RANGE 2
#define DEBUG_VALIDATION_ALBEDO 3
#define DEBUG_VALIDATION_METAL 4
int _DebugValidationIndex;
half _AlbedoMinLuminance = 0.01;
half _AlbedoMaxLuminance = 0.90;
half _AlbedoSaturationTolerance = 0.214;
half _AlbedoHueTolerance = 0.104;
half3 _AlbedoCompareColor = half3(0.5, 0.5, 0.5);

struct DebugData
{
    SurfaceData surfaceData;
    InputData inputData;
};

// Set of colors that should still provide contrast for the Color-blind
#define kPurpleColor half4(156.0 / 255.0, 79.0 / 255.0, 255.0 / 255.0, 1.0) // #9C4FFF 
#define kRedColor half4(203.0 / 255.0, 48.0 / 255.0, 34.0 / 255.0, 1.0) // #CB3022
#define kGreenColor half4(8.0 / 255.0, 215.0 / 255.0, 139.0 / 255.0, 1.0) // #08D78B
#define kYellowGreenColor half4(151.0 / 255.0, 209.0 / 255.0, 61.0 / 255.0, 1.0) // #97D13D
#define kBlueColor half4(75.0 / 255.0, 146.0 / 255.0, 243.0 / 255.0, 1.0) // #4B92F3
#define kOrangeBrownColor half4(219.0 / 255.0, 119.0 / 255.0, 59.0 / 255.0, 1.0) // #4B92F3
#define kGrayColor half4(174.0 / 255.0, 174.0 / 255.0, 174.0 / 255.0, 1.0) // #AEAEAE   

half4 GetShadowCascadeColor(float4 shadowCoord, float3 positionWS)
{
    Light mainLight = GetMainLight(shadowCoord);
    half cascadeIndex = ComputeCascadeIndex(positionWS);

    half4 cascadeColors[] =
    {
        kBlueColor,
        kGreenColor,
        kYellowGreenColor,
        kRedColor,
    };

    return cascadeColors[cascadeIndex];
}

float4 GetLODDebugColor()
{
    if (IsBitSet(unity_LODFade.z, 0))
        return float4(0.4831376f, 0.6211768f, 0.0219608f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 1))
        return float4(0.2792160f, 0.4078432f, 0.5835296f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 2))
        return float4(0.2070592f, 0.5333336f, 0.6556864f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 3))
        return float4(0.5333336f, 0.1600000f, 0.0282352f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 4))
        return float4(0.3827448f, 0.2886272f, 0.5239216f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 5))
        return float4(0.8000000f, 0.4423528f, 0.0000000f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 6))
        return float4(0.4486272f, 0.4078432f, 0.0501960f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 7))
        return float4(0.7749016f, 0.6368624f, 0.0250984f, 1.0f);
    return float4(0,0,0,0);
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
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    }
    
    if (_DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS)
    {
        surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
        surfaceData.metallic = 1.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    }
    
    return surfaceData;
}

half4 LightweightFragmentPBRDebug(InputData inputData, half3 albedo, half metallic, half3 specular,
    half smoothness, half occlusion, half3 emission, half alpha, int disableMask = 0)
{
    BRDFData brdfData;
    InitializeBRDFData(albedo, metallic, specular, smoothness, alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));
    
    half3 color = 0;
    if (IsBitSet(disableMask, DEBUG_PBR_LIGHTING_ENABLE_GI))
        color += GlobalIllumination(brdfData, inputData.bakedGI, occlusion, inputData.normalWS, inputData.viewDirectionWS);
    
    if (IsBitSet(disableMask, DEBUG_PBR_LIGHTING_ENABLE_PBR_LIGHTING))
        color += LightingPhysicallyBased(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS);

#ifdef _ADDITIONAL_LIGHTS
    if (IsBitSet(disableMask, DEBUG_PBR_LIGHTING_ENABLE_ADDITIONAL_LIGHTS))
    {
        int pixelLightCount = GetAdditionalLightsCount();
        for (int i = 0; i < pixelLightCount; ++i)
        {
            Light light = GetAdditionalLight(i, inputData.positionWS);
            color += LightingPhysicallyBased(brdfData, light, inputData.normalWS, inputData.viewDirectionWS);
        }
    }
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    if (IsBitSet(disableMask, DEBUG_PBR_LIGHTING_ENABLE_VERTEX_LIGHTING))
        color += inputData.vertexLighting * brdfData.diffuse;
#endif

    if (IsBitSet(disableMask, DEBUG_PBR_LIGHTING_ENABLE_EMISSION)) 
        color += emission;
    return half4(color, alpha);
}

// Convert rgb to luminance
// with rgb in linear space with sRGB primaries and D65 white point
half LinearRgbToLuminance(half3 linearRgb)
{
    return dot(linearRgb, half3(0.2126729f, 0.7151522f, 0.0721750f));
}

half3 UnityMeta_RGBToHSVHelper(float offset, half dominantColor, half colorone, half colortwo)
{
    half H, S, V;
    V = dominantColor;

    if (V != 0.0)
    {
        half small = 0.0;
        if (colorone > colortwo)
            small = colortwo;
        else
            small = colorone;

        half diff = V - small;

        if (diff != 0)
        {
            S = diff / V;
            H = offset + ((colorone - colortwo) / diff);
        }
        else
        {
            S = 0;
            H = offset + (colorone - colortwo);
        }

        H /= 6.0;

        if (H < 6.0)
        {
            H += 1.0;
        }
    }
    else
    {
        S = 0;
        H = 0;
    }
    return half3(H, S, V);
}

half3 UnityMeta_RGBToHSV(half3 rgbColor)
{
    // when blue is highest valued
    if ((rgbColor.b > rgbColor.g) && (rgbColor.b > rgbColor.r))
        return UnityMeta_RGBToHSVHelper(4.0, rgbColor.b, rgbColor.r, rgbColor.g);
    //when green is highest valued
    else if (rgbColor.g > rgbColor.r)
        return UnityMeta_RGBToHSVHelper(2.0, rgbColor.g, rgbColor.b, rgbColor.r);
    //when red is highest valued
    else
        return UnityMeta_RGBToHSVHelper(0.0, rgbColor.r, rgbColor.g, rgbColor.b);
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

    if (_DebugMaterialIndex == DEBUG_LOD)
        surfaceData.albedo = GetLODDebugColor().rgb;

    // Debug lighting...
    if (_DebugLightingIndex == DEBUG_LIGHTING_SHADOW_CASCADES)
    {
        color.rgb = ShadowCascadeColor(debugData);
        color.a = surfaceData.alpha;
    }
    else if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY
     || _DebugLightingIndex == DEBUG_LIGHTING_LIGHT_DETAIL
     || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS
     || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS
     || _DebugMaterialIndex == DEBUG_LOD)
    {
        color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, 
            surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
    }
    
    if (_DebugPBRLightingMask != 0)
    {
        color = LightweightFragmentPBRDebug(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, 
            surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha, _DebugPBRLightingMask);
    }

    if (_DebugValidationIndex == DEBUG_VALIDATION_ALBEDO)
    {
        half value = LinearRgbToLuminance(surfaceData.albedo);

        if (_AlbedoMinLuminance > value)
             return half4(1.0f, 0.0f, 0.0f, 1.0f);
        else if (_AlbedoMaxLuminance < value)
             return half4(0.0f, 1.0f, 0.0f, 1.0f);
        else
        {
            half3 hsv = UnityMeta_RGBToHSV(surfaceData.albedo);
            half hue = hsv.r;
            half sat = hsv.g;

            half3 compHSV = UnityMeta_RGBToHSV(_AlbedoCompareColor.rgb);
            half compHue = compHSV.r;
            half compSat = compHSV.g;

            if ((compSat - _AlbedoSaturationTolerance > sat) || ((compHue - _AlbedoHueTolerance > hue) && (compHue - _AlbedoHueTolerance + 1.0 > hue)))
            {
                return half4(1.0f, 0.0f, 0.0f, 1.0f);
            }
            else if ((sat > compSat + _AlbedoSaturationTolerance) || ((hue > compHue + _AlbedoHueTolerance) && (hue > compHue + _AlbedoHueTolerance - 1.0)))
            {
                return half4(0.0f, 1.0f, 0.0f, 1.0f);
            }
            else
            {
                return half4(value, value, value, 0);
            }
        }
    }

    return color;
}

#endif
