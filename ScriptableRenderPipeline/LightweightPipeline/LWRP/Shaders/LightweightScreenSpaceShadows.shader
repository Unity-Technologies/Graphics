Shader "Hidden/LightweightPipeline/ScreenSpaceShadows"
{
    SubShader
    {
        Tags {}

        Pass
        {
            ZTest Always ZWrite Off

            HLSLPROGRAM
            #pragma vertex   Vertex
            #pragma fragment Fragment

            #include "LWRP/ShaderLibrary/Core.hlsl"

            //Scene Depth
            TEXTURE2D(_Depth);
            SAMPLER(sampler_Depth);

            //Shadow Cascades
            TEXTURE2D(_ShadowCascades);
            SAMPLER(sampler_ShadowCascades);

            //Far plane corners in view space
            float4 _FrustumCorners[4];

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                uint   id     : SV_VertexID;
            };

            struct VertexOutput
            {
                half4  pos    : SV_POSITION;
                half2  uv     : TEXCOORD0;
                float3 ray    : TEXCOORD1;
            };

            VertexOutput Vertex(VertexInput i)
            {
                VertexOutput o;
                o.pos = TransformObjectToHClip(i.vertex.xyz);
                o.uv  = i.uv;
                o.ray = _FrustumCorners[i.id]; 
                return o;
            }

            float3 ComputeViewSpacePosition(VertexOutput i)
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_Depth, sampler_Depth, i.uv);
                depth = Linear01Depth(depth, _ZBufferParams);

                return i.ray * depth;
            }

            half4 Fragment(VertexOutput i) : SV_Target
            {
                return half4(ComputeViewSpacePosition(i), 1);
            }

            ENDHLSL
        }
    }
}