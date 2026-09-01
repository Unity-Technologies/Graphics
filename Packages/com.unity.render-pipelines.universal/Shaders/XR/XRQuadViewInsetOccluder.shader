Shader "Hidden/Universal Render Pipeline/XRInsetOccluder"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "XRInsetOccluder"

            ColorMask 0
            ZWrite On
            ZTest LEqual   // Compare op must match with GBuffer shaders to keep hardware optimizations like Adreno LRZ
            Blend Off
            Cull Off
            // No stencil ops needed

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Draw full-screen triangle (clipped to the inset region by the pass viewport)
                // Set nearest depth to occlude everything afterward in this pass
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_NEAR_CLIP_VALUE);
                return output;
            }

            void Frag()
            {
                // ColorMask 0 discards all color output; only the near-plane depth from SV_POSITION is written
            }
            ENDHLSL
        }
    }
}