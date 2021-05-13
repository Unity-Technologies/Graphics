Shader "Hidden/kMotion/ObjectMotionVectors"
{
    SubShader
    {
        Pass
        {
            // Lightmode tag required setup motion vector parameters by C++ (legacy Unity)
            Tags{ "LightMode" = "MotionVectors" }

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


#if defined(USING_STEREO_MATRICES)
        float4x4 _PrevViewProjMStereo[2];
#define _PrevViewProjM _PrevViewProjMStereo[unity_StereoEyeIndex]
#else
#define  _PrevViewProjM _PrevViewProjMatrix
#endif

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
                float4 positionCS           : SV_POSITION;
                float4 positionVP           : TEXCOORD0;
                float4 previousPositionVP   : TEXCOORD1;
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

                output.positionCS = TransformObjectToHClip(input.position.xyz);

                // this works around an issue with dynamic batching
                // potentially remove in 5.4 when we use instancing
                #if defined(UNITY_REVERSED_Z)
                    output.positionCS.z -= unity_MotionVectorsParams.z * output.positionCS.w;
                #else
                    output.positionCS.z += unity_MotionVectorsParams.z * output.positionCS.w;
                #endif

                output.positionVP = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, input.position));

                const float4 prevPos = (unity_MotionVectorsParams.x == 1) ? float4(input.positionOld, 1) : input.position;
                output.previousPositionVP = mul(_PrevViewProjM, mul(unity_MatrixPreviousM, prevPos));

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
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                // Calculate positions
                float4 posVP = input.positionVP;
                float4 prevPosVP = input.previousPositionVP;
                posVP.xy *= rcp(posVP.w);
                prevPosVP.xy *= rcp(prevPosVP.w);

                // Calculate velocity
                float2 velocity = (posVP.xy - prevPosVP.xy);
                #if UNITY_UV_STARTS_AT_TOP
                    velocity.y = -velocity.y;
                #endif

                // Convert from Clip space (-1..1) to NDC 0..1 space.
                // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
                // Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (velocity * 0.5)
                return float4(velocity.xy * 0.5, 0, 0);
            }
            ENDHLSL
        }
    }
}
