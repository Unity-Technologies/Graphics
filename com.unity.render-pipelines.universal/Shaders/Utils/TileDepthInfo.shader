Shader "Hidden/Universal Render Pipeline/TileDepthInfo"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

    #if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH)
    #define USE_GATHER 1
    #else
    #define USE_GATHER 0
    #endif

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
            #if USE_GATHER
            #pragma target 4.5 // for GatherRed
            #endif
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

            #if USE_GATHER
            SamplerState my_point_clamp_sampler;
            #endif
            TEXTURE2D_FLOAT(_DepthTex);
            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(3);

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

            uint GenerateBitmask(float4 ds, half listDepthFactorA, half listDepthFactorB)
            {
                #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                ds = 1.0 - ds;
                #endif

                // View space depth (absolute).
                half4 geoDepths = -(_unproject0.z * ds + _unproject0.w) / (_unproject1.z * ds + _unproject1.w);
                int4 bitIndices = geoDepths * listDepthFactorA + listDepthFactorB;
                // bitshift operator has undefined behaviour if bit operarand is out of range on some platforms.
                // On Switch, keeping the test slightly fasten shader execution.
                bool4 isValid = (bitIndices >= 0 && bitIndices <= 31); 
                uint4 bits = isValid << bitIndices;
                return bits.x | bits.y | bits.z | bits.w;
            }

            uint frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(DOWNSAMPLING_SIZE_2)
                    #define DOWNSAMPLING_SIZE 2
                #elif defined(DOWNSAMPLING_SIZE_4)
                    #define DOWNSAMPLING_SIZE 4
                #elif defined(DOWNSAMPLING_SIZE_8)
                    #define DOWNSAMPLING_SIZE 8
                #elif defined(DOWNSAMPLING_SIZE_16)
                    #define DOWNSAMPLING_SIZE 16
                #endif

                #if defined(DOWNSAMPLING_SIZE)
                    const int2 downsamplingSize = int2(DOWNSAMPLING_SIZE, DOWNSAMPLING_SIZE);
                    #define DOWNSAMPLING_LOOP [unroll]
                #else
                    const int2 downsamplingSize = int2(_DownsamplingWidth, _DownsamplingHeight);
                    #define DOWNSAMPLING_LOOP [loop]
                #endif

                int2 fragCoord = int2(input.positionCS.xy);
                int2 sourceCorner = (fragCoord << int2(_SourceShiftX, _SourceShiftY));
                int2 tileCoord = fragCoord >> int2(_TileShiftX, _TileShiftY);
                uint tileID = tileCoord.x + tileCoord.y * _tileXCount;
                uint listDepthRange = LoadDepthRange(tileID - _DepthRangeOffset);
                half listDepthFactorA = f16tof32(listDepthRange);       // a = 32.0 / (listMaxDepth - listMinDepth)
                half listDepthFactorB = f16tof32(listDepthRange >> 16); // b = 32.0 / (listMaxDepth - listMinDepth)

                uint geoBitmask = 0;

                #if !USE_GATHER // Base reference version.
                    DOWNSAMPLING_LOOP for (int j = 0; j < downsamplingSize.y; ++j)
                    DOWNSAMPLING_LOOP for (int i = 0; i < downsamplingSize.x; ++i)
                    {
                        int2 tx = sourceCorner + int2(i, j);
                        tx.y = _DepthTexSize.y - tx.y; // TODO fixme: Depth buffer is y-inverted

                        float d = UNITY_READ_FRAMEBUFFER_INPUT(3, input.positionCS.xy).x;// _DepthTex.Load(int3(tx, 0)).x;
                        #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                        d = 1.0 - d;
                        #endif

                        // View space depth (absolute).
                        half geoDepth = -(_unproject0.z * d + _unproject0.w) / (_unproject1.z * d + _unproject1.w);
                        int bitIndex = geoDepth * listDepthFactorA + listDepthFactorB;
                        // bitshift operator has undefined behaviour if bit operarand is out of range on some platforms.
                        // On Switch, keeping the test slightly fasten shader execution.
                        geoBitmask |= (bitIndex >= 0 && bitIndex <= 31) << bitIndex;
                    }

                #elif !defined(DOWNSAMPLING_SIZE) // Texture fetch optimisation but no loop unrolling.
                    float2 srcCorner = sourceCorner + float2(0.5, 0.5);
                    DOWNSAMPLING_LOOP for (int j = 0; j < downsamplingSize.y; j += 2)
                    DOWNSAMPLING_LOOP for (int i = 0; i < downsamplingSize.x; i += 2)
                    {
                        float2 tx = srcCorner + int2(i, j);
                        tx.y = _DepthTexSize.y - tx.y; // TODO fixme: Depth buffer is y-inverted

                        float4 ds = _DepthTex.GatherRed(my_point_clamp_sampler, tx * _DepthTexSize.zw);
                        geoBitmask |= GenerateBitmask(ds, listDepthFactorA, listDepthFactorB);
                    }

                #else // Texture fetch optimisation but loop unrolling.
                    #define INVERT_Y -1 // TODO fixme: Depth buffer is y-inverted
                    #if defined(INVERT_Y)
                    float2 sourceCornerUv = (sourceCorner + float2(0.5, 0.5)) * _DepthTexSize.zw;
                    sourceCornerUv.y = 1.0 - sourceCornerUv.y;
                    #else
                    float2 sourceCornerUv = sourceCorner * _DepthTexSize.zw;
                    #endif

                    #if DOWNSAMPLING_SIZE >= 2
                    float4 ds0 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv);
                    #endif
                    #if DOWNSAMPLING_SIZE >= 4
                    float4 ds1 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2(2, INVERT_Y * 0) * _DepthTexSize.zw);
                    float4 ds2 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2(0, INVERT_Y * 2) * _DepthTexSize.zw);
                    float4 ds3 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2(2, INVERT_Y * 2) * _DepthTexSize.zw);
                    #endif

                    #if DOWNSAMPLING_SIZE >= 2
                    geoBitmask |= GenerateBitmask(ds0, listDepthFactorA, listDepthFactorB);
                    #endif
                    #if DOWNSAMPLING_SIZE >= 4
                    geoBitmask |= GenerateBitmask(ds1, listDepthFactorA, listDepthFactorB);
                    geoBitmask |= GenerateBitmask(ds2, listDepthFactorA, listDepthFactorB);
                    geoBitmask |= GenerateBitmask(ds3, listDepthFactorA, listDepthFactorB);
                    #endif

                    #define PROCESS_4X4_BLOCK(i, j) \
                        ds0 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 0, INVERT_Y * ((j) + 0) ) * _DepthTexSize.zw); \
                        ds1 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 2, INVERT_Y * ((j) + 0) ) * _DepthTexSize.zw); \
                        ds2 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 0, INVERT_Y * ((j) + 2) ) * _DepthTexSize.zw); \
                        ds3 = _DepthTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 2, INVERT_Y * ((j) + 2) ) * _DepthTexSize.zw); \
                        geoBitmask |= GenerateBitmask(ds0, listDepthFactorA, listDepthFactorB); \
                        geoBitmask |= GenerateBitmask(ds1, listDepthFactorA, listDepthFactorB); \
                        geoBitmask |= GenerateBitmask(ds2, listDepthFactorA, listDepthFactorB); \
                        geoBitmask |= GenerateBitmask(ds3, listDepthFactorA, listDepthFactorB)

                    #if DOWNSAMPLING_SIZE >= 8
                    PROCESS_4X4_BLOCK(4, 0);
                    PROCESS_4X4_BLOCK(0, 4);
                    PROCESS_4X4_BLOCK(4, 4);
                    #endif

                    #if DOWNSAMPLING_SIZE >= 16
                    PROCESS_4X4_BLOCK( 8,  0);
                    PROCESS_4X4_BLOCK(12,  0);
                    PROCESS_4X4_BLOCK( 8,  4);
                    PROCESS_4X4_BLOCK(12,  4);

                    PROCESS_4X4_BLOCK( 0,  8);
                    PROCESS_4X4_BLOCK( 4,  8);
                    PROCESS_4X4_BLOCK( 0, 12);
                    PROCESS_4X4_BLOCK( 4, 12);

                    PROCESS_4X4_BLOCK( 8,  8);
                    PROCESS_4X4_BLOCK(12,  8);
                    PROCESS_4X4_BLOCK( 8, 12);
                    PROCESS_4X4_BLOCK(12, 12);
                    #endif

                    #undef PROCESS_BLOCK

                #endif

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
            #if USE_GATHER
            #pragma target 4.5 // for GatherRed
            #endif
            //#pragma enable_d3d11_debug_symbols

            #if USE_GATHER
            SamplerState my_point_clamp_sampler;
            #endif
            Texture2D<uint> _BitmaskTex;
            float4 _BitmaskTexSize;

            // Missing C# interface to upload int vectors!
            int _DownsamplingWidth;
            int _DownsamplingHeight;

            uint frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                UNITY_SETUP_INSTANCE_ID(input);

                #if defined(DOWNSAMPLING_SIZE_2)
                    #define DOWNSAMPLING_SIZE 2
                #elif defined(DOWNSAMPLING_SIZE_4)
                    #define DOWNSAMPLING_SIZE 4
                #elif defined(DOWNSAMPLING_SIZE_8)
                    #define DOWNSAMPLING_SIZE 8
                #endif

                #if defined(DOWNSAMPLING_SIZE)
                    const int2 downsamplingSize = int2(DOWNSAMPLING_SIZE, DOWNSAMPLING_SIZE);
                    #define DOWNSAMPLING_LOOP [unroll]
                #else
                    const int2 downsamplingSize = int2(_DownsamplingWidth, _DownsamplingHeight);
                    #define DOWNSAMPLING_LOOP [loop]
                #endif

                int2 fragCoord = int2(input.positionCS.xy);
                int2 sourceCorner = fragCoord * downsamplingSize;

                uint geoBitmask = 0;

                #if !USE_GATHER // Base reference version.
                    DOWNSAMPLING_LOOP for (int j = 0; j < downsamplingSize.y; ++j)
                    DOWNSAMPLING_LOOP for (int i = 0; i < downsamplingSize.x; ++i)
                    {
                        int2 tx = sourceCorner + int2(i, j);
                        geoBitmask |= _BitmaskTex.Load(int3(tx, 0));
                    }

                #elif !defined(DOWNSAMPLING_SIZE) // Texture fetch optimisation but no loop unrolling.
                    DOWNSAMPLING_LOOP for (int j = 0; j < downsamplingSize.y; j += 2)
                    DOWNSAMPLING_LOOP for (int i = 0; i < downsamplingSize.x; i += 2)
                    {
                        int2 tx = sourceCorner + int2(i, j);
                        uint4 bits = _BitmaskTex.GatherRed(my_point_clamp_sampler, tx * _BitmaskTexSize.zw);
                        geoBitmask |= bits.x | bits.y | bits.z | bits.w;
                    }

                #else // Texture fetch optimisation but loop unrolling.
                    float2 sourceCornerUv = sourceCorner * _BitmaskTexSize.zw;

                    #if DOWNSAMPLING_SIZE >= 2
                    uint4 bits0 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv);
                    #endif
                    #if DOWNSAMPLING_SIZE >= 4
                    uint4 bits1 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2(2, 0) * _BitmaskTexSize.zw);
                    uint4 bits2 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2(0, 2) * _BitmaskTexSize.zw);
                    uint4 bits3 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2(2, 2) * _BitmaskTexSize.zw);
                    #endif

                    #if DOWNSAMPLING_SIZE >= 2
                    geoBitmask |= bits0.x | bits0.y | bits0.z | bits0.w;
                    #endif
                    #if DOWNSAMPLING_SIZE >= 4
                    geoBitmask |=
                          bits1.x | bits1.y | bits1.z | bits1.w
                        | bits2.x | bits2.y | bits2.z | bits2.w
                        | bits3.x | bits3.y | bits3.z | bits3.w;
                    #endif

                    #define PROCESS_4X4_BLOCK(i, j) \
                        bits0 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 0, (j) + 0 ) * _BitmaskTexSize.zw); \
                        bits1 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 2, (j) + 0 ) * _BitmaskTexSize.zw); \
                        bits2 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 0, (j) + 2 ) * _BitmaskTexSize.zw); \
                        bits3 = _BitmaskTex.GatherRed(my_point_clamp_sampler, sourceCornerUv + float2( (i) + 2, (j) + 2 ) * _BitmaskTexSize.zw); \
                        geoBitmask |= \
                              bits0.x | bits0.y | bits0.z | bits0.w \
                            | bits1.x | bits1.y | bits1.z | bits1.w \
                            | bits2.x | bits2.y | bits2.z | bits2.w \
                            | bits3.x | bits3.y | bits3.z | bits3.w

                    #if DOWNSAMPLING_SIZE >= 8
                    PROCESS_4X4_BLOCK(4, 0);
                    PROCESS_4X4_BLOCK(0, 4);
                    PROCESS_4X4_BLOCK(4, 4);
                    #endif

                    #undef PROCESS_4X4_BLOCK

                #endif

                return geoBitmask;
            }

            ENDHLSL
        }
    }
}
