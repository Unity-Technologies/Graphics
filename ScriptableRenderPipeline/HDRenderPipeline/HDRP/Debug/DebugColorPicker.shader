Shader "Hidden/HDRenderPipeline/DebugColorPicker"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal

            #pragma vertex Vert
            #pragma fragment Frag

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "../ShaderVariables.hlsl"
            #include "../Debug/DebugDisplay.cs.hlsl"
            #include "../Debug/DebugDisplay.hlsl"

            TEXTURE2D(_DebugColorPickerTexture);
            SAMPLER(sampler_DebugColorPickerTexture);

            float4 _ColorPickerParam; // 4 increasing threshold
            int _ColorPickerMode;
            float3 _ColorPickerFontColor;

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
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 DisplayPixelInformationAtMousePosition(Varyings input, float4 result, float4 mouseResult)
            {
                if (_MousePixelCoord.z >= 0.0 && _MousePixelCoord.z <= 1.0 && _MousePixelCoord.w >= 0 && _MousePixelCoord.w <= 1.0)
                {
                    // Display message offset:
                    int displayTextOffsetX = 1.5 * DEBUG_FONT_TEXT_WIDTH;
                    #if UNITY_UV_STARTS_AT_TOP
                    int displayTextOffsetY = -DEBUG_FONT_TEXT_HEIGHT;
                    #else
                    int displayTextOffsetY = DEBUG_FONT_TEXT_HEIGHT;
                    #endif

                    uint2 displayUnormCoord = uint2(_MousePixelCoord.x + displayTextOffsetX, _MousePixelCoord.y + displayTextOffsetY);
                    uint2 unormCoord = input.positionCS.xy;

                    if (_ColorPickerMode == COLORPICKERDEBUGMODE_BYTE || _ColorPickerMode == COLORPICKERDEBUGMODE_BYTE4)
                    {
                        uint4 mouseValue = int4(mouseResult * 255.5);

                        DrawInteger(mouseValue.x, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);

                        if (_ColorPickerMode == COLORPICKERDEBUGMODE_BYTE4)
                        {
                            displayUnormCoord.x = _MousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawInteger(mouseValue.y, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);
                            displayUnormCoord.x = _MousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawInteger(mouseValue.z, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);
                            displayUnormCoord.x = _MousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawInteger(mouseValue.w, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);
                        }
                    }
                    else // float
                    {
                        DrawFloat(mouseResult.x, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);
                        if (_ColorPickerMode == COLORPICKERDEBUGMODE_FLOAT4)
                        {
                            displayUnormCoord.x = _MousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawFloat(mouseResult.y, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);
                            displayUnormCoord.x = _MousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawFloat(mouseResult.z, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);
                            displayUnormCoord.x = _MousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawFloat(mouseResult.w, _ColorPickerFontColor, unormCoord, displayUnormCoord, result.rgb);
                        }
                    }
                }

                return result;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 result = SAMPLE_TEXTURE2D(_DebugColorPickerTexture, sampler_DebugColorPickerTexture, input.texcoord);
                //result.rgb = GetColorCodeFunction(result.x, _ColorPickerParam);
                float4 mouseResult = SAMPLE_TEXTURE2D(_DebugColorPickerTexture, sampler_DebugColorPickerTexture, _MousePixelCoord.zw);

                return DisplayPixelInformationAtMousePosition(input, result, mouseResult);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
