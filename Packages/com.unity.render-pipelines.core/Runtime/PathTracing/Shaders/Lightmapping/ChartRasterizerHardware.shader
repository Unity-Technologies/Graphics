Shader "Hidden/ChartRasterizerHardware"
{
    SubShader
    {
        Pass
        {
            Cull Off
            Conservative On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                nointerpolation uint chartId : TEXCOORD0;
            };

            float4 g_ScaleAndOffset;
            uint g_ChartIndexOffset;
            StructuredBuffer<uint> g_VertexToChartID;

            v2f vert (float4 vertex : POSITION, uint vertexId : SV_VertexID)
            {
                v2f o;
                o.vertex = float4((vertex.xy * g_ScaleAndOffset.xy + g_ScaleAndOffset.zw) * 2-1, 0, 1);
                #if UNITY_UV_STARTS_AT_TOP
                o.vertex.y = -o.vertex.y;
                #endif
                o.chartId = g_VertexToChartID[vertexId];
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                return g_ChartIndexOffset + i.chartId;
            }
            ENDCG
        }
    }
}
