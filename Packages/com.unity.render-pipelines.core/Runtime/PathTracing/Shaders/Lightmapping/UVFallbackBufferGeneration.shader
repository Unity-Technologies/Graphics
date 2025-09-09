Shader "Hidden/UVFallbackBufferGeneration"
{
    SubShader
    {
        Pass
        {
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            #include "GeometryUtils.hlsl"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                nointerpolation float2 vertices[3] : TEXCOORD0;
            };

            uint g_Width;
            uint g_Height;
            float g_WidthScale;
            float g_HeightScale;
            StructuredBuffer<float2> g_VertexBuffer;

            v2f vert (uint vertexId : SV_VertexID)
            {
                v2f o;

                // Expand the triangle.
                uint2 resolution = uint2(g_Width, g_Height);
                float2 tri[3];
                float4 unusedAABB;
                ReadParentTriangle(g_VertexBuffer, vertexId, float4(g_WidthScale, g_HeightScale, 0, 0), tri);
                ExpandTriangleForConservativeRasterization(resolution, tri, vertexId, unusedAABB, o.vertex);

                // Scale the triangle to screen coordinates, pass it to fragment shader.
                o.vertices[0] = tri[0] * resolution;
                o.vertices[1] = tri[1] * resolution;
                o.vertices[2] = tri[2] * resolution;

                return o;
            }

            float2 frag (v2f i, out float depth : SV_Depth) : SV_Target
            {
                float2 a = i.vertices[0];
                float2 b = i.vertices[1];
                float2 c = i.vertices[2];

                // First check the easy case: If the texel center is inside the triangle, no need to clip at all. Just pick it.
                // We need to shrink the triangle by a small epsilon to avoid picking a point precisely on the edge of the triangle,
                // as our intersector cannot handle such points. Epsilon chosen to be 1 order of magnitude larger than the maximum
                // distance between subsequent floats in range [0; 8192]
                float2 texelCenter = i.vertex.xy;
                const float eps = 0.01f;
                if (IsInside(texelCenter, c, b, eps) && IsInside(texelCenter, b, a, eps) && IsInside(texelCenter, a, c, eps))
                {
                    #if UNITY_REVERSED_Z
                    depth = 1.0;
                    #else
                    depth = 0.0;
                    #endif
                    return 0.5;
                }

                // 6 length since clipping a triangle with a quad results in at most a hexagon.
                float2 result[6] = { a, b, c, float2(0, 0), float2(0, 0), float2(0, 0) };
                uint resultSize = 3;

                // Clip to the texel.
                ClipPolygonWithTexel(texelCenter, result, resultSize);

                // Discard removed triangles.
                if (resultSize <= 0)
                    discard;

                // Use the centroid of the clipped triangle as the UV.
                float2 center = float2(0, 0);
                for (uint i = 0; i < resultSize; i++)
                    center += result[i];
                center /= resultSize;
                float2 offset = center - floor(texelCenter);

                // If the centroid lies on the boundary of the texel, discard - this is a singularity.
                if (offset.x <= 0 || offset.x >= 1.0 || offset.y <= 0 || offset.y >= 1.0)
                    discard;

                // Use the zbuffer to pick the triangle with the smallest distance from centroid to texel center.
                float dist = distance(0.5, offset);
                depth = (dist / sqrt(0.5));
                #if UNITY_REVERSED_Z
                depth = 1.0 - depth;
                #endif

                return offset;
            }
            ENDCG
        }
    }
}
