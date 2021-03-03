Shader "Hidden/Test/OutputDepthTexture"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "OutputDepthNormals"
            ZTest Always
            ZWrite Off
            Cull Off


            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = float4(input.positionOS.xyz, 1.0);
                return output;
            }


            float LinearDepthToEyeDepthFloat(float rawDepth)
            {
                #if UNITY_REVERSED_Z
                    return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
                #else
                    return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
                #endif

                return LinearEyeDepth(rawDepth, _ZBufferParams);
                float linearDepth = 0.0;
                if (unity_OrthoParams.w == 1)
                {
                    #if UNITY_REVERSED_Z
                    linearDepth = ((_ProjectionParams.z - _ProjectionParams.y) * (1.0 - rawDepth) + _ProjectionParams.y);
                    #else
                    linearDepth = ((_ProjectionParams.z - _ProjectionParams.y) * (rawDepth) + _ProjectionParams.y);
                    #endif
                }
                else
                {
                    linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                }
                return 1.0 - linearDepth;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.positionCS.xy;
                float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(uv);

                float depth = SampleSceneDepth(normalizedScreenSpaceUV);
                #if !UNITY_REVERSED_Z
                    depth = 1.0 - depth;
                #endif

                return half4(depth, depth, depth, 1);
            }
            ENDHLSL
        }
    }
}
