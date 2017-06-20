Shader "Hidden/HDRenderPipeline/DebugFullScreen"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Off
            Blend One Zero
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: unitl we go futher in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #include "../../ShaderLibrary/Common.hlsl"
            #include "../Debug/DebugDisplay.cs.hlsl"

            TEXTURE2D(_DebugFullScreenTexture);
            SAMPLER2D(sampler_DebugFullScreenTexture);
            float _FullScreenDebugMode;

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
                output.texcoord   = GetFullScreenTriangleTexcoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // SSAO
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_SSAO)
                {
                    return 1.0f - SAMPLE_TEXTURE2D(_DebugFullScreenTexture, sampler_DebugFullScreenTexture, input.texcoord).xxxx;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_SSAOBEFORE_FILTERING)
                {
                    return 1.0f - SAMPLE_TEXTURE2D(_DebugFullScreenTexture, sampler_DebugFullScreenTexture, input.texcoord).xxxx;
                }

                return float4(0.0, 0.0, 0.0, 0.0);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
