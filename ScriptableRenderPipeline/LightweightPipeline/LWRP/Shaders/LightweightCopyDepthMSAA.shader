Shader "Hidden/LightweightPipeline/CopyDepthMSAA"
{
    Properties
    {
        [HideInInspector] _SampleCount("MSAA sample count", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightiweightPipeline"}

        Pass
        {
            ZTest Always ZWrite On ColorMask 0

            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag

            #pragma require msaatex

            #include "LWRP/ShaderLibrary/Core.hlsl"

            Texture2DMS<float> _CameraDepthTexture;
            float _SampleCount;
            float4 _CameraDepthTexture_TexelSize;

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
                int2 coord = int2(i.uv * _CameraDepthTexture_TexelSize.zw);
                int samples = (int)_SampleCount;
                #if UNITY_REVERSED_Z
                    float outDepth = 1.0;
                    #define DEPTH_OP min
                #else
                    float outDepth = 0.0;
                    #define DEPTH_OP max
                #endif

                for (int i = 0; i < samples; ++i)
                    outDepth = DEPTH_OP(LOAD_TEXTURE2D_MSAA(_CameraDepthTexture, coord, i), outDepth);

                return outDepth;
            }
            ENDHLSL
        }
    }
}
