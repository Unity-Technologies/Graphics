
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"

#if defined(_DEBUG_SHADER)

int _ValidationChannels;
float _RangeMinimum;
float _RangeMaximum;

bool CalculateDebugColor(half4 color, out half4 debugColor)
{
    if(_DebugValidationMode == DEBUGVALIDATIONMODE_HIGHLIGHT_NAN_INF_NEGATIVE)
    {
        if (isnan(color.r) || isnan(color.g) || isnan(color.b) || isnan(color.a))
        {
            debugColor = half4(1, 0, 0, 1);
        }
        else if (isinf(color.r) || isinf(color.g) || isinf(color.b) || isinf(color.a))
        {
            debugColor = half4(0, 1, 0, 1);
        }
        else if (color.r < 0 || color.g < 0 || color.b < 0 || color.a < 0)
        {
            debugColor = half4(0, 0, 1, 1);
        }
        else
        {
            debugColor = half4(LinearRgbToLuminance(color.rgb).rrr, 1);
        }

        return true;
    }
    else if(_DebugValidationMode == DEBUGVALIDATIONMODE_HIGHLIGHT_OUTSIDE_OF_RANGE)
    {
        float val;
        if (_ValidationChannels == PIXELVALIDATIONCHANNELS_RGB)
        {
            val = LinearRgbToLuminance(color.rgb);
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
            debugColor = half4(LinearRgbToLuminance(color.rgb).rrr, 1);

        return true;
    }
    else
    {
        return false;
    }
}

#endif
