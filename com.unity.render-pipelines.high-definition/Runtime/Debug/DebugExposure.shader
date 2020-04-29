Shader "Hidden/HDRP/DebugExposure"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/ExposureCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/HistogramExposureCommon.hlsl"
    #define DEBUG_DISPLAY
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    //TEXTURE2D(_ExposureTexture);

    // REMOVE
#pragma enable_d3d11_debug_symbols

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

    float GetHistogramValue(float coord, out bool isEdge)
    {
        float barSize = _ScreenSize.x / HISTOGRAM_BINS;
        float bin = coord / barSize;

        float locWithinBin = barSize * frac(bin);

        isEdge = locWithinBin < 1 || locWithinBin > (barSize - 1);
        return UnpackWeight(_HistogramBuffer[(uint)(bin)]);
    }

    void DrawHistogramIndicatorBar(uint coord, float uvXLocation, float widthNDC, float3 color, inout float3 outColor)
    {
        float halfWidthInScreen = widthNDC * _ScreenSize.x;
        float minScreenPos = (uvXLocation - widthNDC * 0.5) * _ScreenSize.x;
        float maxScreenPos = (uvXLocation + widthNDC * 0.5) * _ScreenSize.x;

        if (coord > minScreenPos && coord < maxScreenPos)
        {
            outColor = color;
        }
    }

    void DrawHistogramFrame(float2 uv, uint2 unormCoord, float frameHeight, float3 backgroundColor, float alpha, inout float3 outColor)
    {
        float2 borderSize = 2 * _ScreenSize.zw * _RTHandleScale.xy;

        if (uv.y > frameHeight) return;

        // ---- Draw General frame ---- 
        if (uv.x < borderSize.x || uv.x >(1.0f - borderSize.x)) outColor = 0.0;
        else if (uv.y > frameHeight - borderSize.y || uv.y < borderSize.y) outColor = 0.0;
        else
        outColor = lerp(outColor, backgroundColor, alpha);

        // ---- Draw Buckets frame ----
        float maxValue = 0;
        for (int i = 0; i <= HISTOGRAM_BINS; ++i)
        {
            float histogramVal = UnpackWeight(_HistogramBuffer[i]);
            maxValue = max(histogramVal, maxValue);
        }

        bool isEdgeOfBin = false;
        float val = GetHistogramValue(unormCoord.x, isEdgeOfBin);
        val /= maxValue;

        val *= (frameHeight * 0.9);
        if (uv.y < val)
        {
            isEdgeOfBin = isEdgeOfBin || (uv.y > val - _ScreenSize.w);
            
            outColor.rgb = float3(1.0f, 1.0f, 1.0f);
            if (isEdgeOfBin) outColor.rgb = 0;
        }

        // ---- Draw indicators ----
      //  void DrawHistogramIndicatorBar(uint coord, float uvXLocation, float widthNDC, float3 color, inout float3 outColor)
        float currExposure = _ExposureTexture[int2(0, 0)].y;
        float evInRange = (currExposure - ParamExposureLimitMin) / (ParamExposureLimitMax - ParamExposureLimitMin);
        DrawHistogramIndicatorBar(unormCoord.xy, evInRange, 0.002f, float3(0, 0, 1), outColor);


        // ---- Draw labels ---- 

        // Number of labels
        int labelCount = 12;
        float oneOverLabelCount = rcp(labelCount);
        float labelDeltaScreenSpace = _ScreenSize.x * oneOverLabelCount;

        int minLabelLocationX = DEBUG_FONT_TEXT_WIDTH * 0.25;
        int maxLabelLocationX = _ScreenSize.x - (DEBUG_FONT_TEXT_WIDTH * 3);

        int labelLocationY = DEBUG_FONT_TEXT_WIDTH * 0.15;

        [unroll]
        for (int i = 0; i <= labelCount; ++i)
        {
            float t = oneOverLabelCount * i;
            float labelValue = lerp(ParamExposureLimitMin, ParamExposureLimitMax, t);
            uint2 labelLoc = uint2((uint)lerp(minLabelLocationX, maxLabelLocationX, t), labelLocationY);
            DrawFloatExplicitPrecision(labelValue, float3(1.0f, 0, 0), unormCoord, 1, labelLoc, outColor.rgb);
        }
    }

    float GetEVAtLocation(float2 uv)
    {
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_SourceTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        float prevExposure = ConvertEV100ToExposure(GetPreviousExposureEV100());
        float luma = Luminance(color / prevExposure);

        return ComputeEV100FromAvgLuminance(max(luma, 1e-4));
    }

    float3 FragMetering(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_SourceTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        float weight = WeightSample(input.positionCS.xy, _ScreenSize.xy);

        return color * weight;// lerp(, color, 0.025f);
    }

    float3 FragSceneEV100(Varyings input) : SV_Target
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

        int2 unormCoord = input.positionCS.xy;

        int2 labelEVMinLoc = (sidebarBottomLeft * _ScreenSize.xy * (1.0f / _RTHandleScale.xy)) + int2(-(sidebarSize.x * _ScreenSize.x + DEBUG_FONT_TEXT_WIDTH), 0);
        int2 textLocation = labelEVMinLoc;
        DrawFloatExplicitPrecision(ParamExposureLimitMin, textColor, unormCoord, 1, textLocation, outputColor.rgb);
        int2 labelEVMaxLoc = (sidebarBottomLeft * _ScreenSize.xy * (1.0f / _RTHandleScale.xy)) + int2(-(sidebarSize.x * _ScreenSize.x + DEBUG_FONT_TEXT_WIDTH), sidebarSize.y * 0.98 * _ScreenSize.y);
        textLocation = labelEVMaxLoc;
        DrawFloatExplicitPrecision(ParamExposureLimitMax, textColor, unormCoord, 1, textLocation, outputColor.rgb);

        int displayTextOffsetX = DEBUG_FONT_TEXT_WIDTH;
        textLocation = int2(_MousePixelCoord.x + displayTextOffsetX, _MousePixelCoord.y);
        DrawFloatExplicitPrecision(indicatorEV, textColor, unormCoord, 1, textLocation, outputColor.rgb);


        return outputColor;
    }



    float3 FragHistogram(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;



        float3 color = SAMPLE_TEXTURE2D_X_LOD(_SourceTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        float weight = WeightSample(input.positionCS.xy, _ScreenSize.xy);

        float3 outputColor = color;
        DrawHistogramFrame(uv, input.positionCS.xy, 0.2 * _RTHandleScale.y, ToHeat(uv.x), 0.1f, outputColor);

        return outputColor;// lerp(, color, 0.025f);
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
            #pragma fragment FragSceneEV100
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragMetering
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragHistogram
            ENDHLSL
        }

    }
    Fallback Off
}
