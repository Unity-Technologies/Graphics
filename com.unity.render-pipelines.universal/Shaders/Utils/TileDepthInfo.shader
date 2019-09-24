Shader "Hidden/Universal Render Pipeline/TileDepthInfo"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Geometry per-tile depth info
        Pass
        {
            Name "TileDepthRange"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma multi_compile_fragment __ TILE_SIZE_2 TILE_SIZE_4 TILE_SIZE_8 TILE_SIZE_16

            #pragma vertex vert
            #pragma fragment frag
            //#pragma enable_d3d11_debug_symbols

            #if USE_CBUFFER_FOR_DEPTHRANGE
                CBUFFER_START(UDepthRanges)
                uint4 _DepthRanges[MAX_DEPTHRANGE_PER_CBUFFER_BATCH/4];
                CBUFFER_END

                uint LoadDepthRange(uint i) { return _DepthRanges[i >> 2][i & 3]; }

            #else
                StructuredBuffer<uint> _DepthRanges;

                uint LoadDepthRange(uint i) { return _DepthRanges[i]; }

            #endif

            TEXTURE2D_FLOAT(_DepthTex);

            float4 _DepthTexSize;
            int _TilePixelWidth;
            int _TilePixelHeight;
            int _tileXCount;
            int _DepthRangeOffset;
            float4 _unproject0;
            float4 _unproject1;

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

            uint frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(TILE_SIZE_2)
                  #define TILE_SIZE_UNROLL [unroll]
                  const int2 tileSize = int2(2, 2);
                #elif defined(TILE_SIZE_4)
                  #define TILE_SIZE_UNROLL [unroll]
                  const int2 tileSize = int2(4, 4);
                #elif defined(TILE_SIZE_8)
                  #define TILE_SIZE_UNROLL [unroll]
                  const int2 tileSize = int2(8, 8);
                #elif defined(TILE_SIZE_16)
                  #define TILE_SIZE_UNROLL [unroll]
                  const int2 tileSize = int2(16, 16);
                #else
                  #define TILE_SIZE_UNROLL
                  const int2 tileSize = int2(_TilePixelWidth, _TilePixelHeight);
                #endif

                int2 fragPos = int2(input.positionCS.xy);
                int2 baseTexel = fragPos * tileSize;

                uint tileID = fragPos.x + fragPos.y * _tileXCount;

                uint listDepthRange = LoadDepthRange(tileID - _DepthRangeOffset);
                half listMinDepth = f16tof32(listDepthRange);
                half listMaxDepth = f16tof32(listDepthRange >> 16);

                uint geoBitmask = 0;

                TILE_SIZE_UNROLL for (int j = 0; j < tileSize.y; ++j)
                TILE_SIZE_UNROLL for (int i = 0; i < tileSize.x; ++i)
                {
                    int2 tx = baseTexel + int2(i, j);

                    // TODO fixme: Depth buffer is y-inverted
                    tx.y = _DepthTexSize.y - tx.y;

                    #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                    float d = 1.0 - _DepthTex.Load(int3(tx, 0)).x;
                    #else
                    float d = _DepthTex.Load(int3(tx, 0)).x;
                    #endif

                    // View space depth (absolute).
                    half geoDepth = -dot(_unproject0, float4(0, 0, d, 1)) / dot(_unproject1, float4(0, 0, d, 1));
                    int bitIndex = 32.0h * (geoDepth - listMinDepth) / (listMaxDepth - listMinDepth);
                    // bitshift operator has undefined behaviour if bit operarand is out of range, so the test cannot be removed.
                    geoBitmask |= (bitIndex >= 0 && bitIndex <= 31) ? uint(1) << bitIndex : 0;
                }

                return geoBitmask;
            }

            ENDHLSL
        }
    }
}
