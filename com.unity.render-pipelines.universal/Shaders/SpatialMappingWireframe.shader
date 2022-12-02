Shader "Universal Render Pipeline/VR/SpatialMapping/Wireframe"
{
    Properties
    {
        _WireThickness ("Wire Thickness", RANGE(0, 800)) = 100
    }
    SubShader
    {
        Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Spatial Mapping Wireframe"

            // Wireframe shader based on the the following
            // http://developer.download.nvidia.com/SDK/10/direct3d/Source/SolidWireframe/Doc/SolidWireframe.pdf

            HLSLPROGRAM
            #pragma require geometry

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_instancing

            #include "UnlitInput.hlsl"

            float _WireThickness;

            struct Attributes
            {
                float4 positionOS       : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 projectionSpaceVertex : SV_POSITION;
                float4 worldSpacePosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2g vert(Attributes input)
            {
                v2g output = (v2g)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.projectionSpaceVertex = vertexInput.positionCS;
                output.worldSpacePosition = mul(UNITY_MATRIX_M, input.positionOS);

                return output;
            }

            struct g2f
            {
                float4 projectionSpaceVertex : SV_POSITION;
                float4 worldSpacePosition : TEXCOORD0;
                float4 dist : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            [maxvertexcount(3)]
            void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i[0]);

                float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
                float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
                float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;

                float2 edge0 = p2 - p1;
                float2 edge1 = p2 - p0;
                float2 edge2 = p1 - p0;

                // To find the distance to the opposite edge, we take the
                // formula for finding the area of a triangle Area = Base/2 * Height,
                // and solve for the Height = (Area * 2)/Base.
                // We can get the area of a triangle by taking its cross product
                // divided by 2.  However we can avoid dividing our area/base by 2
                // since our cross product will already be double our area.
                float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
                float wireThickness = 800 - _WireThickness;

                g2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldSpacePosition = i[0].worldSpacePosition;
                o.projectionSpaceVertex = i[0].projectionSpaceVertex;
                o.dist.xyz = float3( (area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                triangleStream.Append(o);

                o.worldSpacePosition = i[1].worldSpacePosition;
                o.projectionSpaceVertex = i[1].projectionSpaceVertex;
                o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                triangleStream.Append(o);

                o.worldSpacePosition = i[2].worldSpacePosition;
                o.projectionSpaceVertex = i[2].projectionSpaceVertex;
                o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
                o.dist.w = 1.0 / o.projectionSpaceVertex.w;
                triangleStream.Append(o);
            }

            half4 frag(g2f i) : SV_Target
            {
                float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];

                // Early out if we know we are not on a line segment.
                if(minDistanceToEdge > 0.9)
                {
                    return half4(0,0,0,0);
                }

                // Smooth our line out
                float t = exp2(-2 * minDistanceToEdge * minDistanceToEdge);

                const half4 colors[11] = {
                        half4(1.0, 1.0, 1.0, 1.0),  // White
                        half4(1.0, 0.0, 0.0, 1.0),  // Red
                        half4(0.0, 1.0, 0.0, 1.0),  // Green
                        half4(0.0, 0.0, 1.0, 1.0),  // Blue
                        half4(1.0, 1.0, 0.0, 1.0),  // Yellow
                        half4(0.0, 1.0, 1.0, 1.0),  // Cyan/Aqua
                        half4(1.0, 0.0, 1.0, 1.0),  // Magenta
                        half4(0.5, 0.0, 0.0, 1.0),  // Maroon
                        half4(0.0, 0.5, 0.5, 1.0),  // Teal
                        half4(1.0, 0.65, 0.0, 1.0), // Orange
                        half4(1.0, 1.0, 1.0, 1.0)   // White
                    };

                float cameraToVertexDistance = length(_WorldSpaceCameraPos - i.worldSpacePosition.xyz);
                int index = clamp(floor(cameraToVertexDistance), 0, 10);
                half4 wireColor = colors[index];

                half4 finalColor = lerp(float4(0,0,0,1), wireColor, t);
                finalColor.a = t;

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
