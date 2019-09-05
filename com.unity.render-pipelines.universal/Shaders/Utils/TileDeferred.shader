Shader "Hidden/Universal Render Pipeline/TileDeferred"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

    #define MAX_UNIFORM_BUFFER_SIZE (64 * 1024)
    #define SIZEOF_VEC4_TILEDATA 1 // uint4
    #define SIZEOF_VEC4_POINTLIGHTDATA 2 // 2 float4
    #define MAX_TILES_PER_PATCH (MAX_UNIFORM_BUFFER_SIZE / (16 * SIZEOF_VEC4_TILEDATA))
    #define MAX_POINTLIGHT_PER_BATCH (MAX_UNIFORM_BUFFER_SIZE / (16 * SIZEOF_VEC4_POINTLIGHTDATA))
    #define MAX_REL_LIGHT_INDICES_PER_BATCH (MAX_UNIFORM_BUFFER_SIZE / 4) // Should be ushort!
    #define LIGHT_LIST_HEADER_SIZE 1

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Tiled Deferred Point Light
        Pass
        {
            Name "Tiled Deferred Point Light"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One One, Zero One
            BlendOp Add, Add

            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment PointLightShading
//            #pragma enable_d3d11_debug_symbols
            #pragma enable_cbuffer

            CBUFFER_START(UTileDataBuffer)
            uint4 g_TileDataBuffer[MAX_TILES_PER_PATCH * SIZEOF_VEC4_TILEDATA];
            CBUFFER_END

            struct TileData
            {
                uint tileID;         // 2x 16 bits
                uint relLightOffset; // 16 bits is enough
                uint listDepthRange; // 2x halfs
                uint listBitMask;    // 32 bits
            };

            TileData LoadTileData(int i)
            {
                i *= SIZEOF_VEC4_TILEDATA;
                TileData tileData;
                tileData.tileID         = g_TileDataBuffer[i][0];
                tileData.relLightOffset = g_TileDataBuffer[i][1];
                tileData.listDepthRange = g_TileDataBuffer[i][2];
                tileData.listBitMask    = g_TileDataBuffer[i][3];
                return tileData;
            }

            // Keep in sync with PackTileID().
            uint2 UnpackTileID(uint tileID)
            {
                return uint2(tileID & 0xFFFF, (tileID >> 16) & 0xFFFF);
            }

            uint g_TilePixelWidth;
            uint g_TilePixelHeight;
            uint g_InstanceOffset;
            float4 g_unproject0;
            float4 g_unproject1;

            Texture2D _TileDepthRangeTexture;

            struct Attributes
            {
                uint vertexID   : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation int relLightOffset : TEXCOORD0;
                noperspective float2 clipCoord : TEXCOORD1;
            };

            /*
            bool IsWithinRange(float d, float minDepth, float maxDepth, uint bitMask)
            {
                if (d < minDepth || d > maxDepth)
                    return false;
                int bitIndex = int(32.0 * (d - minDepth) / (maxDepth - minDepth));
                return (bitMask & (1u << bitIndex)) !=  0;
            }
            */

            Varyings Vertex(Attributes input)
            {
                uint instanceID = g_InstanceOffset + input.instanceID;

                TileData tileData = LoadTileData(instanceID);
                uint2 tileCoord = UnpackTileID(tileData.tileID);
                float2 geoDepthRange = _TileDepthRangeTexture.Load(int3(tileCoord, 0)).xy;
                // View space depth (absolute).
                half geoMinDepth = -dot(g_unproject0, float4(0, 0, geoDepthRange.x, 1)) / dot(g_unproject1, float4(0, 0, geoDepthRange.x, 1));
                half geoMaxDepth = -dot(g_unproject0, float4(0, 0, geoDepthRange.y, 1)) / dot(g_unproject1, float4(0, 0, geoDepthRange.y, 1));
                half listMinDepth = f16tof32(tileData.listDepthRange);
                half listMaxDepth = f16tof32(tileData.listDepthRange >> 16);
                bool shouldDiscard = (min(listMaxDepth, geoMaxDepth) - max(listMinDepth, geoMinDepth) < 0);

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
                float2 pp = GetQuadVertexPosition(quadIndex).xy;
                uint2 pixelCoord  = tileCoord * uint2(g_TilePixelWidth, g_TilePixelHeight);
                pixelCoord += uint2(pp.xy * uint2(g_TilePixelWidth, g_TilePixelHeight));
                float2 clipCoord = (pixelCoord * _ScreenSize.zw) * 2.0 - 1.0;

                Varyings output;
                output.positionCS = float4(clipCoord, shouldDiscard ? -2 : 0, 1);
//              Screen is already y flipped (different from HDRP)?
//                // Tiles coordinates always start at upper-left corner of the screen (y axis down).
//                // Clip-space coordinatea always have y axis up. Hence, we must always flip y.
//                output.positionCS.y *= -1.0;

                output.clipCoord = clipCoord;
                // Screen is flipped!!!!!!
                output.clipCoord.y *= -1.0;

                output.relLightOffset = tileData.relLightOffset;

                return output;
            }

            CBUFFER_START(UPointLightBuffer)
            // Unity does not support structure inside cbuffer unless for instancing case (not safe to use here).
            uint4 g_PointLightBuffer[MAX_POINTLIGHT_PER_BATCH * SIZEOF_VEC4_POINTLIGHTDATA];
            CBUFFER_END

            PointLightData LoadPointLightData(int relLightIndex)
            {
                uint i = relLightIndex * SIZEOF_VEC4_POINTLIGHTDATA;
                PointLightData pl;
                pl.WsPos  = asfloat(g_PointLightBuffer[i + 0].xyz);
                pl.Radius = asfloat(g_PointLightBuffer[i + 0].w);
                pl.Color.r = f16tof32(g_PointLightBuffer[i + 1].r); 
                pl.Color.g = f16tof32(g_PointLightBuffer[i + 1].r >> 16); 
                pl.Color.b = f16tof32(g_PointLightBuffer[i + 1].g); 
                return pl;
            }

            CBUFFER_START(URelLightIndexBuffer)
            uint4 g_RelLightIndexBuffer[MAX_REL_LIGHT_INDICES_PER_BATCH/4];
            CBUFFER_END

            uint LoadRelLightIndex(int i)
            {
                return g_RelLightIndexBuffer[i >> 2][i & 3];
            }

            Texture2D g_DepthTex;

            half4 PointLightShading(Varyings input) : SV_Target
            {
                int lightCount = LoadRelLightIndex(input.relLightOffset);

                #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                float d = 1.0 - g_DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #else
                float d = g_DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #endif

                // Temporary code to calculate fragment world space position.
                float4 wsPos = mul(_InvCameraViewProj, float4(input.clipCoord, d * 2.0 - 1.0, 1.0));
                wsPos.xyz *= 1.0 / wsPos.w;

                half3 color = 0.0.xxx;

                // We always have at least 1 light by design. do...while allows the compiler to optimize for the first light case.
                int li = 0;
                do
                {
                    int offsetInList = input.relLightOffset + LIGHT_LIST_HEADER_SIZE + li;
                    int relLightIndex = LoadRelLightIndex(offsetInList);
                    PointLightData light = LoadPointLightData(relLightIndex);

                    // TODO calculate lighting.
                    float3 L = light.WsPos - wsPos.xyz;
                    half att = dot(L, L) < light.Radius*light.Radius ? 1.0 : 0.0;

                    color += light.Color.rgb * att * 0.1;
                }
                while (++li < lightCount);

                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
