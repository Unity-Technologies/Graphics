Shader "Hidden/Universal Render Pipeline/XR/XRMotionVector"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "XR Camera MotionVectors"

            Cull Off
            ZWrite On
            ColorMask RGBA

            // Stencil test to only fill the pixels that doesn't have object motion data filled by the previous pass.
            Stencil
            {
                WriteMask 1
                ReadMask 1
                Ref 1
                Comp NotEqual

                // Fail Zero
                // Pass Zero
            }

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex Vert
            #pragma fragment Frag

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // -------------------------------------
            // Constants
            float _SpaceWarpNDCModifier;

            // -------------------------------------
            // Structs
            struct Attributes
            {
                uint vertexID   : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 posCS : TEXCOORD0;
                float4 prevPosCS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // -------------------------------------
            // Vertex
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.position = GetFullScreenTriangleVertexPosition(input.vertexID);

                float depth = 1 - UNITY_NEAR_CLIP_VALUE;
                output.position.z = depth;

                // Reconstruct world position
                float3 posWS = ComputeWorldSpacePosition(output.position.xy, depth, UNITY_MATRIX_I_VP);

                // Multiply with current and previous non-jittered view projection
                output.posCS = mul(_NonJitteredViewProjMatrix, float4(posWS.xyz, 1.0));
                output.prevPosCS = mul(_PrevViewProjMatrix, float4(posWS.xyz, 1.0));

                return output;
            }

            // -------------------------------------
            // Fragment
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
                // since uv remap functions use floats
                float3 posNDC = input.posCS.xyz * rcp(input.posCS.w);
                float3 prevPosNDC = input.prevPosCS.xyz * rcp(input.prevPosCS.w);

                // Calculate forward velocity
                float3 velocity = (posNDC - prevPosNDC);

                #if UNITY_UV_STARTS_AT_TOP
                velocity.y = velocity.y * _SpaceWarpNDCModifier;
                #endif

                return float4(velocity.xyz, 0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
