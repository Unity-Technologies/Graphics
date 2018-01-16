Shader "Hidden/LightweightPipeline/CopyDepth"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightiweightPipeline"}

        Pass
        {
            ZTest Always ZWrite Off ColorMask 0

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex vert
            #pragma fragment frag

            #include "LWRP/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            struct VertexInput
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
            };

            VertexOutput vert(VertexInput i)
            {
                VertexOutput o;
                o.uv = i.uv;
                o.position = TransformObjectToHClip(i.vertex.xyz);
                return o;
            }

            float frag(VertexOutput i) : SV_Depth
            {
                return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);
            }
            ENDHLSL
        }
    }
}
