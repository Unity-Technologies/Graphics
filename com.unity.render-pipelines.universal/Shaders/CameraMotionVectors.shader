Shader "Hidden/kMotion/CameraMotionVectors"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite On
            ZTest Always

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

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

                output.position = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            // -------------------------------------
            // Fragment
            half4 frag(Varyings input, out float outDepth : SV_Depth) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.position.xy / _ScaledScreenParams.xy;
            #if UNITY_REVERSED_Z
                half depth = SampleSceneDepth(uv).x;
            #else
                half depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv).x);
            #endif
                outDepth = depth;

                float3 wpos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                // Multiply with projection
                float4 previousPositionVP = mul(_PrevViewProjMatrix, float4(wpos.xyz, 1.0));
                float4 positionVP = mul(_ViewProjMatrix, float4(wpos.xyz, 1.0));

                previousPositionVP.xy *= rcp(previousPositionVP.w);
                positionVP.xy *= rcp(positionVP.w);

                // Calculate velocity
                float2 velocity = (positionVP.xy - previousPositionVP.xy);
            #if UNITY_UV_STARTS_AT_TOP
                    velocity.y = -velocity.y;
            #endif

                // Convert velocity from Clip space (-1..1) to NDC 0..1 space
                // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
                // Note: ((positionVP * 0.5 + 0.5) - (previousPositionVP * 0.5 + 0.5)) = (velocity * 0.5)
                return half4(velocity.xy * 0.5, 0, 0);
            }

            ENDHLSL
        }
    }
}
