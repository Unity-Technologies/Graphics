Shader "Hidden/HDRP/DebugExposure"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Components/Tonemapping.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/HistogramExposureCommon.hlsl"
    #define DEBUG_DISPLAY
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

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

    #define _DrawTonemapCurve               _ExposureDebugParams.x
    #define _TonemapType                    _ExposureDebugParams.y
    #define _CenterAroundTargetExposure     _ExposureDebugParams.z
    #define _FinalImageHistogramRGB         _ExposureDebugParams.w


    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
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
        return ComputeEV100FromAvgLuminance(max(SampleLuminance(uv), 1e-4), MeterCalibrationConstant);
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
            int distanceInPixels = abs(evValueRange - inRange) * sidebarSize.x * _ScreenSize.x / _RTHandleScale.x;
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

    bool DrawEmptyFrame(float2 uv, float3 frameColor, float frameAlpha, float frameHeight, float heightLabelBar, inout float3 outColor)
    {
        float2 borderSize = 2 * _ScreenSize.zw * _RTHandleScale.xy;

        if (uv.y > frameHeight) return false;

        // ---- Draw General frame ----
        if (uv.x < borderSize.x || uv.x >(1.0f - borderSize.x))
        {
            outColor = 0.0;
            return false;
        }
        else if (uv.y > frameHeight - borderSize.y)
        {
            outColor = 0.0;
            return false;
        }
        else
        {
            outColor = lerp(outColor, frameColor, frameAlpha);
        }

        // ----  Draw label bar -----
        if (uv.y < heightLabelBar)
        {
            outColor = outColor * 0.075f;
        }

        return true;
    }

    float2 GetMinMaxLabelRange(float currEV)
    {
        if (_CenterAroundTargetExposure > 0)
        {
            int maxAtBothSide = min(0.5f * (ParamExposureLimitMax - ParamExposureLimitMin), 10);
            return float2(currEV - maxAtBothSide, currEV + maxAtBothSide);
        }
        else
        {
            return float2(ParamExposureLimitMin, ParamExposureLimitMax);
        }

    }

    float EvToUVLocation(float ev, float currEV)
    {
        float2 valuesRange = GetMinMaxLabelRange(currEV);
        return (ev - valuesRange.x) / (valuesRange.y - valuesRange.x);
    }

    float GetHistogramInfo(float coordOnX, float maxHistValue, float heightLabelBar, float frameHeight, float currExposure, out uint binIndex, out bool isEdgeOfBin)
    {
        float barSize = _ScreenSize.x / HISTOGRAM_BINS;
        float locWithinBin = 0.0f;

        if (_CenterAroundTargetExposure > 0)
        {
            // This is going to be the center of the histogram view in this mode.
            uint centerBin = EVToBinLocation(currExposure);
            uint midXPoint = _ScreenSize.x / 2;
            uint halfBarSize = barSize / 2;
            uint lowerMidPoint = midXPoint - halfBarSize;
            uint higherMidPoint = midXPoint + halfBarSize;
            if (coordOnX < float(lowerMidPoint))
            {
                uint distanceFromCenter = lowerMidPoint - coordOnX;
                float deltaBinFloat = distanceFromCenter / barSize;
                uint deltaInBins = ceil(deltaBinFloat);
                locWithinBin = frac(deltaBinFloat) * barSize;
                binIndex = centerBin - deltaInBins;
            }
            else if (coordOnX > float(higherMidPoint))
            {
                uint distanceFromCenter = coordOnX - higherMidPoint;
                float deltaBinFloat = distanceFromCenter / barSize;
                uint deltaInBins = ceil(deltaBinFloat);
                locWithinBin = frac(deltaBinFloat) * barSize;

                binIndex = centerBin + deltaInBins;
            }
            else
            {
                binIndex = centerBin;

                locWithinBin = (higherMidPoint - coordOnX);
            }
        }
        else
        {
            float bin = coordOnX / barSize;
            locWithinBin = barSize * frac(bin);
            binIndex = uint(bin);
        }

        isEdgeOfBin = locWithinBin < 1 || locWithinBin >(barSize - 1);

        float val = UnpackWeight(_HistogramBuffer[binIndex]);
        val /= maxHistValue;

        val *= 0.95*(frameHeight - heightLabelBar);
        val += heightLabelBar;
        return val;
    }

    float GetLabel(float labelCount, float labelIdx, float currExposure, out uint2 labelLoc)
    {
        int minLabelLocationX = DEBUG_FONT_TEXT_WIDTH * 0.25;
        int maxLabelLocationX = _ScreenSize.x - (DEBUG_FONT_TEXT_WIDTH * 3);

        int labelLocationY = 0.0f;

        float2 labelValuesRange = GetMinMaxLabelRange(currExposure);
        float t = rcp(labelCount) * (labelIdx - 0.25);
        labelLoc = uint2((uint)lerp(minLabelLocationX, maxLabelLocationX, t), labelLocationY);
        return lerp(labelValuesRange.x, labelValuesRange.y, t);

    }

    float GetTonemappedValueAtLocation(float uvX, float currExposure)
    {
        float exposureAtLoc = 0;

        float2 labelValuesRange = GetMinMaxLabelRange(currExposure);
        exposureAtLoc = lerp(labelValuesRange.x, labelValuesRange.y, uvX);

        const float K = 12.5; // Reflected-light meter calibration constant
        float luminanceFromExposure = _ExposureTexture[int2(0, 0)].x * (exp2(exposureAtLoc) * (K / 100.0f));

        return saturate(Luminance(Tonemap(luminanceFromExposure)));

    }

    void DrawHistogramFrame(float2 uv, uint2 unormCoord, float frameHeight, float3 backgroundColor, float alpha, float maxHist, float minPercentLoc, float maxPercentLoc, inout float3 outColor)
    {
        float heightLabelBar = (DEBUG_FONT_TEXT_WIDTH * 1.25) * _ScreenSize.w * _RTHandleScale.y;

        if (DrawEmptyFrame(uv, backgroundColor, alpha, frameHeight, heightLabelBar, outColor))
        {
            float currExposure = _ExposureTexture[int2(0, 0)].y;
            float targetExposure = _ExposureDebugTexture[int2(0, 0)].x;

            // ---- Draw Buckets ----

            bool isEdgeOfBin = false;
            float val = GetHistogramValue(unormCoord.x, isEdgeOfBin);
            val /= maxHist;

            val *= 0.95*(frameHeight - heightLabelBar);
            val += heightLabelBar;

            uint bin = 0;
            val = GetHistogramInfo(unormCoord.x, maxHist, heightLabelBar, frameHeight, currExposure, bin, isEdgeOfBin);

            if (uv.y < val && uv.y > heightLabelBar)
            {
                isEdgeOfBin = isEdgeOfBin || (uv.y > val - _ScreenSize.w);
                if (bin < uint(minPercentLoc) && minPercentLoc > 0)
                {
                    outColor.rgb = float3(0, 0, 1);
                }
                else if (bin >= uint(maxPercentLoc) && maxPercentLoc > 0)
                {
                    outColor.rgb = float3(1, 0, 0);
                }
                else
                    outColor.rgb = float3(1.0f, 1.0f, 1.0f);
                if (isEdgeOfBin) outColor.rgb = 0;
            }

            // ---- Draw labels ----

            // Number of labels
            int labelCount = 12;

            [unroll]
            for (int i = 0; i <= labelCount; ++i)
            {
                uint2 labelLoc;
                float labelValue = GetLabel(labelCount, i, currExposure, labelLoc);
                DrawFloatExplicitPrecision(labelValue, float3(1.0f, 1.0f, 1.0f), unormCoord, 1, labelLoc, outColor.rgb);
            }

            // ---- Draw indicators ----

            float evInRange = EvToUVLocation(currExposure, currExposure);
            float targetEVInRange = EvToUVLocation(targetExposure, currExposure);

            float halfIndicatorSize = 0.007f;
            float halfWidthInScreen = halfIndicatorSize * _ScreenSize.x;

            float labelFrameHeightScreen = heightLabelBar * (_ScreenSize.y / _RTHandleScale.y);

            if (uv.y < heightLabelBar)
            {
                DrawTriangleIndicator(float2(unormCoord.xy), labelFrameHeightScreen, targetEVInRange, halfIndicatorSize, float3(0.9f, 0.75f, 0.1f), outColor);
                DrawTriangleIndicator(float2(unormCoord.xy), labelFrameHeightScreen, evInRange, halfIndicatorSize, float3(0.15f, 0.15f, 0.1f), outColor);
            }
            // TODO: Add bar?
            //else
            //{
            //    if (_CenterAroundTargetExposure > 0)
            //    {
            //        DrawHistogramIndicatorBar(float(unormCoord.x), evInRange, 0.003f, float3(0.0f, 0.0f, 0.0f), outColor);
            //    }
            //}

            // ---- Draw Tonemap curve ----
            if (_DrawTonemapCurve)
            {
                val = GetTonemappedValueAtLocation(unormCoord.x / _ScreenSize.x, currExposure);
                val *= 0.95 * (frameHeight - heightLabelBar);
                val += heightLabelBar;

                float curveWidth = 4 * _ScreenSize.w;

                if (uv.y < val && uv.y > (val - curveWidth))
                {
                    outColor = outColor * 0.1 + 0.9 * 0;
                }
            }
        }
    }

    float3 FragMetering(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;

        float pipFraction = 0.33f;
        uint borderSize = 3;
        float2 topRight = pipFraction * _ScreenSize.xy;

        if (all(input.positionCS.xy < topRight))
        {
            float2 scaledUV = uv / pipFraction;
            float3 pipColor = _ExposureDebugParams.x > 0 ? float3(1.0f, 1.0f, 1.0f) : SAMPLE_TEXTURE2D_X_LOD(_SourceTexture, s_linear_clamp_sampler, scaledUV, 0.0).xyz;
            float  luminance = SampleLuminance(scaledUV);
            float weight = WeightSample(scaledUV.xy * _ScreenSize.xy / _RTHandleScale.xy, _ScreenSize.xy, luminance);

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

        int2 unormCoord = input.positionCS.xy;
        uint2 textLocation = uint2(_MousePixelCoord.xy);


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
        float2 indicatorUV = _MousePixelCoord.xy * _ScreenSize.zw * _RTHandleScale.xy;
        float indicatorEV = GetEVAtLocation(indicatorUV);
        float indicatorEVRange = (indicatorEV - ParamExposureLimitMin) / (ParamExposureLimitMax - ParamExposureLimitMin);

        float extremeMargin = 5 * _ScreenSize.z * _RTHandleScale.x;
        DrawHeatSideBar(uv, sidebarBottomLeft, endPointSidebar, indicatorEVRange, 0.66f, sidebarSize, extremeMargin, outputColor);


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
        textLocation = uint2(_MousePixelCoord.x + displayTextOffsetX + 1, _MousePixelCoord.y - 1);
        DrawFloatExplicitPrecision(indicatorEV, 1.0f - textColor, unormCoord, 1, textLocation, outputColor.rgb);
        textLocation = uint2(_MousePixelCoord.x + displayTextOffsetX, _MousePixelCoord.y);
        DrawFloatExplicitPrecision(indicatorEV, textColor, unormCoord, 1, textLocation, outputColor.rgb);

        textLocation = uint2(_MousePixelCoord.x + 1, _MousePixelCoord.y - 1);
        DrawCharacter('X', 1.0f - textColor, unormCoord, textLocation, outputColor.rgb);
        textLocation = _MousePixelCoord.xy;
        DrawCharacter('X', textColor, unormCoord, textLocation, outputColor.rgb);

        return outputColor;
    }



    float3 FragHistogram(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;

        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;

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

        float currExposure = _ExposureTexture[int2(0, 0)].y;
        float targetExposure = _ExposureDebugTexture[int2(0, 0)].x;

        uint2 unormCoord = input.positionCS.xy;
        float3 textColor = float3(0.5f, 0.5f, 0.5f);
        uint2 textLocation = uint2(DEBUG_FONT_TEXT_WIDTH * 0.5, DEBUG_FONT_TEXT_WIDTH * 0.5 + histFrameHeight * (_ScreenSize.y / _RTHandleScale.y));
        DrawCharacter('C', textColor, unormCoord, textLocation, outputColor.rgb, 1, 10);
        DrawCharacter('u', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('r', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('r', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('e', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('n', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('t', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter(' ', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('E', textColor, unormCoord, textLocation, outputColor.rgb, 1, 10);
        DrawCharacter('x', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('p', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('o', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('s', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('u', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('r', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('e', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter(':', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        textLocation.x += DEBUG_FONT_TEXT_WIDTH * 0.5f;
        DrawFloatExplicitPrecision(currExposure, textColor, unormCoord, 3, textLocation, outputColor.rgb);
        textLocation = uint2(DEBUG_FONT_TEXT_WIDTH * 0.5, textLocation.y + DEBUG_FONT_TEXT_WIDTH);
        DrawCharacter('T', textColor, unormCoord, textLocation, outputColor.rgb, 1, 10);
        DrawCharacter('a', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('r', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('g', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('e', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('t', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter(' ', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('E', textColor, unormCoord, textLocation, outputColor.rgb, 1, 10);
        DrawCharacter('x', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('p', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('o', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('s', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('u', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('r', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('e', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter(':', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        textLocation.x += DEBUG_FONT_TEXT_WIDTH * 0.5f;
        DrawFloatExplicitPrecision(targetExposure, textColor, unormCoord, 3, textLocation, outputColor.rgb);
        textLocation = int2(DEBUG_FONT_TEXT_WIDTH * 0.5f, textLocation.y + DEBUG_FONT_TEXT_WIDTH);
        DrawCharacter('E', textColor, unormCoord, textLocation, outputColor.rgb, 1, 10);
        DrawCharacter('x', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('p', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('o', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('s', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('u', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('r', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('e', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter(' ', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('C', textColor, unormCoord, textLocation, outputColor.rgb, 1, 10);
        DrawCharacter('o', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('m', textColor, unormCoord, textLocation, outputColor.rgb, 1, 8);
        DrawCharacter('p', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('e', textColor, unormCoord, textLocation, outputColor.rgb, 1, 8);
        DrawCharacter('n', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('s', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('a', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('t', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('i', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('o', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter('n', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        DrawCharacter(':', textColor, unormCoord, textLocation, outputColor.rgb, 1, 7);
        textLocation.x += DEBUG_FONT_TEXT_WIDTH * 0.5f;
        DrawFloatExplicitPrecision(ParamExposureCompensation, textColor, unormCoord, 3, textLocation, outputColor.rgb);

        return outputColor;
    }

    StructuredBuffer<uint4> _FullImageHistogram;

    float3 FragImageHistogram(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        float3 outputColor = color;
        float heightLabelBar = (DEBUG_FONT_TEXT_WIDTH * 1.25) * _ScreenSize.w * _RTHandleScale.y;
        uint maxValue = 0;
        uint maxLuma = 0;
        for (int i = 0; i < 256; ++i)
        {
            uint histogramVal = Max3(_FullImageHistogram[i].x, _FullImageHistogram[i].y, _FullImageHistogram[i].z);
            maxValue = max(histogramVal, maxValue);
            maxLuma = max(_FullImageHistogram[i].w, maxLuma);
        }
        float histFrameHeight = 0.2 * _RTHandleScale.y;
        float safeBand = 1.0f / 255.0f;
        float binLocMin = safeBand;
        float binLocMax = 1.0f - safeBand;
        if (DrawEmptyFrame(uv, float3(0.125, 0.125, 0.125), 0.4, histFrameHeight, heightLabelBar, outputColor))
        {
            // Draw labels
            const int labelCount = 12;
            int minLabelLocationX = DEBUG_FONT_TEXT_WIDTH * 0.25;
            int maxLabelLocationX = _ScreenSize.x - (DEBUG_FONT_TEXT_WIDTH * 3);
            int labelLocationY = 0.0f;
            uint2 unormCoord = input.positionCS.xy;

            for (int i = 0; i <= labelCount; ++i)
            {
                float t = rcp(labelCount) * i;
                uint2 labelLoc = uint2((uint)lerp(minLabelLocationX, maxLabelLocationX, t), labelLocationY);
                float labelValue = lerp(0.0, 255.0, t);
                labelLoc.x += 2;
                DrawInteger(labelValue, float3(1.0f, 1.0f, 1.0f), unormCoord, labelLoc, outputColor.rgb);
            }
            float remappedX = (((float)unormCoord.x / _ScreenSize.x) - binLocMin) / (binLocMax - binLocMin);
            // Draw bins
            uint bin = saturate(remappedX) * 255;
            float4 val = _FullImageHistogram[bin];
            val /= float4(maxValue, maxValue, maxValue, maxLuma);
            val *= 0.95*(histFrameHeight - heightLabelBar);
            val += heightLabelBar;
            if (_FinalImageHistogramRGB > 0)
            {
                float3 alphas = 0;
                if (uv.y < val.x && uv.y > heightLabelBar)
                    alphas.x = 0.3333f;
                if (uv.y < val.y && uv.y > heightLabelBar)
                    alphas.y = 0.3333f;
                if (uv.y < val.z && uv.y > heightLabelBar)
                    alphas.z = 0.3333f;
                outputColor = outputColor * (1.0f - (alphas.x + alphas.y + alphas.z)) + alphas;
            }
            else
            {
                if (uv.y < val.w && uv.y > heightLabelBar)
                    outputColor = 0.3333f;
            }
        }
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

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off
            HLSLPROGRAM
                #pragma fragment FragImageHistogram
            ENDHLSL
        }

    }
    Fallback Off
}
