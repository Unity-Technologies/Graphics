Shader "Hidden/Universal Render Pipeline/Blit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "Blit"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#ifdef _LINEAR_TO_SRGB_CONVERSION
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#endif

            TEXTURE2D_X(_BlitTex);
            SAMPLER(sampler_BlitTex);

            float4 _BlitScaleBias;

            struct Attributes
            {
#if _USE_DRAW_PROCEDURAL
                uint vertexID       : SV_VertexID;
#else
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
#endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float2 uv            : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if _USE_DRAW_PROCEDURAL
                output.positionCS = GetQuadVertexPosition(input.vertexID);
                output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
                output.uv = GetQuadTexCoord(input.vertexID) * _BlitScaleBias.xy + _BlitScaleBias.zw;
#else
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
#endif  
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTex, sampler_BlitTex, input.uv);

                #ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
                #endif
                return col;
            }
            ENDHLSL
        }
    }
}
