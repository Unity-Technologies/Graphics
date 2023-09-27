Shader "Hidden/HDRP/DebugHDR"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Components/Tonemapping.cs.hlsl"
    #define DEBUG_DISPLAY
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

    #pragma vertex Vert
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    TEXTURE2D_X(_DebugFullScreenTexture);

    float4 _HDRDebugParams;

    float4 _HDROutputParams;
    float4 _HDROutputParams2;
    #define _MinNits    _HDROutputParams.x
    #define _MaxNits    _HDROutputParams.y
    #define _PaperWhite _HDROutputParams.z
    #define _RangeReductionMode    (int)_HDROutputParams2.x

    #define _TonemapType _HDRDebugParams.w

    TEXTURE2D_X(_xyBuffer);

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

    float3 ToHeat(float value)
    {
        float3 r = value * 2.1f - float3(1.8f, 1.14f, 0.3f);
        return 1.0f - r * r;
    }

    float3 FragMetering(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;
        return color;
    }

    float DistToLine(float2 pt1, float2 pt2, float2 testPt)
    {
        float2 lineDir = pt2 - pt1;
        float2 perpDir = float2(lineDir.y, -lineDir.x);
        float2 dirToPt1 = pt1 - testPt;
        return (dot(normalize(perpDir), dirToPt1));
    }

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

    bool IsInImage(float2 xy)
    {
        return SAMPLE_TEXTURE2D_X_LOD(_xyBuffer, s_point_clamp_sampler, xy, 0.0).x != 0;
    }


    float3 CommonFrag(Varyings input, bool displayClip)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;

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

        float2 pos = input.positionCS.xy;
        float lineThickness = 0.002;

        float2 xy = RGBtoxy(color.rgb);

        float3 rec2020Color = float3(_PaperWhite, 0, 0);
        float3 rec2020ColorDesat = float3(3.0, 0.5, 0.5);
        float3 rec709Color = float3(0, _PaperWhite, 0);
        float3 rec709ColorDesat = float3(0.4, 0.6, 0.4);
        float3 p3Color = float3(0, 0, _PaperWhite);
        float3 p3ColorDesat = float3(0.4, 0.4, 0.6);

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
            float2 uv = pos / gamutPiPSize;
            float4 lineColor = DrawSegment(uv, g_709, b_709, lineThickness, float3(0, 0, 0)) + DrawSegment(uv, b_709, r_709, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, r_709, g_709, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, g_2020, b_2020, lineThickness, float3(0, 0, 0)) + DrawSegment(uv, b_2020, r_2020, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, r_2020, g_2020, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, g_p3, b_p3, lineThickness, float3(0, 0, 0)) + DrawSegment(uv, b_p3, r_p3, lineThickness, float3(0, 0, 0)) +
                DrawSegment(uv, r_p3, g_p3, lineThickness, float3(0, 0, 0));

            float3 linearRGB = 0;
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

                if (IsInImage(uv))
                {
                    gamutColor.a = 1;
                    if (displayClip)
                        gamutColor.rgb = colorSpaceColor;
                }
            }

            gamutColor.rgb = gamutColor.rgb * (1.0f - lineColor.a) + lineColor.rgb;
        }

        color.rgb = gamutColor.rgb * gamutColor.a + color.rgb * (1 - gamutColor.a);

        return color;
    }

    float3 FragColorGamut(Varyings input) : SV_Target
    {
        return CommonFrag(input, false);
    }

    float3 FragColorGamutClip(Varyings input) : SV_Target
    {
        return CommonFrag(input, true);
    }


    float3 FragNits(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.texcoord.xy;
        float3 color = SAMPLE_TEXTURE2D_X_LOD(_DebugFullScreenTexture, s_linear_clamp_sampler, uv, 0.0).xyz;

        float maxC = max(color.x, max(color.y, color.z));

        if (maxC > _PaperWhite)
        {
            if ( maxC > _MaxNits)
            {
                return float3(0, 0, _PaperWhite);
            }
            else
            {
                float t = (maxC - _PaperWhite) / (_MaxNits - _PaperWhite);
                return lerp(float3(_PaperWhite, _PaperWhite, 0), float3(_PaperWhite, 0, 0), saturate(t));
            }
        }
        else
        {
            return Luminance(color).xxx;
        }
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
            #pragma fragment FragColorGamut
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragColorGamutClip
            ENDHLSL
        }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FragNits
            ENDHLSL
        }
    }
    Fallback Off
}
