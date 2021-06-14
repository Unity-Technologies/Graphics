Shader "MotionVecDebug"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "MotionVectorDebugPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionHCS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Note: The pass is setup with a mesh already in CS
                // Therefore, we can just output vertex position
                output.positionCS = float4(input.positionHCS.xyz, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                output.positionCS.y *= -1;
                #endif

                output.uv = input.uv;
                return output;
            }

            TEXTURE2D_X(_MotionVectorTexture);
            SAMPLER(sampler_MotionVectorTexture);

            float4 _SourceTex_TexelSize;
            float _Intensity;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 vel = SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_MotionVectorTexture, input.uv);

                return vel * _Intensity;
            }
            ENDHLSL
        }
    }
}
