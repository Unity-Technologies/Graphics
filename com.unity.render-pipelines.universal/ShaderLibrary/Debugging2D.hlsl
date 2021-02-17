
#ifndef UNIVERSAL_DEBUGGING2D_INCLUDED
#define UNIVERSAL_DEBUGGING2D_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebuggingCommon.hlsl"

#if defined(_DEBUG_SHADER)
bool CalculateDebugColor(half3 albedo, half alpha, half3 mask, half2 lightingUV, out half4 debugColor)
{
    switch(_DebugMaterialMode)
    {
        case DEBUGMATERIALMODE_UNLIT:
        {
            debugColor = half4(albedo, 1);
            return true;
        }

        case DEBUGMATERIALMODE_ALPHA:
        {
            debugColor = half4(alpha, alpha, alpha, 1);
            return true;
        }

        case DEBUGMATERIALMODE_SPRITE_MASK:
        {
            debugColor = half4(mask, 1);
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
