Shader "Hidden/Universal Render Pipeline/TileDepthInfo"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - min&max depth with hardcoded 16x16 tiles
        Pass
        {
            Name "Tile 16x16 MinMax"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
//            #pragma enable_d3d11_debug_symbols

            TEXTURE2D_FLOAT(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTexSize;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = input.uv;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float2 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                float2 baseUv = (uint2(input.positionCS.xy) * 16) * _MainTexSize.zw;

                float minDepth =  1.0;
                float maxDepth = -1.0;
                [unroll] for (int j = 0; j < 16; ++j)
                [unroll] for (int i = 0; i < 16; ++i)
                {
                    float2 uv = baseUv + float2(i, j) * _MainTexSize.zw;
                    uv.y = 1.0 - uv.y; // TODO: Find why the input texture is inverted!
                    float d = SAMPLE_DEPTH_TEXTURE(_MainTex, sampler_MainTex, uv).r;
                    minDepth = min(minDepth, d);
                    maxDepth = max(maxDepth, d);
                }

                // TODO: can fold reversed_z into g_unproject parameters.
                #if UNITY_REVERSED_Z
                minDepth = 1.0 - minDepth;
                maxDepth = 1.0 - maxDepth;
                float c = minDepth; minDepth = maxDepth; maxDepth = c;
                #endif

                return float2(minDepth, maxDepth);
            }

            ENDHLSL
        }
    }
}
