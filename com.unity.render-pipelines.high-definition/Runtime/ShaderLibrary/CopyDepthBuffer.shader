Shader "Hidden/HDRP/CopyDepthBuffer"
{

    Properties{
        _FlipY("FlipY", Int) = 0
    }
    HLSLINCLUDE



    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Copy Depth"

            Cull   Off
            ZTest  Always
            ZWrite On
            Blend  Off
            ColorMask 0

            HLSLPROGRAM
            #pragma target 4.5
            #pragma editor_sync_compilation
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch
            #pragma fragment Frag
            #pragma vertex Vert
            //#pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D_X_FLOAT(_InputDepthTexture);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            uniform float4 _BlitScaleBias;
            uniform int _FlipY;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                if (_FlipY)
                {
                    output.texcoord.y = 1.0 - output.texcoord.y;
                }
                output.texcoord *= _BlitScaleBias.xy;
                return output;
            }

            float Frag(Varyings input) : SV_Depth
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                uint2 coord = uint2(input.texcoord.xy * _ScreenSize.xy);
                return LOAD_TEXTURE2D_X(_InputDepthTexture, coord).x;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
