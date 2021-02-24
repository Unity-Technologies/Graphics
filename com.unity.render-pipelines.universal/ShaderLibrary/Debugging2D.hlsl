
#ifndef UNIVERSAL_DEBUGGING2D_INCLUDED
#define UNIVERSAL_DEBUGGING2D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebuggingCommon.hlsl"

#if defined(_DEBUG_SHADER)

bool CalculateDebugColorMaterialSettings(in SurfaceData2D surfaceData, out half4 debugColor)
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

bool CalculateDebugColorForRenderingSettings(in SurfaceData2D surfaceData, out half4 debugColor)
{
    return CalculateColorForDebugSceneOverride(debugColor);
}

bool CalculateDebugColorLightingSettings(in SurfaceData2D surfaceData, out half4 debugColor)
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

bool CalculateDebugColor(in SurfaceData2D surfaceData, out half4 debugColor)
{
    if(CalculateDebugColorMaterialSettings(surfaceData, debugColor))
    {
        return true;
    }
    else if(CalculateDebugColorForRenderingSettings(surfaceData, debugColor))
    {
        return true;
    }
    else if(CalculateDebugColorLightingSettings(surfaceData, debugColor))
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
