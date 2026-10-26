Shader "Hidden/Test/MultiDrawIndirectProcedural"
{
    Properties
    {
        _BandCount("Band Count", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            // Number of horizontal bands the render target is split into, one per sub draw.
            // Set from the test, must match kDrawCount.
            float _BandCount;

            // Two triangles making up a full width band.
            static const float2 kQuadCorners[6] =
            {
                float2(0, 0), float2(1, 0), float2(0, 1),
                float2(1, 0), float2(1, 1), float2(0, 1)
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                nointerpolation uint subDraw : TEXCOORD0;
            };

            v2f vert(uint vertexID : SV_VertexID)
            {
                // Each sub draw owns 6 consecutive vertices and fills a single band. The sub draw index can be
                // recovered from the vertex id because every backend folds the start vertex/index location of the
                // indirect arguments into SV_VertexID.
                uint subDraw = vertexID / 6;
                float2 corner = kQuadCorners[vertexID % 6];

                v2f o;
                o.position = float4(corner.x * 2.0 - 1.0, -1.0 + 2.0 * (subDraw + corner.y) / _BandCount, 0.0, 1.0);
                o.subDraw = subDraw;
                return o;
            }

            float4 frag(v2f input) : SV_Target
            {
                // Exact 8 bit values (32, 64, 96, ...) against a cleared 0, so the readback tells which sub draws
                // ran and not just how many.
                return float4((input.subDraw + 1) * 32.0 / 255.0, 0.0, 0.0, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
