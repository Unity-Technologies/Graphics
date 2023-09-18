Shader "Hidden/Universal/HDRDebugView"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma editor_sync_compilation
    #pragma multi_compile_fragment _ DEBUG_DISPLAY
    #pragma multi_compile_local_fragment _ HDR_ENCODING

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    
    TEXTURE2D_X(_DebugScreenTexture);
    TEXTURE2D_X(_OverlayUITexture);
    TEXTURE2D_X(_SourceTexture);
    TEXTURE2D(_xyBuffer);

    int _DebugHDRMode;

    float4 _HDRDebugParams; // xy: brightness min/max, z: paper white brightness, w: color primairies
    #define _MinNits    _HDRDebugParams.x
    #define _MaxNits    _HDRDebugParams.y
    #define _PaperWhite _HDRDebugParams.z

    float2 RGBtoxy(float3 rgb)
    {
        float3 XYZ = RotateOutputSpaceToXYZ(rgb);
        return XYZtoxy(XYZ);
    }

    float3 uvToGamut(float2 uv)
    {
        float3 xyzColor = xyYtoXYZ(float3(uv.x, uv.y, 1.0f));
        float3 linearRGB = RotateXYZToOutputSpace(xyzColor);

        float scale = 1.0f / length(linearRGB);

        float desat = dot(linearRGB, 0.333f);
        scale *= 1.0 + exp(-length(linearRGB - desat) * 2.0f) * 0.5f;

        linearRGB *= scale;

        return linearRGB;
    }

    bool IsInCIExyMapping(float2 xy)
    {
        return SAMPLE_TEXTURE2D_LOD(_xyBuffer, sampler_PointClamp, xy, 0.0).x != 0;
    }

    float3 ValuesAbovePaperWhite(half4 color, float2 uv)
    {
        float maxC = max(color.x, max(color.y, color.z));

        float t = (maxC - _PaperWhite) / (_MaxNits - _PaperWhite);

        if (maxC > _PaperWhite)
        {
            return lerp(float3(_PaperWhite, _PaperWhite, 0), float3(_PaperWhite, 0, 0), saturate(t));
        }
        else
        {
            return Luminance(color).xxx;
        }
    }

    void RenderDebugHDR(half4 color, float2 uv, inout half4 debugColor)
    {
        if (_DebugHDRMode == HDRDEBUGMODE_VALUES_ABOVE_PAPER_WHITE)
        {
            debugColor.xyz = ValuesAbovePaperWhite(color, uv);
            return;
        }

        int displayClip = (_DebugHDRMode == HDRDEBUGMODE_GAMUT_CLIP);
        int gamutPiPSize = _ScreenSize.x / 3.0f;

        float2 r_2020 = float2(0.708, 0.292);
        float2 g_2020 = float2(0.170, 0.797);
        float2 b_2020 = float2(0.131, 0.046);

        float2 r_709 = float2(0.64, 0.33);
        float2 g_709 = float2(0.3, 0.6);
        float2 b_709 = float2(0.15, 0.06);

        float2 r_p3 = float2(0.68, 0.32);
        float2 g_p3 = float2(0.265, 0.69);
        float2 b_p3 = float2(0.15, 0.06);

        float2 pos = uv * _ScreenSize.xy;
        float lineThickness = 0.002;

        float2 xy = RGBtoxy(color.rgb);

        float3 rec2020Color = float3(_PaperWhite, 0, 0);
        float3 rec2020ColorDesat = float3(3.0, 0.5, 0.5);
        float3 rec709Color = float3(0, _PaperWhite, 0);
        float3 rec709ColorDesat = float3(0.4, 0.6, 0.4);
        float3 p3Color = float3(0, 0, _PaperWhite);
        float3 p3ColorDesat = float3(0.4, 0.4, 0.6);

        //Display Gamut Clip Scene colour conversion
        if (displayClip)
        {
            float clipAlpha = 0.2f;
            if (IsPointInTriangle(xy, r_709, g_709, b_709))
            {
                color.rgb = (color.rgb * (1 - clipAlpha) + clipAlpha * rec709Color);
            }
            else if (IsPointInTriangle(xy, r_p3, g_p3, b_p3))
            {
                color.rgb = (color.rgb * (1 - clipAlpha) + clipAlpha * p3Color);
            }
            else if (IsPointInTriangle(xy, r_2020, g_2020, b_2020))
            {
                color.rgb = (color.rgb * (1 - clipAlpha) + clipAlpha * rec2020Color);
            }
        }


        float4 gamutColor = 0;
        if (all(pos < gamutPiPSize))
        {
            float2 uv = pos / gamutPiPSize;     // scale-down uv
            float4 lineColor = DrawSegment(uv, g_709, b_709, lineThickness, float3(0, 0, 0)) + DrawSegment(uv, b_709, r_709, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, r_709, g_709, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, g_2020, b_2020, lineThickness, float3(0, 0, 0)) + DrawSegment(uv, b_2020, r_2020, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, r_2020, g_2020, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, g_p3, b_p3, lineThickness, float3(0, 0, 0)) + DrawSegment(uv, b_p3, r_p3, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, r_p3, g_p3, lineThickness, float3(0, 0, 0));

            float3 linearRGB = 0;
            bool pointInRec709 = true;
            if (IsPointInTriangle(uv, r_2020, g_2020, b_2020))
            {
                float3 colorSpaceColor = rec709Color;
                linearRGB = uvToGamut(uv);

                if (displayClip)
                {
                    if (IsPointInTriangle(uv, r_709, g_709, b_709))
                    {
                        colorSpaceColor = rec709Color;
                        linearRGB.rgb = rec709ColorDesat;
                    }
                    else if (IsPointInTriangle(uv, r_p3, g_p3, b_p3))
                    {
                        colorSpaceColor = p3Color;
                        linearRGB.rgb = p3ColorDesat;
                    }
                    else
                    {
                        colorSpaceColor = rec2020Color;
                        linearRGB.rgb = rec2020ColorDesat;
                    }
                }

                gamutColor.a = max(lineColor.a, 0.15);
                gamutColor.rgb = linearRGB * _PaperWhite;

                if (IsInCIExyMapping(uv))
                {
                    gamutColor.a = 1;
                    if (displayClip)
                        gamutColor.rgb = colorSpaceColor;
                }
            }

            gamutColor.rgb = gamutColor.rgb * (1.0f - lineColor.a) + lineColor.rgb;
        }

        debugColor.rgb = gamutColor.rgb * gamutColor.a + color.rgb * (1 - gamutColor.a);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            ZClip Off
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag

                RW_TEXTURE2D(float, _xyBufferRW);

                #define _SizePerDim _HDRDebugParams.xy

                half4 Frag(Varyings input) : SV_Target
                {
                    float4 col = SAMPLE_TEXTURE2D_X(_DebugScreenTexture, sampler_PointClamp, input.texcoord);
                    float2 xy = (RGBtoxy(col.rgb));

                    _xyBufferRW[(xy * _SizePerDim)] = 1;
                    return col;
                }
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            ZClip Off
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
                
                half4 Frag(Varyings input) : SV_Target
                {
                    float4 outCol = 0;
                    float2 uv = input.texcoord;
                    half4 col = SAMPLE_TEXTURE2D_X(_SourceTexture, sampler_PointClamp, uv);
                    half4 outColor = 0;
                    RenderDebugHDR(col, uv, outColor);

#if defined(HDR_ENCODING)
                    float4 uiSample = SAMPLE_TEXTURE2D_X(_OverlayUITexture, sampler_PointClamp, input.texcoord);
                    outColor.rgb = SceneUIComposition(uiSample, outColor.rgb, _PaperWhite, _MaxNits);
                    outColor.rgb = OETF(outColor.rgb, _MaxNits);
#endif

#if defined(DEBUG_DISPLAY)
                    half4 debugColor = 0;

                    if (CanDebugOverrideOutputColor(outColor, uv, debugColor))
                    {
                        return debugColor;
                    }
#endif
                    return outColor;
                }
            ENDHLSL
        }
    }
}
