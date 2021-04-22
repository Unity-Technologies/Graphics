
#ifndef UNIVERSAL_DEBUGGING3D_INCLUDED
#define UNIVERSAL_DEBUGGING3D_INCLUDED

// Ensure that we always include "DebuggingCommon.hlsl" even if we don't use it - saves extraneous includes elsewhere...
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"

#if defined(DEBUG_DISPLAY)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

#define SETUP_DEBUG_TEXTURE_DATA(inputData, uv, texture)    SetupDebugDataTexture(inputData, uv, texture##_TexelSize, texture##_MipInfo, GetMipCount(TEXTURE2D_ARGS(texture, sampler##texture)))

void SetupDebugDataTexture(inout InputData inputData, float2 uv, float4 texelSize, float4 mipInfo, uint mipCount)
{
    inputData.uv = uv;
    inputData.texelSize = texelSize;
    inputData.mipInfo = mipInfo;
    inputData.mipCount = mipCount;
}

void SetupDebugDataBrdf(inout InputData inputData, half3 brdfDiffuse, half3 brdfSpecular)
{
    inputData.brdfDiffuse = brdfDiffuse;
    inputData.brdfSpecular = brdfSpecular;
}

half3 GetLODDebugColor()
{
#ifdef SHADER_API_GLES
    // No integer bit ops on GLES
    return kPurpleColor.rgb;
#else
    if (IsBitSet(unity_LODFade.z, 0))
        return GetDebugColor(0);
    else if (IsBitSet(unity_LODFade.z, 1))
        return GetDebugColor(1);
    else if (IsBitSet(unity_LODFade.z, 2))
        return GetDebugColor(2);
    else if (IsBitSet(unity_LODFade.z, 3))
        return GetDebugColor(3);
    else if (IsBitSet(unity_LODFade.z, 4))
        return GetDebugColor(4);
    else if (IsBitSet(unity_LODFade.z, 5))
        return GetDebugColor(5);
    else if (IsBitSet(unity_LODFade.z, 6))
        return GetDebugColor(6);
    else if (IsBitSet(unity_LODFade.z, 7))
        return GetDebugColor(7);
    else
        return GetDebugColor(8);
#endif
}

bool UpdateSurfaceAndInputDataForDebug(inout SurfaceData surfaceData, inout InputData inputData)
{
    bool changed = false;

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_ONLY || _DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_DETAIL)
    {
        surfaceData.albedo = 1;
        surfaceData.emission = 0;
        surfaceData.specular = 0;
        surfaceData.occlusion = 1;
        surfaceData.clearCoatMask = 0;
        surfaceData.clearCoatSmoothness = 1;
        surfaceData.metallic = 0;
        surfaceData.smoothness = 0;
        changed = true;
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS || _DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS)
    {
        surfaceData.albedo = 0;
        surfaceData.emission = 0;
        surfaceData.occlusion = 1;
        surfaceData.clearCoatMask = 0;
        surfaceData.clearCoatSmoothness = 1;
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS)
        {
            surfaceData.specular = 1;
            surfaceData.metallic = 0;
            surfaceData.smoothness = 1;
        }
        else if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS)
        {
            surfaceData.specular = 0;
            surfaceData.metallic = 1;
            surfaceData.smoothness = 0;
        }
        changed = true;
    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_ONLY || _DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS)
    {
        const half3 normalTS = half3(0, 0, 1);

        #if defined(_NORMALMAP)
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
        #else
        inputData.normalWS = inputData.normalWS;
        #endif
        surfaceData.normalTS = normalTS;
        changed = true;
    }

    return changed;
}

bool CalculateValidationMetallic(half3 albedo, half metallic, inout half4 debugColor)
{
    if (metallic < _DebugValidateMetallicMinValue)
    {
        debugColor = _DebugValidateBelowMinThresholdColor;
    }
    else if (metallic > _DebugValidateMetallicMaxValue)
    {
        debugColor = _DebugValidateAboveMaxThresholdColor;
    }
    else
    {
        half luminance = Luminance(albedo);

        debugColor = half4(luminance, luminance, luminance, 1);
    }
    return true;
}

bool CalculateValidationColorForDebug(in InputData inputData, in SurfaceData surfaceData, inout half4 debugColor)
{
    switch(_DebugValidationMode)
    {
        case DEBUGVALIDATIONMODE_NONE:
        case DEBUGVALIDATIONMODE_HIGHLIGHT_NAN_INF_NEGATIVE:
        case DEBUGVALIDATIONMODE_HIGHLIGHT_OUTSIDE_OF_RANGE:
            return false;

        case DEBUGVALIDATIONMODE_VALIDATE_ALBEDO:
            return CalculateValidationAlbedo(surfaceData.albedo, debugColor);

        case DEBUGVALIDATIONMODE_VALIDATE_METALLIC:
            return CalculateValidationMetallic(surfaceData.albedo, surfaceData.metallic, debugColor);

        case DEBUGVALIDATIONMODE_VALIDATE_MIPMAPS:
            return CalculateValidationMipLevel(inputData.mipCount, inputData.mipInfo.y, inputData.uv, inputData.texelSize, surfaceData.albedo, surfaceData.alpha, debugColor);

        default:
            return TryGetDebugColorInvalidMode(debugColor);
    }
}

bool CalculateDebugColorForMipmaps(in InputData inputData, in SurfaceData surfaceData, inout half4 debugColor)
{
    switch (_DebugMipInfoMode)
    {
        case DEBUGMIPINFOMODE_NONE:
            return false;

        case DEBUGMIPINFOMODE_LEVEL:
            debugColor = GetMipLevelDebugColor(inputData.positionWS, surfaceData.albedo, inputData.uv, inputData.texelSize);
            return true;

        case DEBUGMIPINFOMODE_COUNT:
            debugColor = GetMipCountDebugColor(inputData.positionWS, surfaceData.albedo, inputData.mipCount);
            return true;

        default:
            return TryGetDebugColorInvalidMode(debugColor);
    }
}

bool CalculateColorForDebugMaterial(in InputData inputData, in SurfaceData surfaceData, inout half4 debugColor)
{
    // Debug materials...
    switch(_DebugMaterialMode)
    {
        case DEBUGMATERIALMODE_NONE:
            return false;

        case DEBUGMATERIALMODE_ALBEDO:
            debugColor = half4(surfaceData.albedo, 1);
            return true;

        case DEBUGMATERIALMODE_SPECULAR:
            debugColor = half4(surfaceData.specular, 1);
            return true;

        case DEBUGMATERIALMODE_ALPHA:
            debugColor = half4(surfaceData.alpha.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_SMOOTHNESS:
            debugColor = half4(surfaceData.smoothness.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_AMBIENT_OCCLUSION:
            debugColor = half4(surfaceData.occlusion.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_EMISSION:
            debugColor = half4(surfaceData.emission, 1);
            return true;

        case DEBUGMATERIALMODE_NORMAL_WORLD_SPACE:
            debugColor = half4(inputData.normalWS.xyz * 0.5 + 0.5, 1);
            return true;

        case DEBUGMATERIALMODE_NORMAL_TANGENT_SPACE:
            debugColor = half4(surfaceData.normalTS.xyz * 0.5 + 0.5, 1);
            return true;

        case DEBUGMATERIALMODE_LOD:
            debugColor = half4(GetLODDebugColor(), 1);
            return true;

        case DEBUGMATERIALMODE_METALLIC:
            debugColor = half4(surfaceData.metallic.rrr, 1);
            return true;

        default:
            return TryGetDebugColorInvalidMode(debugColor);
    }
}

bool CalculateColorForDebug(in InputData inputData, in SurfaceData surfaceData, inout half4 debugColor)
{
    if (CalculateColorForDebugSceneOverride(debugColor))
    {
        return true;
    }
    else if (CalculateColorForDebugMaterial(inputData, surfaceData, debugColor))
    {
        return true;
    }
    else if (CalculateValidationColorForDebug(inputData, surfaceData, debugColor))
    {
        return true;
    }
    else if (CalculateDebugColorForMipmaps(inputData, surfaceData, debugColor))
    {
        return true;
    }
    else
    {
        return false;
    }
}

half3 CalculateDebugShadowCascadeColor(in InputData inputData)
{
    float3 positionWS = inputData.positionWS;
    half cascadeIndex = ComputeCascadeIndex(positionWS);

    return GetDebugColor(cascadeIndex).rgb;
}

half4 CalculateDebugLightingComplexityColor(in InputData inputData, in SurfaceData surfaceData)
{
    // Assume a main light and add 1 to the additional lights.
    int numLights = GetAdditionalLightsCount() + 1;

    return CalculateDebugColorWithNumber(inputData.positionWS, surfaceData.albedo, numLights);
}

bool CanDebugOverrideOutputColor(inout InputData inputData, inout SurfaceData surfaceData, inout BRDFData brdfData, inout half4 debugColor)
{
    if (_DebugMaterialMode == DEBUGMATERIALMODE_LIGHTING_COMPLEXITY)
    {
        debugColor = CalculateDebugLightingComplexityColor(inputData, surfaceData);
        return true;
    }
    else
    {
        debugColor = half4(0, 0, 0, 1);

        if (_DebugLightingMode == DEBUGLIGHTINGMODE_SHADOW_CASCADES)
        {
            surfaceData.albedo = CalculateDebugShadowCascadeColor(inputData);
        }
        else if ((_DebugMaterialMode == DEBUGMATERIALMODE_LOD) && CalculateColorForDebug(inputData, surfaceData, debugColor))
        {
            surfaceData.albedo = debugColor.rgb;
        }
        else
        {
            if (UpdateSurfaceAndInputDataForDebug(surfaceData, inputData))
            {
                // If we've modified any data we'll need to re-sample the GI to ensure that everything works correctly...
                inputData.bakedGI = SAMPLE_GI(inputData.lightmapUV, inputData.vertexSH, inputData.normalWS);
            }
        }

        // Update the BRDF data following any changes to the input/surface above...
        InitializeBRDFData(surfaceData, brdfData);

        return (_DebugMaterialMode != DEBUGMATERIALMODE_LOD) && CalculateColorForDebug(inputData, surfaceData, debugColor);
    }
}

bool CanDebugOverrideOutputColor(inout InputData inputData, inout SurfaceData surfaceData, inout half4 debugColor)
{
    if (_DebugMaterialMode == DEBUGMATERIALMODE_LIGHTING_COMPLEXITY)
    {
        debugColor = CalculateDebugLightingComplexityColor(inputData, surfaceData);
        return true;
    }
    else
    {
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_SHADOW_CASCADES)
        {
            surfaceData.albedo = CalculateDebugShadowCascadeColor(inputData);
        }
        else if ((_DebugMaterialMode == DEBUGMATERIALMODE_LOD) && CalculateColorForDebug(inputData, surfaceData, debugColor))
        {
            surfaceData.albedo = debugColor.rgb;
        }
        else
        {
            if (UpdateSurfaceAndInputDataForDebug(surfaceData, inputData))
            {
                // If we've modified any data we'll need to re-sample the GI to ensure that everything works correctly...
                inputData.bakedGI = SAMPLE_GI(inputData.lightmapUV, inputData.vertexSH, inputData.normalWS);
            }
        }

        return (_DebugMaterialMode != DEBUGMATERIALMODE_LOD) && CalculateColorForDebug(inputData, surfaceData, debugColor);
    }
}

#else

// When "DEBUG_DISPLAY" isn't defined this macro does nothing - there's no debug-data to set-up...
#define SETUP_DEBUG_TEXTURE_DATA(inputData, uv, texture)

#endif

#endif
