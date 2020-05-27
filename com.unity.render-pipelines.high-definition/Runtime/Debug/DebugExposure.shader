Shader "Hidden/HDRP/DebugExposure"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Components/Tonemapping.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/ExposureCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/HistogramExposureCommon.hlsl"
    #define DEBUG_DISPLAY
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #define PERCENTILE_AS_BARS 0

    // Contains the scene color post-processed (tonemapped etc.)
    TEXTURE2D_X(_DebugFullScreenTexture);

    // Tonemap related 
    TEXTURE3D(_LogLut3D);
    SAMPLER(sampler_LogLut3D);

    float4 _ExposureDebugParams;
    float4 _LogLut3D_Params;    // x: 1 / lut_size, y: lut_size - 1, z: contribution, w: unused
    // Custom tonemapping settings
    float4 _CustomToneCurve;
    float4 _ToeSegmentA;
    float4 _ToeSegmentB;
    float4 _MidSegmentA;
    float4 _MidSegmentB;
    float4 _ShoSegmentA;
    float4 _ShoSegmentB;

    #define _DrawTonemapCurve   _ExposureDebugParams.x
    #define _TonemapType        _ExposureDebugParams.y


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

    float3 Tonemap(float3 colorLinear)
    {
        if(_TonemapType == TONEMAPPINGMODE_NEUTRAL) 
        {
            colorLinear = NeutralTonemap(colorLinear);
        }
        if (_TonemapType == TONEMAPPINGMODE_ACES) 
        {
            // Note: input is actually ACEScg (AP1 w/ linear encoding)
            float3 aces = ACEScg_to_ACES(colorLinear);
            colorLinear = AcesTonemap(aces);
        }
        if (_TonemapType == TONEMAPPINGMODE_CUSTOM) // Custom
        {
            colorLinear = CustomTonemap(colorLinear, _CustomToneCurve.xyz, _ToeSegmentA, _ToeSegmentB.xy, _MidSegmentA, _MidSegmentB.xy, _ShoSegmentA, _ShoSegmentB.xy);
        }
        if (_TonemapType == TONEMAPPINGMODE_EXTERNAL) // External
        {
            float3 colorLutSpace = saturate(LinearToLogC(colorLinear));
            float3 colorLut = ApplyLut3D(TEXTURE3D_ARGS(_LogLut3D, sampler_LogLut3D), colorLutSpace, _LogLut3D_Params.xy);
            colorLinear = lerp(colorLinear, colorLut, _LogLut3D_Params.z);
        }

        return colorLinear;
    }

    float3 ToHeat(float value)
    {
        float3 r = value * 2.1f - float3(1.8f, 1.14f, 0.3f);
        return 1.0f - r * r;
    }

    float GetEVAtLocation(float2 uv)
    {
        return ComputeEV100FromAvgLuminance(max(SampleLuminance(uv), 1e-4));
    }

    // Returns true if it drew the location of the indicator.
    void DrawHeatSideBar(float2 uv, float2 startSidebar, float2 endSidebar, float evValueRange, float3 indicatorColor, float2 sidebarSize, float extremeMargin, inout float3 sidebarColor)
    {
        float2 extremesSize = float2(extremeMargin, 0);
        float2 borderSize = 2 * _ScreenSize.zw * _RTHandleScale.xy;
        int indicatorHalfSize = 5;


        if (all(uv > startSidebar) && all(uv < endSidebar))
        {
            float inRange = (uv.x - startSidebar.x) / (endSidebar.x - startSidebar.x);
            evValueRange = clamp(evValueRange, 0.0f, 1.0f);
            int distanceInPixels = abs(evValueRange - inRange) * sidebarSize.x * _ScreenSize.x;
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
        else if (all(uv > startSidebar - extremesSize) && all(uv < endSidebar))
        {
            sidebarColor = float3(0,0,0);
        }
        else if (all(uv > startSidebar) && all(uv < endSidebar + extremesSize))
        {
            sidebarColor = float3(1, 1, 1);
        }
        else if(all(uv > startSidebar - (extremesSize + borderSize)) && all(uv < endSidebar + (extremesSize + borderSize)))
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

    float ComputePercentile(float2 uv, float histSum, out float minPercentileBin, out float maxPercentileBin)
    {
        float sumBelowValue = 0.0f;
        float sumForMin = 0.0f;
        float sumForMax = 0.0f;

        minPercentileBin = -1;
        maxPercentileBin = -1;

        float ev = GetEVAtLocation(uv);

        for (int i = 0; i < HISTOGRAM_BINS; ++i)
        {
            float evAtBin = BinLocationToEV(i);
            float evAtNextBin = BinLocationToEV(i+1);

            float histVal = UnpackWeight(_HistogramBuffer[i]);

            if (ev >= evAtBin)
            {
                sumBelowValue += histVal;
            }

             //TODO: This could be more precise, now it locks to bin location
            if (minPercentileBin < 0)
            {
                sumForMin += histVal;
                if (sumForMin / histSum >= _HistogramMinPercentile)
                {

                    minPercentileBin = i;
                }
            }

            if (maxPercentileBin < 0)
            {
                sumForMax += histVal;
                if (sumForMax / histSum > _HistogramMaxPercentile)
                {
                    maxPercentileBin = i;
                }
            }
        }

        return sumBelowValue / histSum;
    }

    void DrawHistogramIndicatorBar(float coord, float uvXLocation, float widthNDC, float3 color, inout float3 outColor)
    {
        float halfWidthInScreen = widthNDC * _ScreenSize.x;
        float minScreenPos = (uvXLocation - widthNDC * 0.5) * _ScreenSize.x;
        float maxScreenPos = (uvXLocation + widthNDC * 0.5) * _ScreenSize.x;

        if (coord > minScreenPos && coord < maxScreenPos)
        {
            outColor = color;
        }
    }

    void DrawTriangleIndicator(float2 coord, float labelBarHeight, float uvXLocation, float widthNDC, float3 color, inout float3 outColor)
    {
        float halfWidthInScreen = widthNDC * _ScreenSize.x;
        float arrowStart = labelBarHeight * 0.4f;

        float heightInIndicator = ((coord.y - arrowStart) / (labelBarHeight - arrowStart));
        float indicatorWidth = 1.0f - heightInIndicator;

        float minScreenPos = (uvXLocation - widthNDC * indicatorWidth * 0.5) * _ScreenSize.x;
        float maxScreenPos = (uvXLocation + widthNDC * indicatorWidth * 0.5) * _ScreenSize.x;

        uint triangleBorder = 2;
        if (coord.x > minScreenPos && coord.x < maxScreenPos && coord.y >= arrowStart)
        {
            outColor = color;
        }
        else if (coord.x > minScreenPos - triangleBorder && coord.x < maxScreenPos + triangleBorder && coord.y > arrowStart - triangleBorder)
        {
            outColor = 0;
        }
    }

    void DrawHistogramFrame(float2 uv, uint2 unormCoord, float frameHeight, float3 backgroundColor, float alpha, float maxHist, float minPercentLoc, float maxPercentLoc, inout float3 outColor)
    {
        float2 borderSize = 2 * _ScreenSize.zw * _RTHandleScale.xy;
        float heightLabelBar = (DEBUG_FONT_TEXT_WIDTH * 1.25) * _ScreenSize.w * _RTHandleScale.y;

        if (uv.y > frameHeight) return;

        // ---- Draw General frame ---- 
        if (uv.x < borderSize.x || uv.x >(1.0f - borderSize.x))
        {
            outColor = 0.0;
            return;
        }
        else if (uv.y > frameHeight - borderSize.y || uv.y < borderSize.y)
        {
            outColor = 0.0;
            return;
        }
        else
        {
            outColor = lerp(outColor, backgroundColor, alpha);
        }

        // ----  Draw label bar -----
        if (uv.y < heightLabelBar)
        {
            outColor = outColor * 0.075f;
        }

        // ---- Draw Buckets frame ----

        bool isEdgeOfBin = false;
        float val = GetHistogramValue(unormCoord.x, isEdgeOfBin);
        val /= maxHist;

        val *= 0.95*(frameHeight - heightLabelBar);
        val += heightLabelBar;

        if (uv.y < val && uv.y > heightLabelBar)
        {
            isEdgeOfBin = isEdgeOfBin || (uv.y > val - _ScreenSize.w);
#if PERCENTILE_AS_BARS == 0
            uint bin = uint((unormCoord.x * (HISTOGRAM_BINS)) / (_ScreenSize.x));
            if (bin <= uint(minPercentLoc) && minPercentLoc > 0)
            {
                outColor.rgb = float3(0, 0, 1);
            }
            else if(bin >= uint(maxPercentLoc) && maxPercentLoc > 0)
            {
                outColor.rgb = float3(1, 0, 0);
            }
            else
#endif
            outColor.rgb = float3(1.0f, 1.0f, 1.0f);
            if (isEdgeOfBin) outColor.rgb = 0;
        }

        // ---- Draw labels ---- 

        // Number of labels
        int labelCount = 12;
        float oneOverLabelCount = rcp(labelCount);
        float labelDeltaScreenSpace = _ScreenSize.x * oneOverLabelCount;

        int minLabelLocationX = DEBUG_FONT_TEXT_WIDTH * 0.25;
        int maxLabelLocationX = _ScreenSize.x - (DEBUG_FONT_TEXT_WIDTH * 3);

        int labelLocationY = 0.0f;

        [unroll]
        for (int i = 0; i <= labelCount; ++i)
        {
            float t = oneOverLabelCount * i;
            float labelValue = lerp(ParamExposureLimitMin, ParamExposureLimitMax, t);
            uint2 labelLoc = uint2((uint)lerp(minLabelLocationX, maxLabelLocationX, t), labelLocationY);
            DrawFloatExplicitPrecision(labelValue, float3(1.0f, 1.0f, 1.0f), unormCoord, 1, labelLoc, outColor.rgb);
        }

        // ---- Draw indicators ----
        float currExposure = _ExposureTexture[int2(0, 0)].y;
        float targetExposure = _ExposureDebugTexture[int2(0, 0)].x;

        float evInRange = (currExposure - ParamExposureLimitMin) / (ParamExposureLimitMax - ParamExposureLimitMin);
        float targetEVInRange = (targetExposure - ParamExposureLimitMin) / (ParamExposureLimitMax - ParamExposureLimitMin);

        float halfIndicatorSize = 0.007f;
        float halfWidthInScreen = halfIndicatorSize * _ScreenSize.x;

        float labelFrameHeightScreen = heightLabelBar * (_ScreenSize.y / _RTHandleScale.y);

        if (uv.y < heightLabelBar)
        {
            DrawTriangleIndicator(float2(unormCoord.xy), labelFrameHeightScreen, targetEVInRange, halfIndicatorSize, float3(0.9f, 0.75f, 0.1f), outColor);
            DrawTriangleIndicator(float2(unormCoord.xy), labelFrameHeightScreen, evInRange, halfIndicatorSize, float3(0.15f, 0.15f, 0.1f), outColor);

             // Find location for percentiles bars.
#if PERCENTILE_AS_BARS
            DrawHistogramIndicatorBar(float(unormCoord.x), minPercentLoc, 0.003f, float3(0, 0, 1), outColor);
            DrawHistogramIndicatorBar(float(unormCoord.x), maxPercentLoc, 0.003f, float3(1, 0, 0), outColor);
#endif
        }

        // ---- Draw Tonemap curve ----
        if (_DrawTonemapCurve)
        {
            float exposureAtLoc = lerp(ParamExposureLimitMin, ParamExposureLimitMax, uv.x);
            const float K = 12.5; // Reflected-light meter calibration constant
            float luminanceFromExposure = _ExposureTexture[int2(0, 0)].x * (exp2(exposureAtLoc) * (K / 100.0f));

            val = saturate(Luminance(Tonemap(luminanceFromExposure)));
            val *= 0.95 * (frameHeight - heightLabelBar);
            val += heightLabelBar;

            float curveWidth = 4 * _ScreenSize.w;

            if (uv.y < val && uv.y >(val - curveWidth))
            {
                outColor = outColor * 0.1 + 0.9 * 0;
            }
        }
    }

    float3 FragMetering(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        float weight = WeightSample(input.positionCS.xy, _ScreenSize.xy);

        float pipFraction = 0.33f;
        uint borderSize = 3;
        float2 topRight = pipFraction * _ScreenSize.xy;

        if (all(input.positionCS.xy < topRight))
        {
            float2 scaledUV = uv / pipFraction;
            float3 pipColor = SAMPLE_TEXTURE2D_X_LOD(_SourceTexture, s_linear_clamp_sampler, scaledUV, 0.0).xyz;
            float weight = WeightSample(scaledUV.xy * _ScreenSize.xy / _RTHandleScale.xy, _ScreenSize.xy);

            return pipColor * weight;
        }
        else if (all(input.positionCS.xy < (topRight + borderSize)))
        {
            return float3(0.33f, 0.33f, 0.33f);
        }
        else
        {
            return color;
        }
    }

    float3 FragSceneEV100(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;

        float3 textColor = 0.0f;

        float2 sidebarSize = float2(0.9, 0.02) * _RTHandleScale.xy;

        float heightLabelBar = (DEBUG_FONT_TEXT_WIDTH * 1.25f) * _ScreenSize.w * _RTHandleScale.y;

        float2 sidebarBottomLeft = float2(0.05 * _RTHandleScale.x, heightLabelBar);
        float2 endPointSidebar = sidebarBottomLeft + sidebarSize;

        float3 outputColor = 0;
        float ev = GetEVAtLocation(uv);

        float evInRange = (ev - ParamExposureLimitMin) / (ParamExposureLimitMax - ParamExposureLimitMin);

        if (ev < ParamExposureLimitMax && ev > ParamExposureLimitMin)
        {
            outputColor = ToHeat(evInRange);
        }
        else if (ev > ParamExposureLimitMax)
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

        float extremeMargin = 5 * _ScreenSize.z * _RTHandleScale.x;
        DrawHeatSideBar(uv, sidebarBottomLeft, endPointSidebar, indicatorEVRange, 0.66f, sidebarSize, extremeMargin, outputColor);

        int2 unormCoord = input.positionCS.xy;

        // Label bar
        float2 borderSize = 2 * _ScreenSize.zw * _RTHandleScale.xy;
        if (uv.y < heightLabelBar  &&
            uv.x >= (sidebarBottomLeft.x - borderSize.x) && uv.x <= (borderSize.x + endPointSidebar.x))
        {
            outputColor = outputColor * 0.075f;
        }

        // Number of labels
        int labelCount = 8;
        float oneOverLabelCount = rcp(labelCount);
        float labelDeltaScreenSpace = _ScreenSize.x * oneOverLabelCount;

        int minLabelLocationX = (sidebarBottomLeft.x - borderSize.x) * (_ScreenSize.x / _RTHandleScale.x) + DEBUG_FONT_TEXT_WIDTH * 0.25;
        int maxLabelLocationX = (borderSize.x + endPointSidebar.x) * (_ScreenSize.x / _RTHandleScale.x) - (DEBUG_FONT_TEXT_WIDTH * 3);

        int labelLocationY = 0.0f;

        [unroll]
        for (int i = 0; i <= labelCount; ++i)
        {
            float t = oneOverLabelCount * i;
            float labelValue = lerp(ParamExposureLimitMin, ParamExposureLimitMax, t);
            uint2 labelLoc = uint2((uint)lerp(minLabelLocationX, maxLabelLocationX, t), labelLocationY);
            DrawFloatExplicitPrecision(labelValue, float3(1.0f, 1.0f, 1.0f), unormCoord, 1, labelLoc, outputColor.rgb);
        }

        int displayTextOffsetX = DEBUG_FONT_TEXT_WIDTH;
        uint2 textLocation = uint2(_MousePixelCoord.x + displayTextOffsetX, _MousePixelCoord.y);
        DrawFloatExplicitPrecision(indicatorEV, textColor, unormCoord, 1, textLocation, outputColor.rgb);
        textLocation =  uint2(_MousePixelCoord.xy);
        DrawCharacter('X', float3(0.0f, 0.0f, 0.0f), unormCoord, textLocation, outputColor.rgb);

        return outputColor;
    }



    float3 FragHistogram(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;

        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        float weight = WeightSample(input.positionCS.xy, _ScreenSize.xy);

        float3 outputColor = color;

        // Get some overall info from the histogram
        float maxValue = 0;
        float sum = 0;
        for (int i = 0; i < HISTOGRAM_BINS; ++i)
        {
            float histogramVal = UnpackWeight(_HistogramBuffer[i]);
            maxValue = max(histogramVal, maxValue);
            sum += histogramVal;
        }

        float minPercentileBin = 0;
        float maxPercentileBin = 0;
        float percentile = ComputePercentile(uv, sum, minPercentileBin, maxPercentileBin);

        if (percentile < _HistogramMinPercentile)
        {
            outputColor = (input.positionCS.x + input.positionCS.y) % 2 == 0 ? float3(0.0f, 0.0f, 1.0) : color*0.33;
        }
        if (percentile > _HistogramMaxPercentile)
        {
            outputColor = (input.positionCS.x + input.positionCS.y) % 2 == 0 ? float3(1.0, 0.0f, 0.0f) : color * 0.33;
        }

        float histFrameHeight = 0.2 * _RTHandleScale.y;
        float minPercentileLoc = max(minPercentileBin, 0);
        float maxPercentileLoc = min(maxPercentileBin, HISTOGRAM_BINS - 1);
#if PERCENTILE_AS_BARS
        minPercentileLoc /= (HISTOGRAM_BINS - 1);
        maxPercentileLoc /= (HISTOGRAM_BINS - 1);
#endif

        DrawHistogramFrame(uv, input.positionCS.xy, histFrameHeight, float3(0.125,0.125,0.125), 0.4f, maxValue, minPercentileLoc, maxPercentileLoc,  outputColor);


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
