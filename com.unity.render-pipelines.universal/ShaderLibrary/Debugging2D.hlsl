
#ifndef UNIVERSAL_DEBUGGING2D_INCLUDED
#define UNIVERSAL_DEBUGGING2D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebuggingCommon.hlsl"

#if defined(_DEBUG_SHADER)

bool CalculateDebugColor(in SurfaceData2D surfaceData, out half4 debugColor)
{
    switch(_DebugMaterialMode)
    {
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

        case DEBUGMATERIALMODE_NONE:
        {
            return CalculateColorForDebugSceneOverride(debugColor);
        }

        default:
        {
            // We cannot display anything sensible for this mode - so display a color which tells us this...
            debugColor = half4(0.5h, 0.25h, 0, 1);
            return true;
        }
    }
}
#endif

#endif
