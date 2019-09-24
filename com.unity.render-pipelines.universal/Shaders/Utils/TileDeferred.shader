Shader "Hidden/Universal Render Pipeline/TileDeferred"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

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
            //#pragma enable_d3d11_debug_symbols

            struct TileData
            {
                uint tileID;                 // 2 ushorts
                uint listBitMask;            // 1 uint
                uint relLightOffsetAndCount; // 2 ushorts
                uint unused;
            };

            #if USE_CBUFFER_FOR_TILELIST
                CBUFFER_START(UTileList)
                uint4 _TileList[MAX_TILES_PER_CBUFFER_PATCH * SIZEOF_VEC4_TILEDATA];
                CBUFFER_END

                TileData LoadTileData(int i)
                {
                    i *= SIZEOF_VEC4_TILEDATA;
                    TileData tileData;
                    tileData.tileID                 = _TileList[i][0];
                    tileData.listBitMask            = _TileList[i][1];
                    tileData.relLightOffsetAndCount = _TileList[i][2];
                    return tileData;
                }

            #else
                StructuredBuffer<TileData> _TileList;

                TileData LoadTileData(int i) { return _TileList[i]; }

            #endif

            // Keep in sync with PackTileID().
            uint2 UnpackTileID(uint tileID)
            {
                return uint2(tileID & 0xFFFF, (tileID >> 16) & 0xFFFF);
            }

            uint _TilePixelWidth;
            uint _TilePixelHeight;
            uint _InstanceOffset;

            Texture2D<uint> _TileDepthRangeTexture;

            struct Attributes
            {
                uint vertexID   : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation int2 relLightOffsetAndCount : TEXCOORD0;
                noperspective float2 clipCoord : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                uint instanceID = _InstanceOffset + input.instanceID;

                TileData tileData = LoadTileData(instanceID);
                uint2 tileCoord = UnpackTileID(tileData.tileID);

                uint geoDepthBitmask = _TileDepthRangeTexture.Load(int3(tileCoord, 0)).x;
                bool shouldDiscard = (geoDepthBitmask & tileData.listBitMask) == 0;

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
                float2 pp = GetQuadVertexPosition(quadIndex).xy;
                uint2 pixelCoord  = tileCoord * uint2(_TilePixelWidth, _TilePixelHeight);
                pixelCoord += uint2(pp.xy * uint2(_TilePixelWidth, _TilePixelHeight));
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

                output.relLightOffsetAndCount.x = tileData.relLightOffsetAndCount & 0xFFFF;
                output.relLightOffsetAndCount.y = (tileData.relLightOffsetAndCount >> 16) & 0xFFFF;

                return output;
            }

            #if USE_CBUFFER_FOR_LIGHTDATA
                CBUFFER_START(UPointLightBuffer)
                // Unity does not support structure inside cbuffer unless for instancing case (not safe to use here).
                uint4 _PointLightBuffer[MAX_POINTLIGHT_PER_CBUFFER_BATCH * SIZEOF_VEC4_POINTLIGHTDATA];
                CBUFFER_END

                PointLightData LoadPointLightData(int relLightIndex)
                {
                    uint i = relLightIndex * SIZEOF_VEC4_POINTLIGHTDATA;
                    PointLightData pl;
                    pl.wsPos  = asfloat(_PointLightBuffer[i + 0].xyz);
                    pl.radius = asfloat(_PointLightBuffer[i + 0].w);
                    pl.color.rgb = asfloat(_PointLightBuffer[i + 1].rgb);
                    return pl;
                }

            #else
                StructuredBuffer<PointLightData> _PointLightBuffer;

                PointLightData LoadPointLightData(int relLightIndex) { return _PointLightBuffer[relLightIndex]; }

            #endif

            #if USE_CBUFFER_FOR_LIGHTLIST
                CBUFFER_START(URelLightList)
                uint4 _RelLightList[MAX_REL_LIGHT_INDICES_PER_CBUFFER_BATCH/4];
                CBUFFER_END

                uint LoadRelLightIndex(uint i) { return _RelLightList[i >> 2][i & 3]; }

            #else
                StructuredBuffer<uint> _RelLightList;

                uint LoadRelLightIndex(uint i) { return _RelLightList[i]; }

            #endif

            Texture2D _DepthTex;
            Texture2D _GBuffer0;
            Texture2D _GBuffer1;
            Texture2D _GBuffer2;
            Texture2D _GBuffer3;
            Texture2D _GBuffer4;

            half4 PointLightShading(Varyings input) : SV_Target
            {
                int relLightOffset = input.relLightOffsetAndCount.x;
                int lightCount = input.relLightOffsetAndCount.y;

                #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                float d = 1.0 - _DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #else
                float d = _DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #endif

                // Temporary code to calculate fragment world space position.
                float4 wsPos = mul(_InvCameraViewProj, float4(input.clipCoord, d * 2.0 - 1.0, 1.0));
                wsPos.xyz *= 1.0 / wsPos.w;

#if TEST_WIP_DEFERRED_POINT_LIGHTING
                half4 gbuffer0 = _GBuffer0.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer1 = _GBuffer1.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer2 = _GBuffer2.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer3 = _GBuffer3.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer4 = _GBuffer4.Load(int3(input.positionCS.xy, 0));

                SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2, gbuffer3);
                InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, gbuffer4, wsPos.xyz);
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
#else
                float4 albedoOcc = _GBuffer0.Load(int3(input.positionCS.xy, 0));
                float4 normalRoughness = _GBuffer1.Load(int3(input.positionCS.xy, 0));
                float4 spec = _GBuffer2.Load(int3(input.positionCS.xy, 0));
#endif

                half3 color = 0.0.xxx;

#if TEST_WIP_DEFERRED_POINT_LIGHTING
                // TODO re-use _GBuffer4 as base RT instead?
                color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
#endif
                [loop] for (int li = 0; li < lightCount; ++li)
                {
                    uint offsetInList = relLightOffset + li;
                    uint relLightIndex = LoadRelLightIndex(offsetInList);
                    PointLightData light = LoadPointLightData(relLightIndex);

#if TEST_WIP_DEFERRED_POINT_LIGHTING
                    Light unityLight = UnityLightFromPointLightDataAndWorldSpacePosition(light, wsPos.xyz);
                    color += LightingPhysicallyBased(brdfData, unityLight, inputData.normalWS, inputData.viewDirectionWS);
#else
                    // TODO calculate lighting.
                    float3 L = light.wsPos - wsPos.xyz;
                    half att = dot(L, L) < light.radius*light.radius ? 1.0 : 0.0;

                    color += light.color.rgb * att * 0.1; // + (albedoOcc.rgb + normalRoughness.rgb + spec.rgb) * 0.001 + half3(albedoOcc.a, normalRoughness.a, spec.a) * 0.01;
#endif
                }

                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
