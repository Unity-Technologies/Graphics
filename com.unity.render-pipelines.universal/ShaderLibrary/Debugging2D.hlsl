
#ifndef UNIVERSAL_DEBUGGING2D_INCLUDED
#define UNIVERSAL_DEBUGGING2D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebuggingCommon.hlsl"

#if defined(_DEBUG_SHADER)

void SetupDebugData(inout InputData2D inputData, float3 positionWS)
{
    inputData.positionWS = positionWS;

    // TODO: Pass the actual mipmap and texel data in here somehow, but we don't have access to textures here...
    const int textureWdith = 1024;
    const int textureHeight = 1024;
    inputData.texelSize = half4(1.0h / textureWdith, 1.0h / textureHeight, textureWdith, textureHeight);
    inputData.mipCount = 9;
}

bool CalculateDebugColorMaterialSettings(in SurfaceData2D surfaceData, in InputData2D inputData, out half4 debugColor)
{
    switch(_DebugMaterialMode)
    {
        case DEBUGMATERIALMODE_NONE:
        {
            debugColor = 0;
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
            debugColor = _DebugColorInvalidMode;
            return true;
        }
    }
}

bool CalculateDebugColorForRenderingSettings(in SurfaceData2D surfaceData, in InputData2D inputData, out half4 debugColor)
{
    if(CalculateColorForDebugSceneOverride(debugColor))
    {
        return true;
    }
    else
    {
        switch(_DebugMipInfoMode)
        {
            case DEBUGMIPINFOMODE_LEVEL:
            {
                debugColor = GetMipLevelDebugColor(inputData.positionWS, surfaceData.albedo, inputData.uv, inputData.texelSize);
                return true;
            }

            case DEBUGMIPINFOMODE_COUNT:
            {
                debugColor = GetMipCountDebugColor(inputData.positionWS, surfaceData.albedo, inputData.mipCount);
                return true;
            }

            default:
            {
                debugColor = 0;
                return false;
            }
        }
    }
}

bool CalculateDebugColorLightingSettings(in SurfaceData2D surfaceData, in InputData2D inputData, out half4 debugColor)
{
    switch(_DebugLightingMode)
    {
        case DEBUGLIGHTINGMODE_SHADOW_CASCADES:
        case DEBUGLIGHTINGMODE_REFLECTIONS:
        case DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS:
        {
            debugColor = _DebugColorInvalidMode;
            return true;
        }

        default:
        {
            debugColor = 0;
            return false;
        }
    }       // End of switch.
}

bool CalculateDebugColor(in SurfaceData2D surfaceData, in InputData2D inputData, out half4 debugColor)
{
    if(CalculateDebugColorMaterialSettings(surfaceData, inputData, debugColor))
    {
        return true;
    }
    else if(CalculateDebugColorForRenderingSettings(surfaceData, inputData, debugColor))
    {
        return true;
    }
    else if(CalculateDebugColorLightingSettings(surfaceData, inputData, debugColor))
    {
        return true;
    }
    else
    {
        debugColor = 0;
        return false;
    }
}
#endif

#endif
