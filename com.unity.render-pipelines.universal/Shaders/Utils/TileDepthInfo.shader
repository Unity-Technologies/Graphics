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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float2 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                int2 baseTexel = int2(input.positionCS.xy) * int2(16, 16);

                float minDepth =  1.0;
                float maxDepth = -1.0;
                [unroll] for (int j = 0; j < 16; ++j)
                [unroll] for (int i = 0; i < 16; ++i)
                {
                    int2 tx = baseTexel + int2(i, j);
                    // TODO fixme: Depth buffer is inverted
                    tx.y = _MainTexSize.y - tx.y;
                    tx = min(tx, int2(_MainTexSize.xy) - 1);
                    float d = _MainTex.Load(int3(tx, 0)).x;
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
