Shader "Hidden/ChartRasterizerSoftware"
{
    SubShader
    {
        Pass
        {
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "GeometryUtils.hlsl"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                nointerpolation uint chartId : TEXCOORD0;
                nointerpolation float4 aabb : TEXCOORD1;
            };

            uint g_Width;
            uint g_Height;
            float4 g_ScaleAndOffset;
            uint g_ChartIndexOffset;
            StructuredBuffer<float2> g_VertexBuffer;
            StructuredBuffer<uint> g_VertexToOriginalVertex;
            StructuredBuffer<uint> g_VertexToChartID;

            v2f vert (uint vertexId : SV_VertexID)
            {
                v2f o;

                // Expand the triangle.
                uint2 resolution = uint2(g_Width, g_Height);
                float2 tri[3];
                ReadParentTriangle(g_VertexBuffer, vertexId, g_ScaleAndOffset, tri);
                ExpandTriangleForConservativeRasterization(resolution, tri, vertexId, o.aabb, o.vertex);

                // Get the chart index.
                uint originalVertexId = g_VertexToOriginalVertex[vertexId];
                o.chartId = g_VertexToChartID[originalVertexId];

                return o;
            }

            float frag (v2f i) : SV_Target
            {
                // Clip overly conservative edges.
                float2 pos = i.vertex.xy;
                if (pos.x < i.aabb.x || pos.y < i.aabb.y ||
                    pos.x > i.aabb.z || pos.y > i.aabb.w)
                    discard;

                return g_ChartIndexOffset + i.chartId;
            }
            ENDCG
        }
    }
}
