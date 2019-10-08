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

            Texture2D<uint> _TileDepthInfoTexture;

            struct Attributes
            {
                uint vertexID   : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation int2 relLightOffsets : TEXCOORD0;
                noperspective float2 clipCoord : TEXCOORD1;
            };

            #if USE_CBUFFER_FOR_LIGHTLIST
                CBUFFER_START(URelLightList)
                uint4 _RelLightList[MAX_REL_LIGHT_INDICES_PER_CBUFFER_BATCH/4];
                CBUFFER_END

                uint LoadRelLightIndex(uint i) { return _RelLightList[i >> 2][i & 3]; }

            #else
                StructuredBuffer<uint> _RelLightList;

                uint LoadRelLightIndex(uint i) { return _RelLightList[i]; }

            #endif

            Varyings Vertex(Attributes input)
            {
                uint instanceID = _InstanceOffset + input.instanceID;
                TileData tileData = LoadTileData(instanceID);
                uint2 tileCoord = UnpackTileID(tileData.tileID);
                uint geoDepthBitmask = _TileDepthInfoTexture.Load(int3(tileCoord, 0)).x;
                bool shouldDiscard = (geoDepthBitmask & tileData.listBitMask) == 0;

                Varyings output;

                [branch] if (shouldDiscard)
                {
                    output.positionCS = float4(-2, -2, -2, 1);
                    output.clipCoord = 0.0;
                    output.relLightOffsets = 0;
                    return output;
                }

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
                float2 pp = GetQuadVertexPosition(quadIndex).xy;
                uint2 pixelCoord  = tileCoord * uint2(_TilePixelWidth, _TilePixelHeight);
                pixelCoord += uint2(pp.xy * uint2(_TilePixelWidth, _TilePixelHeight));
                float2 clipCoord = (pixelCoord * _ScreenSize.zw) * 2.0 - 1.0;

                output.positionCS = float4(clipCoord, 0, 1);
//              Screen is already y flipped (different from HDRP)?
//                // Tiles coordinates always start at upper-left corner of the screen (y axis down).
//                // Clip-space coordinatea always have y axis up. Hence, we must always flip y.
//                output.positionCS.y *= -1.0;

                output.clipCoord = clipCoord;
                // Screen is flipped!!!!!!
                output.clipCoord.y *= -1.0;

                // flat interpolators are calculated by the provoking vertex of the triangles or quad.
                // Provoking vertex convention is different per platform.
                #if defined(SHADER_API_METAL)
                [branch] if (input.vertexID == 0 || input.vertexID == 1)
                #elif SHADER_API_SWITCH
				[branch] if (input.vertexID == 3)
                #else
                [branch] if (input.vertexID == 0 || input.vertexID == 3)
                #endif
                {
                    int relLightOffset = tileData.relLightOffsetAndCount & 0xFFFF;
                    int relLightOffsetEnd = relLightOffset + (tileData.relLightOffsetAndCount >> 16);

                    // Trim beginning of the light list.
                    [loop] for (; relLightOffset < relLightOffsetEnd; ++relLightOffset)
                    {
                        uint lightIndexAndRange = LoadRelLightIndex(relLightOffset);
                        uint firstBit = (lightIndexAndRange >> 16) & 0xFF;
                        uint bitCount = lightIndexAndRange >> 24;
                        uint lightBitmask = (0xFFFFFFFF >> (32 - bitCount)) << firstBit;

                        [branch] if ((geoDepthBitmask & lightBitmask) != 0)
                            break;
                    }

                    // Trim end of the light list.
                    [loop] for (; relLightOffsetEnd >= relLightOffset; --relLightOffsetEnd)
                    {
                        uint lightIndexAndRange = LoadRelLightIndex(relLightOffsetEnd - 1);
                        uint firstBit = (lightIndexAndRange >> 16) & 0xFF;
                        uint bitCount = lightIndexAndRange >> 24;
                        uint lightBitmask = (0xFFFFFFFF >> (32 - bitCount)) << firstBit;

                        [branch] if ((geoDepthBitmask & lightBitmask) != 0)
                            break;
                    }

                    output.relLightOffsets.x = relLightOffset;
                    output.relLightOffsets.y = relLightOffsetEnd;
                }
                else
                {
                    output.relLightOffsets.x = 0;
                    output.relLightOffsets.y = 0;
                }

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
                    pl.radius2 = asfloat(_PointLightBuffer[i + 0].w);
                    pl.color.rgb = asfloat(_PointLightBuffer[i + 1].rgb);
                    pl.attenuation.xyzw = asfloat(_PointLightBuffer[i + 2].xyzw);
                    pl.spotDirection.xyz = asfloat(_PointLightBuffer[i + 3].xyz);
                    // pl.padding0 = asfloat(_PointLightBuffer[i + 3].w); // TODO use for something?

                    return pl;
                }

            #else
                StructuredBuffer<PointLightData> _PointLightBuffer;

                PointLightData LoadPointLightData(int relLightIndex) { return _PointLightBuffer[relLightIndex]; }

            #endif

            Texture2D _DepthTex;
            Texture2D _GBuffer0;
            Texture2D _GBuffer1;
            Texture2D _GBuffer2;

            half4 PointLightShading(Varyings input) : SV_Target
            {
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

                SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
                InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, wsPos.xyz);
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
#else
                float4 albedoOcc = _GBuffer0.Load(int3(input.positionCS.xy, 0));
                float4 normalRoughness = _GBuffer1.Load(int3(input.positionCS.xy, 0));
                float4 spec = _GBuffer2.Load(int3(input.positionCS.xy, 0));
#endif
                half3 color = 0.0.xxx;

                //[loop] for (int li = input.relLightOffsets.x; li < input.relLightOffsets.y; ++li)
                int li = input.relLightOffsets.x;
                [loop] do
                {
                    uint relLightIndex = LoadRelLightIndex(li) & 0xFFFF;
                    PointLightData light = LoadPointLightData(relLightIndex);

#if TEST_WIP_DEFERRED_POINT_LIGHTING
                    float3 L = light.wsPos - wsPos.xyz;
                    [branch] if (dot(L, L) < light.radius2)
                    {
                        Light unityLight = UnityLightFromPointLightDataAndWorldSpacePosition(light, wsPos.xyz);
                        color += LightingPhysicallyBased(brdfData, unityLight, inputData.normalWS, inputData.viewDirectionWS);
                    }
#else
                    // TODO calculate lighting.
                    float3 L = light.wsPos - wsPos.xyz;
                    half att = dot(L, L) < light.radius2 ? 1.0 : 0.0;

                    color += light.color.rgb * att * 0.1; // + (albedoOcc.rgb + normalRoughness.rgb + spec.rgb) * 0.001 + half3(albedoOcc.a, normalRoughness.a, spec.a) * 0.01;
#endif
                }
                while(++li < input.relLightOffsets.y);

                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
