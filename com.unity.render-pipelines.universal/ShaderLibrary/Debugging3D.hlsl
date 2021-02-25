
#ifndef UNIVERSAL_DEBUGGING3D_INCLUDED
#define UNIVERSAL_DEBUGGING3D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebuggingCommon.hlsl"

#if defined(_DEBUG_SHADER)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

#define SETUP_DEBUG_TEXTURE_DATA(inputData, uv, texture)    SetupDebugDataTexture(inputData, uv, texture##_TexelSize, texture##_MipInfo, GetMipCount(texture))

half4 GetShadowCascadeColor(float4 shadowCoord, float3 positionWS);

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
}

bool UpdateSurfaceAndInputDataForDebug(inout SurfaceData surfaceData, inout InputData inputData)
{
    bool changed = false;

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_ONLY || _DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_DETAIL)
    {
        surfaceData.albedo = half3(1, 1, 1);
        surfaceData.emission = half3(0, 0, 0);
        surfaceData.specular = half3(0, 0, 0);
        surfaceData.occlusion = 1;
        surfaceData.clearCoatMask = 0;
        surfaceData.clearCoatSmoothness = 1;
        surfaceData.metallic = 0;
        surfaceData.smoothness = 0;
        changed = true;
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS || _DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS)
    {
        surfaceData.albedo = half3(0, 0, 0);
        surfaceData.emission = half3(0, 0, 0);
        surfaceData.occlusion = 1;
        surfaceData.clearCoatMask = 0;
        surfaceData.clearCoatSmoothness = 1;
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS)
        {
            surfaceData.specular = half3(1, 1, 1);
            surfaceData.metallic = 0;
            surfaceData.smoothness = 1;
        }
        else if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS)
        {
            surfaceData.specular = half3(0, 0, 0);
            surfaceData.metallic = 1;
            surfaceData.smoothness = 0;
        }
        changed = true;
    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_ONLY || _DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS)
    {
        const half3 normalTS = half3(0, 0, 1);

        #if defined(_NORMALMAP)
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentMatrixWS);
        #else
        inputData.normalWS = inputData.normalWS;
        #endif
        inputData.normalTS = normalTS;
        surfaceData.normalTS = normalTS;
        changed = true;
    }

    return changed;
}

bool CalculateValidationMetallic(half3 albedo, half metallic, out half4 color)
{
    if(metallic < _DebugValidateMetallicMinValue)
    {
        color = half4(1, 0, 0, 1);
    }
    else if(metallic > _DebugValidateMetallicMaxValue)
    {
        color = half4(0, 0, 1, 1);
    }
    else
    {
        half luminance = LinearRgbToLuminance(albedo);

        color = half4(luminance, luminance, luminance, 1);
    }
    return true;
}

bool CalculateValidationColorForDebug(in InputData inputData, in SurfaceData surfaceData, out half4 color)
{
    if (_DebugValidationMode == DEBUGVALIDATIONMODE_VALIDATE_ALBEDO)
    {
        return CalculateValidationAlbedo(surfaceData.albedo, color);
    }
    else if(_DebugValidationMode == DEBUGVALIDATIONMODE_VALIDATE_METALLIC)
    {
        return CalculateValidationMetallic(surfaceData.albedo, surfaceData.metallic, color);
    }
    else
    {
        color = 0;
        return false;
    }
}

bool CalculateValidationColorForMipMaps(in InputData inputData, in SurfaceData surfaceData, out half4 color)
{
    switch (_DebugMipInfoMode)
    {
        case DEBUGMIPINFOMODE_LEVEL:
            color = GetMipLevelDebugColor(inputData.positionWS, surfaceData.albedo, inputData.uv, inputData.texelSize);
            return true;

        case DEBUGMIPINFOMODE_COUNT:
            color = GetMipCountDebugColor(inputData.positionWS, surfaceData.albedo, inputData.mipCount);
            return true;

        default:
            color = 0;
            return false;
    }
}

bool CalculateColorForDebugMaterial(in InputData inputData, in SurfaceData surfaceData, out half4 color)
{
    // Debug materials...
    switch(_DebugMaterialMode)
    {
        case DEBUGMATERIALMODE_ALBEDO:
            color = half4(surfaceData.albedo, 1);
            return true;

        case DEBUGMATERIALMODE_SPECULAR:
            color = half4(surfaceData.specular, 1);
            return true;

        case DEBUGMATERIALMODE_ALPHA:
            color = half4(surfaceData.alpha.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_SMOOTHNESS:
            color = half4(surfaceData.smoothness.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_AMBIENT_OCCLUSION:
            color = half4(surfaceData.occlusion.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_EMISSION:
            color = half4(surfaceData.emission, 1);
            return true;

        case DEBUGMATERIALMODE_NORMAL_WORLD_SPACE:
            color = half4(inputData.normalWS.xyz * 0.5 + 0.5, 1);
            return true;

        case DEBUGMATERIALMODE_NORMAL_TANGENT_SPACE:
            color = half4(surfaceData.normalTS.xyz * 0.5 + 0.5, 1);
            return true;

        case DEBUGMATERIALMODE_LOD:
            color = half4(GetLODDebugColor(), 1);
            return true;

        case DEBUGMATERIALMODE_METALLIC:
            color = half4(surfaceData.metallic.rrr, 1);
            return true;

        default:
            color = 0;
            return false;
    }
}

bool CalculateColorForDebug(in InputData inputData, in SurfaceData surfaceData, out half4 color)
{
    if(CalculateColorForDebugSceneOverride(color))
    {
        return true;
    }
    else if(CalculateColorForDebugMaterial(inputData, surfaceData, color))
    {
        return true;
    }
    else if(CalculateValidationColorForDebug(inputData, surfaceData, color))
    {
        return true;
    }
    else if(CalculateValidationColorForMipMaps(inputData, surfaceData, color))
    {
        return true;
    }
    else
    {
        color = 0;
        return false;
    }
}

#else

// When "_DEBUG_SHADER" isn't defined this macro does nothing - there's no debug-data to set-up...
#define SETUP_DEBUG_TEXTURE_DATA(inputData, uv, texture)

#endif

#endif
