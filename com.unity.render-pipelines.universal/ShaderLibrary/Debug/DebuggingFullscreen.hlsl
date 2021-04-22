
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"

#if defined(DEBUG_DISPLAY)

float _RangeMinimum;
float _RangeMaximum;
int _HighlightOutOfRangeAlpha;

TEXTURE2D(_DebugTexture);        SAMPLER(sampler_DebugTexture);
half4 _DebugTextureDisplayRect;

bool CalculateDebugColorRenderingSettings(half4 color, float2 uv, inout half4 debugColor)
{
    switch(_DebugFullScreenMode)
    {
        case DEBUGFULLSCREENMODE_DEPTH:
        {
            half4 sampleColor = SAMPLE_TEXTURE2D(_DebugTexture, sampler_DebugTexture, uv);

            debugColor = half4(sampleColor.rrr, 1);
            return true;
        }

        case DEBUGFULLSCREENMODE_MAIN_LIGHT_SHADOW_MAP:
        case DEBUGFULLSCREENMODE_ADDITIONAL_LIGHTS_SHADOW_MAP:
        {
            float2 uvOffset = half2(uv.x - _DebugTextureDisplayRect.x, uv.y - _DebugTextureDisplayRect.y);

            if ((uvOffset.x >= 0) && (uvOffset.x < _DebugTextureDisplayRect.z) &&
                (uvOffset.y >= 0) && (uvOffset.y < _DebugTextureDisplayRect.w))
            {
                float2 debugTextureUv = float2(uvOffset.x / _DebugTextureDisplayRect.z, uvOffset.y / _DebugTextureDisplayRect.w);

                debugColor = SAMPLE_TEXTURE2D(_DebugTexture, sampler_DebugTexture, debugTextureUv);
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
#if !defined (SHADER_API_GLES)
            if (AnyIsNaN(color))
            {
                debugColor = half4(1, 0, 0, 1);
            }
            else if (AnyIsInf(color))
            {
                debugColor = half4(0, 1, 0, 1);
            }
            else
#endif
            if (color.r < 0 || color.g < 0 || color.b < 0 || color.a < 0)
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
            if (color.r < _RangeMinimum || color.g < _RangeMinimum || color.b < _RangeMinimum ||
                (_HighlightOutOfRangeAlpha && (color.a < _RangeMinimum)))
            {
                debugColor = _DebugValidateBelowMinThresholdColor;
            }
            else if (color.r > _RangeMaximum || color.g > _RangeMaximum || color.b > _RangeMaximum ||
                    (_HighlightOutOfRangeAlpha && (color.a > _RangeMaximum)))
            {
                debugColor = _DebugValidateAboveMaxThresholdColor;
            }
            else
            {
                debugColor = half4(Luminance(color).rrr, 1);
            }

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
