// Final compositing pass, just does gamma conversion for now.

Shader "SRP/FinalPass-Mobile"
{
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile __ UNITY_COLORSPACE_GAMMA

            #include "UnityCG.cginc"
       
            struct v2f {
                float4 vertex : SV_Position;
            };

            UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(0);

            v2f vert(uint id : SV_VertexID)
            {
                v2f o;
                o.vertex.x = (id & 1) != 0 ? 3 : -1;
                o.vertex.y = (id & 2) != 0 ? -5 : 1;
                o.vertex.z = 0;
                o.vertex.w = 1;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return UNITY_READ_FRAMEBUFFER_INPUT(0, i.vertex);
            }
            ENDCG

        }
    
   } Fallback Off
}
