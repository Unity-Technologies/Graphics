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
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            #pragma fragment Frag
            #pragma vertex Vert
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D_FLOAT(_InputDepthTexture);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_Position;
                float2 texcoord   : TEXCOORD0;
            };

            int _FlipY;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                if (_FlipY)
                {
                    output.texcoord.y = 1.0 - output.texcoord.y;
                }
                return output;
            }

            float Frag(Varyings input) : SV_Depth
            {
                uint2 coord = uint2(input.texcoord.xy * _ScreenSize.xy);
                return LOAD_TEXTURE2D(_InputDepthTexture, coord).x;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
