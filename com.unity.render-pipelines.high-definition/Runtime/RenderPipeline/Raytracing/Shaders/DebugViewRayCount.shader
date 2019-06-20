Shader "Hidden/HDRP/DebugViewRayCount"
{
    Properties
    {
        _FontColor("_FontColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            StructuredBuffer<uint> _TotalRayCountBuffer;
            float4 _FontColor;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  texcoord    : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            // Helper to write mega rays per frame
            #define MAX_STRING_SIZE 16
            #define AO_STRING_SIZE 4
            static const uint AOName[MAX_STRING_SIZE] = {'A','O',':',' ','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0'};
            #define REFLECTION_STRING_SIZE 12
            static const uint ReflectionName[MAX_STRING_SIZE] = {'R','e','f','l','e','c','t','i','o','n',':',' ','\0','\0','\0','\0'};
            #define SHADOW_STRING_SIZE 13
            static const uint ShadowName[MAX_STRING_SIZE] = {'A','r','e','a',' ','S','h','a','d','o','w',':',' ','\0','\0','\0'};
            #define TOTAL_STRING_SIZE 7
            static const uint TotalName[MAX_STRING_SIZE] = {'T','o','t','a','l',':',' ','\0','\0','\0','\0','\0','\0','\0','\0','\0'};
            #define MRPF_STRING_SIZE 12
            static const uint MGPFName[MAX_STRING_SIZE] = {' ', 'm','r','a','y','s','/','f','r','a','m','e','\0','\0','\0','\0'};

            // Helper function to write strings
            void WriteMegaraysPerFrame(uint targetWord[MAX_STRING_SIZE], uint stringSize, float3 fontColor, uint2 unormCoord, inout uint2 displayUnormCoord, inout float3 result)
            {
                for(int i = 0; i < stringSize; ++i)
                {
                    DrawCharacter(targetWord[i], fontColor, unormCoord, displayUnormCoord, result);
                }
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Display message offset:
                int displayTextOffsetX = 1.5 * DEBUG_FONT_TEXT_WIDTH;
                int displayTextOffsetY = -DEBUG_FONT_TEXT_HEIGHT;

                // Get MRays/frame
                float aoMRays = (float)_TotalRayCountBuffer[0] / (1e6f);
                float reflectionMRays = (float)_TotalRayCountBuffer[1] / (1e6f);
                float areaShadowMRays = (float)_TotalRayCountBuffer[2] / (1e6f);
				float totalMRays = aoMRays + reflectionMRays + areaShadowMRays;

                uint2 displayUnormCoord = uint2(displayTextOffsetX, abs(displayTextOffsetY) * 4);
                uint2 unormCoord = input.positionCS.xy;
                float3 fontColor = _FontColor.rgb;
                float4 result = float4(0.0, 0.0, 0.0, 1.0);

                // Ambient Occlusion Data
                WriteMegaraysPerFrame(AOName, AO_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);
                DrawFloat(aoMRays, fontColor, unormCoord, displayUnormCoord, result.rgb);
                WriteMegaraysPerFrame(MGPFName, MRPF_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);

                // Reflection Data
                displayUnormCoord = uint2(displayTextOffsetX, abs(displayTextOffsetY) * 3);
                WriteMegaraysPerFrame(ReflectionName, REFLECTION_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);
                DrawFloat(reflectionMRays, fontColor, unormCoord, displayUnormCoord, result.rgb);
                WriteMegaraysPerFrame(MGPFName, MRPF_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);

                // Shadow Data
                displayUnormCoord = uint2(displayTextOffsetX, abs(displayTextOffsetY) * 2);
                WriteMegaraysPerFrame(ShadowName, SHADOW_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);
                DrawFloat(areaShadowMRays, fontColor, unormCoord, displayUnormCoord, result.rgb);
                WriteMegaraysPerFrame(MGPFName, MRPF_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);

                // Total Data
                displayUnormCoord = uint2(displayTextOffsetX, abs(displayTextOffsetY));
                WriteMegaraysPerFrame(TotalName, TOTAL_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);
                DrawFloat(totalMRays, fontColor, unormCoord, displayUnormCoord, result.rgb);
                WriteMegaraysPerFrame(MGPFName, MRPF_STRING_SIZE, fontColor, unormCoord, displayUnormCoord, result.rgb);

                // If the pixel is black, the value is not required 
                result.w = result.x > 0.0f ? 1.0f : 0.0f;
                return result;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
