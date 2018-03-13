Shader "Hidden/HDRenderPipeline/DebugColorPicker"
{
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
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal

            #pragma vertex Vert
            #pragma fragment Frag

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "CoreRP/ShaderLibrary/Color.hlsl"
            #include "../ShaderVariables.hlsl"
            #include "../Debug/DebugDisplay.cs.hlsl"
            #include "../Debug/DebugDisplay.hlsl"

            TEXTURE2D(_DebugColorPickerTexture);
            SAMPLER(sampler_DebugColorPickerTexture);

            float4 _ColorPickerParam; // 4 increasing threshold
            int _ColorPickerMode;
            float3 _ColorPickerFontColor;
            float _ApplyLinearToSRGB;
            float _RequireToFlipInputTexture;

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

            float4 DisplayPixelInformationAtMousePosition(Varyings input, float4 result, float4 mouseResult, float4 mousePixelCoord)
            {
                bool flipY = _RequireToFlipInputTexture > 0.0;

                if (mousePixelCoord.z >= 0.0 && mousePixelCoord.z <= 1.0 && mousePixelCoord.w >= 0 && mousePixelCoord.w <= 1.0)
                {
                    // As when we read with the color picker we don't go through the final blit (that current hardcode a conversion to sRGB)
                    // and as our material debug take it into account, we need to a transform here.
                    if (_ApplyLinearToSRGB > 0.0)
                    {
                        mouseResult.rgb = LinearToSRGB(mouseResult.rgb);
                    }

                    // Display message offset:
                    int displayTextOffsetX = 1.5 * DEBUG_FONT_TEXT_WIDTH;
                    int displayTextOffsetY;
                    if (flipY)
                    {
                        displayTextOffsetY = DEBUG_FONT_TEXT_HEIGHT;
                    }
                    else
                    {
                        displayTextOffsetY = -DEBUG_FONT_TEXT_HEIGHT;
                    }

                    uint2 displayUnormCoord = uint2(mousePixelCoord.x + displayTextOffsetX, mousePixelCoord.y + displayTextOffsetY);
                    uint2 unormCoord = input.positionCS.xy;

                    if (_ColorPickerMode == COLORPICKERDEBUGMODE_BYTE || _ColorPickerMode == COLORPICKERDEBUGMODE_BYTE4)
                    {
                        uint4 mouseValue = int4(mouseResult * 255.5);

                        DrawCharacter('R', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawInteger(mouseValue.x, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);

                        if (_ColorPickerMode == COLORPICKERDEBUGMODE_BYTE4)
                        {
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('G', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawInteger(mouseValue.y, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('B', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawInteger(mouseValue.z, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('A', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawInteger(mouseValue.w, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        }
                    }
                    else // float
                    {
                        DrawCharacter('X', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawFloat(mouseResult.x, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        if (_ColorPickerMode == COLORPICKERDEBUGMODE_FLOAT4)
                        {
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('Y', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawFloat(mouseResult.y, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('Z', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawFloat(mouseResult.z, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('W', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawFloat(mouseResult.w, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        }
                    }
                }

                return result;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                if (_RequireToFlipInputTexture > 0.0)
                {
                    input.texcoord.y = 1.0 * _ScreenToTargetScale.y - input.texcoord.y;
                }

                float4 result = SAMPLE_TEXTURE2D(_DebugColorPickerTexture, sampler_DebugColorPickerTexture, input.texcoord);
                //result.rgb = GetColorCodeFunction(result.x, _ColorPickerParam);

                float4 mousePixelCoord = _MousePixelCoord;
                if (_RequireToFlipInputTexture > 0.0)
                {
                    mousePixelCoord.y = _ScreenParams.y - mousePixelCoord.y;
                    // Note: We must not flip the mousePixelCoord.w coordinate
                }

                float4 mouseResult = SAMPLE_TEXTURE2D(_DebugColorPickerTexture, sampler_DebugColorPickerTexture, mousePixelCoord.zw);

                float4 finalResult = DisplayPixelInformationAtMousePosition(input, result, mouseResult, mousePixelCoord);
                return finalResult;
            }

            ENDHLSL
        }

    }
    Fallback Off
}
