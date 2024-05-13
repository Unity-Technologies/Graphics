
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingCommon.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

#if defined(DEBUG_DISPLAY)

int _DebugMaxPixelCost;
int _ValidationChannels;
float _RangeMinimum;
float _RangeMaximum;

TEXTURE2D_X(_DebugTexture);
TEXTURE2D(_DebugTextureNoStereo);

// 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
#define sampler_DebugTexture sampler_PointClamp

half4 _DebugTextureDisplayRect;
int _DebugRenderTargetSupportsStereo;
float4 _DebugRenderTargetRangeRemap;

// CPU parametrized, non-clamping, range remap. (RangeRemap in common.hlsl saturates!)
half4 RemapSourceRange(half4 source)
{
    float4 r = _DebugRenderTargetRangeRemap;
    float4 s = source;
    // Remap(float origFrom, float origTo, float targetFrom, float targetTo, float value)
    s.r = Remap(r.x, r.y, r.z, r.w, s.r);
    s.g = Remap(r.x, r.y, r.z, r.w, s.g);
    s.b = Remap(r.x, r.y, r.z, r.w, s.b);
    s.a = Remap(r.x, r.y, r.z, r.w, s.a);
    return half4(s);
}

bool CalculateDebugColorRenderingSettings(half4 color, float2 uv, inout half4 debugColor)
{
    if (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_OVERDRAW)
    {
        // color.r is (Number of overdraw / Max displayed overdraw count)
        debugColor.rgb = GetOverdrawColor(color.r * _DebugMaxPixelCost, _DebugMaxPixelCost).rgb;
        DrawOverdrawLegend(uv, _DebugMaxPixelCost, _ScreenSize, debugColor.rgb);
        return true;
    }

    if (_DebugMipInfoMode != DEBUGMIPINFOMODE_NONE)
    {
        debugColor = color; // just passing through

        // draw legend
        switch(_DebugMipInfoMode)
        {
            case DEBUGMIPINFOMODE_MIP_COUNT:
                DrawMipCountLegend(uv, _ScreenSize, debugColor.rgb);
                break;
            case DEBUGMIPINFOMODE_MIP_RATIO:
                DrawMipRatioLegend(uv, _ScreenSize, debugColor.rgb);
                break;
            case DEBUGMIPINFOMODE_MIP_STREAMING_STATUS:
                if (_DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_TEXTURE)
                    DrawMipStreamingStatusLegend(uv, _ScreenSize, _DebugMipMapShowStatusCode, debugColor.rgb);
                else
                    DrawMipStreamingStatusPerMaterialLegend(uv, _ScreenSize, debugColor.rgb);
                break;
            case DEBUGMIPINFOMODE_MIP_STREAMING_PERFORMANCE:
                DrawTextureStreamingPerformanceLegend(uv, _ScreenSize, debugColor.rgb);
                break;
            case DEBUGMIPINFOMODE_MIP_STREAMING_PRIORITY:
                DrawMipPriorityLegend(uv, _ScreenSize, debugColor.rgb);
                break;
            case DEBUGMIPINFOMODE_MIP_STREAMING_ACTIVITY:
                DrawMipRecentlyUpdatedLegend(uv, _ScreenSize, _DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_MATERIAL, debugColor.rgb);
                break;
        }

        return true;
    }

    switch(_DebugFullScreenMode)
    {
        case DEBUGFULLSCREENMODE_DEPTH:
        case DEBUGFULLSCREENMODE_MOTION_VECTOR:
        case DEBUGFULLSCREENMODE_MAIN_LIGHT_SHADOW_MAP:
        case DEBUGFULLSCREENMODE_ADDITIONAL_LIGHTS_SHADOW_MAP:
        case DEBUGFULLSCREENMODE_ADDITIONAL_LIGHTS_COOKIE_ATLAS:
        case DEBUGFULLSCREENMODE_REFLECTION_PROBE_ATLAS:
        case DEBUGFULLSCREENMODE_STP:
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

                // Optionally remap source to valid visualization range.
                if(any(_DebugRenderTargetRangeRemap != 0))
                {
                    sampleColor.rgb = RemapSourceRange(sampleColor).rgb;
                }

                if (_DebugFullScreenMode == DEBUGFULLSCREENMODE_DEPTH)
                {
                    debugColor = half4(sampleColor.rrr, 1);
                }
                else if (_DebugFullScreenMode == DEBUGFULLSCREENMODE_MOTION_VECTOR)
                {
                    // Motion vector is RG only.
                    debugColor = half4(sampleColor.rg, 0, 1);
                }
                else if (_DebugFullScreenMode == DEBUGFULLSCREENMODE_STP)
                {
                    // This is encoded in gamma 2.0 (so the square is needed to get it back to linear).
                    debugColor = sampleColor * sampleColor;
                }
                else
                {
                    debugColor = sampleColor;
                }

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
