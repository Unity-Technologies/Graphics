Shader "Universal Render Pipeline/SpatialMappingOcclusion"
{
    SubShader
    {
        Tags {"RenderType" = "Opaque" "IgnoreProjector" = "False" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Blend One OneMinusSrcAlpha
        ZWrite On
        ZTest LEqual
        Colormask 0
        Cull Off

        Pass
        {
            Name "Spatial Mapping Occlusion"

            HLSLPROGRAM
            #pragma gles gles3 glcore
            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "UnlitInput.hlsl"

            struct Attributes
            {
                float4 positionOS       : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex  : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
