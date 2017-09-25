Shader "Hidden/ScriptableRenderPipeline/LightweightPipeline/Blit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100

        Pass
        {
            Tags { "LightMode" = "LightweightForward"}

            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

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

            sampler2D _BlitTex;

            VertexOutput Vertex(VertexInput i)
            {
                VertexOutput o;
                o.pos = half4(i.vertex.xyz, 1.0);
                o.uv = i.uv;
                return o;
            }

            fixed4 Fragment(VertexOutput i) : SV_Target
            {
                fixed4 col = tex2D(_BlitTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
