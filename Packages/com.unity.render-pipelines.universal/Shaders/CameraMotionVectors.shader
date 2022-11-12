Shader "Hidden/Universal Render Pipeline/CameraMotionVectors"
{
    SubShader
    {
        Pass
        {
            Name "Camera Motion Vectors"

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // -------------------------------------
            // Vertex
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // TODO: Use Core Blitter vert.
                output.position = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // -------------------------------------
            // Fragment
            half4 frag(Varyings input, out float outDepth : SV_Depth) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.position.xy / _ScaledScreenParams.xy;

                float depth = SampleSceneDepth(uv).x;
                outDepth = depth; // Write depth out unmodified

            #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv).x);
            #endif

                // Reconstruct world position
                float3 posWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                // Multiply with current and previous non-jittered view projection
                float4 posCS = mul(_NonJitteredViewProjMatrix, float4(posWS.xyz, 1.0));
                float4 prevPosCS = mul(_PrevViewProjMatrix, float4(posWS.xyz, 1.0));

                half2 posNDC = posCS.xy * rcp(posCS.w);
                half2 prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

                // Calculate forward velocity
                half2 velocity = (posNDC - prevPosNDC);

                // TODO: test that velocity.y is correct
                #if UNITY_UV_STARTS_AT_TOP
                    velocity.y = -velocity.y;
                #endif

                // Convert velocity from NDC space (-1..1) to screen UV 0..1 space
                // Note: It doesn't mean we don't have negative values, we store negative or positive offset in the UV space.
                // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
                return half4(velocity.xy * 0.5f, 0, 0);
            }

            ENDHLSL
        }
    }
}
