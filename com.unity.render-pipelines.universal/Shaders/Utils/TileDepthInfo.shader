Shader "Hidden/Universal Render Pipeline/TileDepthInfo"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

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

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Geometry per-tile depth info (calculate bitmask and downsample)
        Pass
        {
            Name "TileDepthInfo"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma multi_compile_fragment __ DOWNSAMPLING_SIZE_2 DOWNSAMPLING_SIZE_4 DOWNSAMPLING_SIZE_8 DOWNSAMPLING_SIZE_16

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
            // Missing C# interface to upload int vectors!
            int _DownsamplingWidth;
            int _DownsamplingHeight;
            int _tileXCount;
            int _DepthRangeOffset;
            int _SourceShiftX;
            int _SourceShiftY;
            int _TileShiftX;
            int _TileShiftY;
            float4 _unproject0;
            float4 _unproject1;

            uint frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(DOWNSAMPLING_SIZE_2)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(2, 2);
                #elif defined(DOWNSAMPLING_SIZE_4)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(4, 4);
                #elif defined(DOWNSAMPLING_SIZE_8)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(8, 8);
                #elif defined(DOWNSAMPLING_SIZE_16)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(16, 16);
                #else
                  #define DOWNSAMPLING_LOOP [loop]
                  const int2 downsamplingSize = int2(_DownsamplingWidth, _DownsamplingHeight);
                #endif

                int2 fragCoord = int2(input.positionCS.xy);
                int2 sourceCorner = (fragCoord << int2(_SourceShiftX, _SourceShiftY));
                int2 tileCoord = fragCoord >> int2(_TileShiftX, _TileShiftY);
                uint tileID = tileCoord.x + tileCoord.y * _tileXCount;
                uint listDepthRange = LoadDepthRange(tileID - _DepthRangeOffset);
                half listDepthFactorA = f16tof32(listDepthRange);       // a = 32.0 / (listMaxDepth - listMinDepth)
                half listDepthFactorB = f16tof32(listDepthRange >> 16); // b = 32.0 / (listMaxDepth - listMinDepth)

                uint geoBitmask = 0;

                DOWNSAMPLING_LOOP for (int j = 0; j < downsamplingSize.y; ++j)
                DOWNSAMPLING_LOOP for (int i = 0; i < downsamplingSize.x; ++i)
                {
                    int2 tx = sourceCorner + int2(i, j);

                    // TODO fixme: Depth buffer is y-inverted
                    tx.y = _DepthTexSize.y - tx.y;

                    // TODO use Gather.
                    #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                    float d = 1.0 - _DepthTex.Load(int3(tx, 0)).x;
                    #else
                    float d = _DepthTex.Load(int3(tx, 0)).x;
                    #endif

                    // View space depth (absolute).
                    half geoDepth = -(_unproject0.z * d + _unproject0.w) / (_unproject1.z * d + _unproject1.w);
                    int bitIndex = geoDepth * listDepthFactorA + listDepthFactorB;
                    // bitshift operator has undefined behaviour if bit operarand is out of range on some platforms.
                    // On Switch, keeping the test slightly fasten shader execution.
                    geoBitmask |= (bitIndex >= 0 && bitIndex <= 31) << bitIndex;
                }

                /*
                #pragma target 5.0 // for GatherRed
                SamplerState my_point_clamp_sampler;
                // TODO fixme: Depth buffer is y-inverted
                int2 tx = int2(sourceCorner.x, _DepthTexSize.y - sourceCorner.y);
                float4 ds = 1.0 - _DepthTex.GatherRed(my_point_clamp_sampler, tx * _DepthTexSize.zw);
                half4 geoDepths = -(_unproject0.z * ds + _unproject0.w) / (_unproject1.z * ds + _unproject1.w);
                int4 bitIndices = geoDepths * listDepthFactorA + listDepthFactorB;
                // bitshift operator has undefined behaviour if bit operarand is out of range on some platforms.
                // On Switch, keeping the test slightly fasten shader execution.
                bool4 isValid = (bitIndices >= 0 && bitIndices <= 31); 
                uint4 bits = isValid << bitIndices;
                geoBitmask |= bits.x | bits.y | bits.z | bits.w;
                */

                return geoBitmask;
            }

            ENDHLSL
        }

        // 1 - Geometry per-tile depth info (downsample only)
        Pass
        {
            Name "downsampleBitmask"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma multi_compile_fragment __ DOWNSAMPLING_SIZE_2 DOWNSAMPLING_SIZE_4 DOWNSAMPLING_SIZE_8

            #pragma vertex vert
            #pragma fragment frag
            //#pragma enable_d3d11_debug_symbols

            Texture2D<uint> _BitmaskTex;

            // Missing C# interface to upload int vectors!
            int _DownsamplingWidth;
            int _DownsamplingHeight;

            uint frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(DOWNSAMPLING_SIZE_2)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(2, 2);
                #elif defined(DOWNSAMPLING_SIZE_4)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(4, 4);
                #elif defined(DOWNSAMPLING_SIZE_8)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(8, 8);
                #elif defined(DOWNSAMPLING_SIZE_16)
                  #define DOWNSAMPLING_LOOP [unroll]
                  const int2 downsamplingSize = int2(16, 16);
                #else
                  #define DOWNSAMPLING_LOOP [loop]
                  const int2 downsamplingSize = int2(_DownsamplingWidth, _DownsamplingHeight);
                #endif

                int2 fragCoord = int2(input.positionCS.xy);
                int2 sourceCorner = fragCoord * downsamplingSize;

                uint geoBitmask = 0;

                DOWNSAMPLING_LOOP for (int j = 0; j < downsamplingSize.y; ++j)
                DOWNSAMPLING_LOOP for (int i = 0; i < downsamplingSize.x; ++i)
                {
                    int2 tx = sourceCorner + int2(i, j);
                    // TODO use Gather.
                    geoBitmask |= _BitmaskTex.Load(int3(tx, 0));
                }

                return geoBitmask;
            }

            ENDHLSL
        }
    }
}
