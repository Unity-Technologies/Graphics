
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

#if defined(DEBUG_DISPLAY)

int _DebugMaxPixelCost;
int _ValidationChannels;
float _RangeMinimum;
float _RangeMaximum;

TEXTURE2D_X(_DebugTexture);
TEXTURE2D(_DebugTextureNoStereo);
SAMPLER(sampler_DebugTexture);
half4 _DebugTextureDisplayRect;
int _DebugRenderTargetSupportsStereo;

bool CalculateDebugColorRenderingSettings(half4 color, float2 uv, inout half4 debugColor)
{
    if (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_OVERDRAW)
    {
        // color.r is (Number of overdraw / Max displayed overdraw count)
        debugColor.rgb = GetOverdrawColor(color.r * _DebugMaxPixelCost, _DebugMaxPixelCost).rgb;
        DrawOverdrawLegend(uv, _DebugMaxPixelCost, _ScreenSize, debugColor.rgb);
        return true;
    }

    switch(_DebugFullScreenMode)
    {
        case DEBUGFULLSCREENMODE_DEPTH:
        case DEBUGFULLSCREENMODE_MAIN_LIGHT_SHADOW_MAP:
        case DEBUGFULLSCREENMODE_ADDITIONAL_LIGHTS_SHADOW_MAP:
        case DEBUGFULLSCREENMODE_REFLECTION_PROBE_ATLAS:
        {
            float2 uvOffset = half2(uv.x - _DebugTextureDisplayRect.x, uv.y - _DebugTextureDisplayRect.y);

            if ((uvOffset.x >= 0) && (uvOffset.x < _DebugTextureDisplayRect.z) &&
                (uvOffset.y >= 0) && (uvOffset.y < _DebugTextureDisplayRect.w))
            {
                float2 debugTextureUv = float2(uvOffset.x / _DebugTextureDisplayRect.z, uvOffset.y / _DebugTextureDisplayRect.w);

                half4 sampleColor = (half4)0;
                if (_DebugRenderTargetSupportsStereo == 1)
                    sampleColor = SAMPLE_TEXTURE2D_X(_DebugTexture, sampler_DebugTexture, debugTextureUv);
                else
                    sampleColor = SAMPLE_TEXTURE2D(_DebugTextureNoStereo, sampler_DebugTexture, debugTextureUv);

                debugColor = _DebugFullScreenMode == DEBUGFULLSCREENMODE_DEPTH ? half4(sampleColor.rrr, 1) : sampleColor;
                return true;
            }
            else
            {
                return false;
            }
        }

        default:
        {
            return false;
        }
    }       // End of switch.
}

bool CalculateDebugColorValidationSettings(half4 color, float2 uv, inout half4 debugColor)
{
    switch(_DebugValidationMode)
    {
        case DEBUGVALIDATIONMODE_HIGHLIGHT_NAN_INF_NEGATIVE:
        {
            if (AnyIsNaN(color))
            {
                debugColor = half4(1, 0, 0, 1);
            }
            else if (AnyIsInf(color))
            {
                debugColor = half4(0, 1, 0, 1);
            }
            else if (color.r < 0 || color.g < 0 || color.b < 0 || color.a < 0)
            {
                debugColor = half4(0, 0, 1, 1);
            }
            else
            {
                debugColor = half4(Luminance(color).rrr, 1);
            }

            return true;
        }

        case DEBUGVALIDATIONMODE_HIGHLIGHT_OUTSIDE_OF_RANGE:
        {
            float val;
            if (_ValidationChannels == PIXELVALIDATIONCHANNELS_RGB)
            {
                val = Luminance(color.rgb);
            }
            else if (_ValidationChannels == PIXELVALIDATIONCHANNELS_R)
            {
                val = color.r;
            }
            else if (_ValidationChannels == PIXELVALIDATIONCHANNELS_G)
            {
                val = color.g;
            }
            else if (_ValidationChannels == PIXELVALIDATIONCHANNELS_B)
            {
                val = color.b;
            }
            else if (_ValidationChannels == PIXELVALIDATIONCHANNELS_A)
            {
                val = color.a;
            }

            if (val < _RangeMinimum)
                debugColor = _DebugValidateBelowMinThresholdColor;
            else if (val > _RangeMaximum)
                debugColor = _DebugValidateAboveMaxThresholdColor;
            else
                debugColor = half4(Luminance(color.rgb).rrr, 1);

            return true;
        }

        default:
        {
            return false;
        }
    }       // End of switch.
}

bool CanDebugOverrideOutputColor(half4 color, float2 uv, inout half4 debugColor)
{
    return CalculateDebugColorRenderingSettings(color, uv, debugColor) ||
           CalculateDebugColorValidationSettings(color, uv, debugColor);
}

#endif
