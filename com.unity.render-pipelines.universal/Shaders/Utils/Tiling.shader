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

            struct Attributes
            {
                uint vertexID   : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation uint relLightOffset : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                uint  tileID = g_TileIDBuffer[input.instanceID >> 2][input.instanceID & 3];
                uint2 tileCoord = UnpackTileID(tileID);
                uint2 pixelCoord  = tileCoord * uint2(g_TilePixelWidth, g_TilePixelHeight);

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
                float2 pp = GetQuadVertexPosition(quadIndex).xy;
                pixelCoord += uint2(pp.xy * uint2(g_TilePixelWidth, g_TilePixelHeight));

                Varyings output;
                output.positionCS = float4((pixelCoord * _ScreenSize.zw) * 2.0 - 1.0, 0, 1);
//              Screen is already y flipped (different from HDRP)?
//                // Tiles coordinates always start at upper-left corner of the screen (y axis down).
//                // Clip-space coordinatea always have y axis up. Hence, we must always flip y.
//                output.positionCS.y *= -1.0;

                output.relLightOffset = g_TileRelLightBuffer[input.instanceID >> 2][input.instanceID & 3];

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

            PointLightData LoadPointLightData(int relLightIndex)
            {
                PointLightData pl;
                pl.WsPos = g_PointLightBuffer[relLightIndex].xyz;
                pl.Radius = g_PointLightBuffer[relLightIndex].w;
                pl.Color = g_PointLightBuffer[MAX_POINTLIGHT_PER_BATCH + relLightIndex];
                return pl;
            }

            half4 PointLightShading(Varyings input) : SV_Target
            {
                uint lightCount = g_RelLightIndexBuffer[input.relLightOffset >> 2][input.relLightOffset & 3];

                float3 color = 0.0.xxx;

                for (int li = 0; li < lightCount; ++li)
                {
                    int offsetInList = input.relLightOffset + 1 + li;
                    uint relLightIndex = g_RelLightIndexBuffer[offsetInList >> 2][offsetInList & 3];
                    PointLightData light = LoadPointLightData(relLightIndex);

                    // TODO calculate lighting.

                    color += light.Color.rgb * 0.06;
                }

                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
