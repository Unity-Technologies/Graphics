Shader "Hidden/HDRP/DebugExposure"
{
    HLSLINCLUDE

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/ExposureCommon.hlsl"
#define DEBUG_DISPLAY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);

        return output;
    }

    float3 ToHeat(float value)
    {
        float3 r = value * 2.1f - float3(1.8f, 1.14f, 0.3f);
        return 1.0f - r * r;
    }

    // Returns true if it drew the location of the indicator.
    void DrawHeatSideBar(float2 uv, float2 startSidebar, float2 endSidebar, float evValueRange, float3 indicatorColor, float2 sidebarSize, inout float3 sidebarColor)
    {
        float2 borderSize = 2 * _ScreenSize.zw;
        uint indicatorHalfSize = 5;

        if (all(uv > startSidebar) && all(uv < endSidebar))
        {
            float inRange = (uv.y - startSidebar.y) / (endSidebar.y - startSidebar.y);
            evValueRange = clamp(evValueRange, 0.0f, 1.0f);
            int distanceInPixels = abs(evValueRange - inRange) * sidebarSize.y * _ScreenSize.y;
            if (distanceInPixels < indicatorHalfSize)
            {
                sidebarColor = indicatorColor;
            }
            else if (distanceInPixels < indicatorHalfSize + 1)
            {
                sidebarColor = 0.0f;
            }
            else
            {
                sidebarColor = ToHeat(inRange);
            }
        }
        else if(all(uv > startSidebar - borderSize) && all(uv < endSidebar + borderSize))
        {
            sidebarColor = 0.0f;
        }
    }

    float GetEVAtLocation(float2 uv)
    {
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_SourceTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        float prevExposure = ConvertEV100ToExposure(GetPreviousExposureEV100());
        float luma = Luminance(color / prevExposure);

        return ComputeEV100FromAvgLuminance(max(luma, 1e-4));
    }

    float3 Frag(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;

        float3 textColor = 0.0f;

        // TODO: Should they be in pixels? ASK UX!
        float2 sidebarSize = float2(0.025, 0.7);

        float2 sidebarBottomLeft = float2(0.04, (1.0 - sidebarSize.y) * 0.5) * _RTHandleScale.xy;
        float2 endPointSidebar = sidebarBottomLeft + sidebarSize * _RTHandleScale.xy;

        float3 outputColor = 0;
        float ev = GetEVAtLocation(uv);

        float evInRange = (ev - ParamExposureLimitMin) / (ParamExposureLimitMax - ParamExposureLimitMin);

        if (ev < ParamExposureLimitMax && ev > ParamExposureLimitMin)
        {
            outputColor = ToHeat(evInRange);
        }
        else if (ev > ParamExposureLimitMax)                 // << TODO_FCC: Ask UX TODO what color scheme is good here.
        {
            outputColor = 1.0f;
        }
        else if (ev < ParamExposureLimitMin)
        {
            outputColor = 0.0f;
        }

        // Get value at indicator
        float2 indicatorUV = _MousePixelCoord.zw;
        float indicatorEV = GetEVAtLocation(indicatorUV);
        float indicatorEVRange = (indicatorEV - ParamExposureLimitMin) / (ParamExposureLimitMax - ParamExposureLimitMin);

        DrawHeatSideBar(uv, sidebarBottomLeft, endPointSidebar, indicatorEVRange, 0.66f, sidebarSize, outputColor);

        uint2 unormCoord = input.positionCS.xy;

        uint2 labelEVMinLoc = (sidebarBottomLeft * _ScreenSize.xy * (1.0f / _RTHandleScale.xy)) + int2(-(sidebarSize.x * _ScreenSize.x + DEBUG_FONT_TEXT_WIDTH), 0);
        uint2 textLocation = labelEVMinLoc;
        DrawFloatExplicitPrecision(ParamExposureLimitMin, textColor, unormCoord, 1, textLocation, outputColor.rgb);
        uint2 labelEVMaxLoc = (sidebarBottomLeft * _ScreenSize.xy * (1.0f / _RTHandleScale.xy)) + int2(-(sidebarSize.x * _ScreenSize.x + DEBUG_FONT_TEXT_WIDTH), sidebarSize.y * 0.98 * _ScreenSize.y);
        textLocation = labelEVMaxLoc;
        DrawFloatExplicitPrecision(ParamExposureLimitMax, textColor, unormCoord, 1, textLocation, outputColor.rgb);

        int displayTextOffsetX = 1.5 * DEBUG_FONT_TEXT_WIDTH;
        textLocation = uint2(_MousePixelCoord.x + displayTextOffsetX, _MousePixelCoord.y);
        DrawFloatExplicitPrecision(indicatorEV, textColor, unormCoord, 1, textLocation, outputColor.rgb);

        return outputColor;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma enable_d3d11_debug_symbols // TODO_FCC TODO : REMOVE
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }

    }
    Fallback Off
}
