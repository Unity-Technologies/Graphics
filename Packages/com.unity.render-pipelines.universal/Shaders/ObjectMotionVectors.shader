Shader "Hidden/Universal Render Pipeline/ObjectMotionVectors"
{
    SubShader
    {
        Pass
        {
            Name "Object Motion Vectors"

            // Lightmode tag required setup motion vector parameters by C++ (legacy Unity)
            Tags{ "LightMode" = "MotionVectors" }

            HLSLPROGRAM
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#ifndef HAVE_VFX_MODIFICATION
    #pragma multi_compile _ DOTS_INSTANCING_ON
    #if UNITY_PLATFORM_ANDROID || UNITY_PLATFORM_WEBGL || UNITY_PLATFORM_UWP
        #pragma target 3.5 DOTS_INSTANCING_ON
    #else
        #pragma target 4.5 DOTS_INSTANCING_ON
    #endif
#endif // HAVE_VFX_MODIFICATION

            // -------------------------------------
            // Structs
            struct Attributes
            {
                float4 position             : POSITION;
                float3 positionOld          : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS                 : SV_POSITION;
                float4 positionCSNoJitter         : TEXCOORD0;
                float4 previousPositionCSNoJitter : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // -------------------------------------
            // Vertex
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);

                // Jittered. Match the frame.
                output.positionCS = vertexInput.positionCS;

                // This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platforms
                #if defined(UNITY_REVERSED_Z)
                    output.positionCS.z -= unity_MotionVectorsParams.z * output.positionCS.w;
                #else
                    output.positionCS.z += unity_MotionVectorsParams.z * output.positionCS.w;
                #endif

                output.positionCSNoJitter = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.position));

                const float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.position;
                output.previousPositionCSNoJitter = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, prevPos));

                return output;
            }

            // -------------------------------------
            // Fragment
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
                bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
                if (forceNoMotion)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }

                // Calculate positions
                float4 posCS = input.positionCSNoJitter;
                float4 prevPosCS = input.previousPositionCSNoJitter;

                half2 posNDC = posCS.xy * rcp(posCS.w);
                half2 prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

                // Calculate forward velocity
                half2 velocity = (posNDC.xy - prevPosNDC.xy);
                #if UNITY_UV_STARTS_AT_TOP
                    velocity.y = -velocity.y;
                #endif

                // Convert velocity from NDC space (-1..1) to UV 0..1 space
                // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
                // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
                return half4(velocity.xy * 0.5, 0, 0);
            }
            ENDHLSL
        }
    }
}
