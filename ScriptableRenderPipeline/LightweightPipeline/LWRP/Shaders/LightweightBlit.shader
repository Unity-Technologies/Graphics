Shader "Hidden/LightweightPipeline/Blit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100

        Pass
        {
            Tags { "LightMode" = "LightweightForward"}

            ZTest Always ZWrite Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "LWRP/ShaderLibrary/Core.hlsl"

            struct VertexInput
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
            };

            struct VertexOutput
            {
                half4 pos       : SV_POSITION;
                half2 uv        : TEXCOORD0;
            };

            TEXTURE2D(_BlitTex);
            SAMPLER(sampler_BlitTex);

            VertexOutput Vertex(VertexInput i)
            {
                VertexOutput o;
                o.pos = TransformObjectToHClip(i.vertex.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 Fragment(VertexOutput i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BlitTex, sampler_BlitTex, i.uv);
                return col;
            }
            ENDHLSL
        }
    }
}
