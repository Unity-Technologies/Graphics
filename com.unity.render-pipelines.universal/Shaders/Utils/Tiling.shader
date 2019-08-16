Shader "Hidden/Universal Render Pipeline/Tiling"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // Keep in sync with PackTileID().
    uint2 UnpackTileID(uint tileID)
    {
        return uint2(tileID & 0xFFFF, (tileID >> 16) & 0xFFFF);
    }

    ENDHLSL

    SubShader
    {
//        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

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

            StructuredBuffer<uint> g_TileIDBuffer;
            StructuredBuffer<uint> g_TileRelLightBuffer;

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
                const uint tilePixelWidth = 16;
                const uint tilePixelHeight = 16;

                uint  tileID = g_TileIDBuffer[input.instanceID];
                uint2 tileCoord = UnpackTileID(tileID);
                uint2 pixelCoord  = tileCoord * uint2(g_TilePixelWidth, g_TilePixelHeight);

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
                float2 pp = GetQuadVertexPosition(quadIndex).xy;
                pixelCoord += uint2(pp.xy * uint2(tilePixelWidth, tilePixelHeight));

                Varyings output;
                output.positionCS = float4((pixelCoord * _ScreenSize.zw) * 2.0 - 1.0, 0, 1);
//              Screen is already y flipped (different from HDRP)?
//                // Tiles coordinates always start at upper-left corner of the screen (y axis down).
//                // Clip-space coordinatea always have y axis up. Hence, we must always flip y.
//                output.positionCS.y *= -1.0;

                output.relLightOffset = g_TileRelLightBuffer[input.instanceID];

                return output;
            }

            struct PointLightData
            {
                float3 WsPos;
                float Radius;
                float4 Color;
            };

            StructuredBuffer<PointLightData> g_PointLightBuffer;
            StructuredBuffer<uint> g_RelLightIndexBuffer;

            half4 PointLightShading(Varyings input) : SV_Target
            {
                uint lightCount = g_RelLightIndexBuffer[input.relLightOffset];

                float3 color = 0.0.xxx;

                for (int li = 0; li < lightCount; ++li)
                {
                    uint relLightIndex = g_RelLightIndexBuffer[input.relLightOffset + 1 + li];
                    PointLightData light = g_PointLightBuffer[relLightIndex];

                    // TODO calculate lighting.

                    color += light.Color.rgb * 0.05;
                }

                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
