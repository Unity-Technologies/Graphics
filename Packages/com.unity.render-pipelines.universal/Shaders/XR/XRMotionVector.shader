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

                output.position = GetFullScreenTriangleVertexPosition(input.vertexID, 1 - UNITY_NEAR_CLIP_VALUE);

                // Reconstruct world position
                // We can use the clip space as is because contrary to the convention mentioned in Common.hlsl (RP Core),
                // this clip space is already Y-up
                float3 posWS = ComputeWorldSpacePosition(output.position, UNITY_MATRIX_I_VP);
                
                // Multiply with current and previous non-jittered view projection
                output.posCS = mul(_NonJitteredViewProjMatrix, float4(posWS, 1.0));
                output.prevPosCS = mul(_PrevViewProjMatrix, float4(posWS, 1.0));

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

        // Pass 1: Blit valid depth data from _XRDepthTexture to motionvector depth target
        Pass
        {
            Name "XR MotionVector Depth Copy"

            Cull Off
            ZWrite On
            ColorMask RGBA

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_fragment _ _SUBSAMPLE_DEPTH

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
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // -------------------------------------
            // Vertex
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.position = GetFullScreenTriangleVertexPosition(input.vertexID, 1 - UNITY_NEAR_CLIP_VALUE);
                output.posCS = output.position;
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            TEXTURE2D_X(_XRDepthTexture);
            SAMPLER(sampler_XRDepthTexture);

            // -------------------------------------
            // Fragment
            float4 Frag(Varyings input, out float outDepth : SV_Depth) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;

                // The backbuffer depth is imported with textureUVOrigin = TopLeft on Vulkan,
                // while the motion vector depth target defaults to BottomLeft. Compensate here.
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif

             #if _SUBSAMPLE_DEPTH && SHADER_TARGET >= 45 && defined(PLATFORM_SUPPORT_GATHER)
                float4 depth4 = GATHER_RED_TEXTURE2D_X(_XRDepthTexture, sampler_XRDepthTexture, uv);
                #if UNITY_REVERSED_Z
                    float depth = min(min(depth4.x, depth4.y), min(depth4.z, depth4.w));
                #else
                    float depth = max(max(depth4.x, depth4.y), max(depth4.z, depth4.w));
                #endif
            #else
                float depth = SAMPLE_TEXTURE2D_X(_XRDepthTexture, sampler_XRDepthTexture, uv).x;
            #endif

                // This is required to avoid artifacts from the motion vector pass outputting the same z
            #if UNITY_REVERSED_Z
                outDepth = depth - 0.0001; // Write depth with a small offset
            #else
                outDepth = depth + 0.0001; // Write depth with a small offset
            #endif

                // Reconstruct world position from the sampled depth instead of the
                // far-plane WS interpolated from the vertex stage. Without this, static
                // pixels get camera motion as if they were at the far plane and lose
                // parallax during head translation.
                float4 posCSReal = float4(input.posCS.xy, depth, 1.0);
                float3 posWS = ComputeWorldSpacePosition(posCSReal, UNITY_MATRIX_I_VP);

                float4 posCS = mul(_NonJitteredViewProjMatrix, float4(posWS, 1.0));
                float4 prevPosCS = mul(_PrevViewProjMatrix, float4(posWS, 1.0));

                // Non-uniform raster needs to keep the posNDC values in float to avoid additional conversions
                // since uv remap functions use floats
                float3 posNDC = posCS.xyz * rcp(posCS.w);
                float3 prevPosNDC = prevPosCS.xyz * rcp(prevPosCS.w);

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
