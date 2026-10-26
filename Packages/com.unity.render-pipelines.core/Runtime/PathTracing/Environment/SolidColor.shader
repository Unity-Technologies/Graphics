Shader "Hidden/PathTracing/SolidColor"
{
    Properties { _Color ("Color", Vector) = (0,0,0,0) }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 _Color;

            float4 Vert(uint vertexID : SV_VertexID) : SV_POSITION
            {
                // Fullscreen triangle from the vertex id (DrawProcedural with 3 verts).
                float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
                return float4(uv * 2.0 - 1.0, 0.0, 1.0);
            }

            float4 Frag() : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
