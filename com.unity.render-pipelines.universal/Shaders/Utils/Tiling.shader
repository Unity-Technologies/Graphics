Shader "Hidden/Universal Render Pipeline/Tiling"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // TODO customize per platform.
    #define MAX_UNIFORM_BUFFER_SIZE (64 * 1024)
    #define MAX_TILES_PER_PATCH (MAX_UNIFORM_BUFFER_SIZE / 4) // uint
    #define SIZEOF_VEC4_POINTLIGHT_DATA 2 // 2 float4
    #define MAX_POINTLIGHT_PER_BATCH (MAX_UNIFORM_BUFFER_SIZE / (16 * SIZEOF_VEC4_POINTLIGHT_DATA))
    #define MAX_REL_LIGHT_INDICES_PER_BATCH (MAX_UNIFORM_BUFFER_SIZE / 4) // Should be ushort!

    // Keep in sync with PackTileID().
    uint2 UnpackTileID(uint tileID)
    {
        return uint2(tileID & 0xFFFF, (tileID >> 16) & 0xFFFF);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Deferred Point Light
        Pass
        {
            Name "Deferred Point Light"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend One One, Zero One
            BlendOp Add, Add

            HLSLPROGRAM

            // Required to compile gles 2.0 with standard srp library
//            #pragma prefer_hlslcc gles
//            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment PointLightShading
//            #pragma enable_d3d11_debug_symbols
            #pragma enable_cbuffer

            CBUFFER_START(UTileIDBuffer)
            uint4 g_TileIDBuffer[MAX_TILES_PER_PATCH/4];
            CBUFFER_END

            CBUFFER_START(UTileRelLightBuffer)
            uint4 g_TileRelLightBuffer[MAX_TILES_PER_PATCH/4];
            CBUFFER_END

            uint g_TilePixelWidth;
            uint g_TilePixelHeight;
            uint g_InstanceOffset;

            struct Attributes
            {
                uint vertexID   : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation uint relLightOffset : TEXCOORD0;
                noperspective float2 clipCoord : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                uint instanceID = g_InstanceOffset + input.instanceID;
                uint  tileID = g_TileIDBuffer[instanceID >> 2][instanceID & 3];
                uint2 tileCoord = UnpackTileID(tileID);
                uint2 pixelCoord  = tileCoord * uint2(g_TilePixelWidth, g_TilePixelHeight);

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
                float2 pp = GetQuadVertexPosition(quadIndex).xy;
                pixelCoord += uint2(pp.xy * uint2(g_TilePixelWidth, g_TilePixelHeight));
                float2 clipCoord = (pixelCoord * _ScreenSize.zw) * 2.0 - 1.0;

                Varyings output;
                output.positionCS = float4(clipCoord, 0, 1);
//              Screen is already y flipped (different from HDRP)?
//                // Tiles coordinates always start at upper-left corner of the screen (y axis down).
//                // Clip-space coordinatea always have y axis up. Hence, we must always flip y.
//                output.positionCS.y *= -1.0;

                output.relLightOffset = g_TileRelLightBuffer[instanceID >> 2][instanceID & 3];
                output.clipCoord = clipCoord;

                // Screen is flipped!!!!!!
                output.clipCoord.y *= -1.0;

                return output;
            }


            struct PointLightData
            {
                float3 WsPos;
                float Radius;
                float4 Color;
            };

            CBUFFER_START(UPointLightBuffer)
            // Unity does not support structure inside cbuffer unless for instancing case (not safe to use here).
            float4 g_PointLightBuffer[MAX_POINTLIGHT_PER_BATCH * SIZEOF_VEC4_POINTLIGHT_DATA];
            CBUFFER_END

            CBUFFER_START(URelLightIndexBuffer)
            uint4 g_RelLightIndexBuffer[MAX_REL_LIGHT_INDICES_PER_BATCH/4];
            CBUFFER_END

            float4 g_unproject0;
            float4 g_unproject1;
            Texture2D g_DepthTex;

            PointLightData LoadPointLightData(int relLightIndex)
            {
                PointLightData pl;
                pl.WsPos = g_PointLightBuffer[relLightIndex].xyz;
                pl.Radius = g_PointLightBuffer[relLightIndex].w;
                pl.Color = g_PointLightBuffer[MAX_POINTLIGHT_PER_BATCH + relLightIndex];
                return pl;
            }

            uint LoadRelLightIndex(int i)
            {
                return g_RelLightIndexBuffer[i >> 2][i & 3];
            }

            bool IsWithinRange(float d, float minDepth, float maxDepth, uint bitMask)
            {
                if (d < minDepth || d > maxDepth)
                    return false;
                int bitIndex = int(32.0 * (d - minDepth) / (maxDepth - minDepth));
                return (bitMask & (1u << bitIndex)) !=  0;
            }

            half4 PointLightShading(Varyings input) : SV_Target
            {
                uint lightCount = LoadRelLightIndex(input.relLightOffset);
                // absolute min&max depth range of the light list in view space.
                float minDepth = f16tof32(LoadRelLightIndex(input.relLightOffset + 1));
                float maxDepth = f16tof32(LoadRelLightIndex(input.relLightOffset + 2));
                uint bitMask =  LoadRelLightIndex(input.relLightOffset + 3)
                             | (LoadRelLightIndex(input.relLightOffset + 4) << 16);

                #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                float d = 1.0 - g_DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #else
                float d = g_DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #endif

                // View space depth (signed).
                float z = dot(g_unproject0, float4(0, 0, d, 1)) / dot(g_unproject1, float4(0, 0, d, 1));

                float4 wsPos = mul(_InvCameraViewProj, float4(input.clipCoord, d * 2.0 - 1.0, 1.0));
                wsPos.xyz *= 1.0 / wsPos.w;

                float3 color = 0.0.xxx;

                [branch] if (IsWithinRange(-z, minDepth, maxDepth, bitMask))
                {
                    for (int li = 0; li < lightCount; ++li)
                    {
                        int offsetInList = input.relLightOffset + 5 + li;
                        uint relLightIndex = LoadRelLightIndex(offsetInList);
                        PointLightData light = LoadPointLightData(relLightIndex);

                        // TODO calculate lighting.
                        float3 L = light.WsPos - wsPos;
                        half att = dot(L, L) < light.Radius*light.Radius ? 1.0 : 0.0;

                        color += light.Color.rgb * att * 0.1;
                    }
                }

                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
