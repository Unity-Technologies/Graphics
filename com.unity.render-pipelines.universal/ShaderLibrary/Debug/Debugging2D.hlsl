
#ifndef UNIVERSAL_DEBUGGING2D_INCLUDED
#define UNIVERSAL_DEBUGGING2D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"

#if defined(DEBUG_DISPLAY)

#define SETUP_DEBUG_TEXTURE_DATA_2D(inputData, positionWS, texture)    SetupDebugDataTexture(inputData, positionWS, texture##_TexelSize, texture##_MipInfo, GetMipCount(TEXTURE2D_ARGS(texture, sampler##texture)))
#define SETUP_DEBUG_DATA_2D(inputData, positionWS)                     SetupDebugData(inputData, positionWS)

void SetupDebugData(inout InputData2D inputData, float3 positionWS)
{
    inputData.positionWS = positionWS;
}

void SetupDebugDataTexture(inout InputData2D inputData, float3 positionWS, float4 texelSize, float4 mipInfo, uint mipCount)
{
    SetupDebugData(inputData, positionWS);

    inputData.texelSize = texelSize;
    inputData.mipInfo = mipInfo;
    inputData.mipCount = mipCount;
}

bool CalculateDebugColorMaterialSettings(in SurfaceData2D surfaceData, in InputData2D inputData, inout half4 debugColor)
{
    switch(_DebugMaterialMode)
    {
        case DEBUGMATERIALMODE_NONE:
        {
            return false;
        }

        case DEBUGMATERIALMODE_ALBEDO:
        {
            debugColor = half4(surfaceData.albedo, 1);
            return true;
        }

        case DEBUGMATERIALMODE_ALPHA:
        {
            debugColor = half4(surfaceData.alpha.rrr, 1);
            return true;
        }

        case DEBUGMATERIALMODE_SPRITE_MASK:
        {
            debugColor = surfaceData.mask;
            return true;
        }

        case DEBUGMATERIALMODE_NORMAL_TANGENT_SPACE:
        case DEBUGMATERIALMODE_NORMAL_WORLD_SPACE:
        {
            debugColor = half4(surfaceData.normalTS, 1);
            return true;
        }

        default:
        {
            return TryGetDebugColorInvalidMode(debugColor);
        }
    }
}

bool CalculateDebugColorForRenderingSettings(in SurfaceData2D surfaceData, in InputData2D inputData, inout half4 debugColor)
{
    if (CalculateColorForDebugSceneOverride(debugColor))
    {
        return true;
    }
    return TryGetDebugColorInvalidMode(debugColor);
}

bool CalculateDebugColorLightingSettings(inout SurfaceData2D surfaceData, inout InputData2D inputData, inout half4 debugColor)
{
    switch(_DebugLightingMode)
    {
        case DEBUGLIGHTINGMODE_NONE:
        {
            return false;
        }

        case DEBUGLIGHTINGMODE_LIGHTING_WITHOUT_NORMAL_MAPS:
        case DEBUGLIGHTINGMODE_LIGHTING_WITH_NORMAL_MAPS:
        {
            surfaceData.albedo = 1;
            return false;
        }

        default:
        {
            return TryGetDebugColorInvalidMode(debugColor);
        }
    }       // End of switch.
}

bool CalculateDebugColorValidationSettings(in SurfaceData2D surfaceData, in InputData2D inputData, inout half4 debugColor)
{
    switch(_DebugMaterialValidationMode)
    {
        case DEBUGMATERIALVALIDATIONMODE_NONE:
            return false;

        case DEBUGMATERIALVALIDATIONMODE_ALBEDO:
            return CalculateValidationAlbedo(surfaceData.albedo, debugColor);

        default:
            return TryGetDebugColorInvalidMode(debugColor);
    }
}

bool CanDebugOverrideOutputColor(inout SurfaceData2D surfaceData, inout InputData2D inputData, inout half4 debugColor)
{
    if (CalculateDebugColorMaterialSettings(surfaceData, inputData, debugColor))
    {
        return _DebugMaterialMode != DEBUGMATERIALMODE_SPRITE_MASK;
    }
    else if (CalculateDebugColorForRenderingSettings(surfaceData, inputData, debugColor))
    {
        return true;
    }
    else if (CalculateDebugColorLightingSettings(surfaceData, inputData, debugColor))
    {
        return true;
    }
    else if (CalculateDebugColorValidationSettings(surfaceData, inputData, debugColor))
    {
        return true;
    }
    else
    {
        return false;
    }
}

#else

#define SETUP_DEBUG_TEXTURE_DATA_2D(inputData, positionWS, texture)
#define SETUP_DEBUG_DATA_2D(inputData, positionWS)

#endif

#endif
