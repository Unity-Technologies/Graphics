
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebuggingCommon.hlsl"

#if defined(_DEBUG_SHADER)

float _RangeMinimum;
float _RangeMaximum;
int _HighlightOutOfRangeAlpha;

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
        if(color.r < _RangeMinimum || color.g < _RangeMinimum || color.b < _RangeMinimum ||
            (_HighlightOutOfRangeAlpha && (color.a < _RangeMinimum)))
        {
            debugColor = _DebugValidateBelowMinThresholdColor;
        }
        else if(color.r > _RangeMaximum || color.g > _RangeMaximum || color.b > _RangeMaximum ||
                (_HighlightOutOfRangeAlpha && (color.a > _RangeMaximum)))
        {
            debugColor = _DebugValidateAboveMaxThresholdColor;
        }
        else
        {
            debugColor = half4(LinearRgbToLuminance(color.rgb).rrr, 1);
        }

        return true;
    }
    else
    {
        return false;
    }
}

#endif
