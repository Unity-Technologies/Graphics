Shader "Hidden/HDRP/DebugViewTiles"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
            #pragma multi_compile SHOW_LIGHT_CATEGORIES SHOW_FEATURE_VARIANTS
            #pragma multi_compile _ IS_DRAWPROCEDURALINDIRECT
            #pragma multi_compile _ DISABLE_TILE_MODE

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

            #define DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            // Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
            // deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem),
            // the deferred shader will require to use multicompile.
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

#if (SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS) && defined(USE_CLUSTERED_LIGHTLIST)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeLightLoopDef.hlsl"
#endif
            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            uint _ViewTilesFlags;
            uint _NumTiles;
            float _ClusterDebugDistance;
            int _ClusterDebugMode;

            StructuredBuffer<uint> g_TileList;
            Buffer<uint> g_DispatchIndirectBuffer;

            float GetTileDepth(uint2 coord)
            {
                float depth = 0.0;

                if (_ClusterDebugMode == CLUSTERDEBUGMODE_VISUALIZE_OPAQUE)
                {
                    depth = LoadCameraDepth(coord.xy);
                }
                else
                {
                    float4 temp = mul(UNITY_MATRIX_P, float4(0.0, 0.0, _ClusterDebugDistance, 1.0));
                    depth = temp.z / temp.w;
                }

                return depth;
            }

            uint GetDispatchIndirectCount(uint variant)
            {
#if IS_DRAWPROCEDURALINDIRECT
                return g_DispatchIndirectBuffer[variant * 4 + 1];
#else
                return g_DispatchIndirectBuffer[variant * 3 + 0] / 4; // 4 8x8 groups per tile
#endif
            }

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                int     variant     : TEXCOORD0;
                float2  texcoord    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

#if SHOW_FEATURE_VARIANTS
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                uint quadIndex = input.vertexID / 6;
                uint quadVertex = input.vertexID - quadIndex * 6;
                quadVertex = (0x312210 >> (quadVertex<<2)) & 3; //remap [0,5]->[0,3]

                uint2 tileSize = GetTileSize();

                uint variant = 0;
                while (quadIndex >= GetDispatchIndirectCount(variant) && variant < NUM_FEATURE_VARIANTS) // 4 group 8x8 per tile.
                {
                    quadIndex -= GetDispatchIndirectCount(variant);
                    variant++;
                }

                uint tileIndex = g_TileList[variant * _NumTiles + quadIndex];
                uint2 tileCoord = uint2((tileIndex >> TILE_INDEX_SHIFT_X) & TILE_INDEX_MASK, (tileIndex >> TILE_INDEX_SHIFT_Y) & TILE_INDEX_MASK); // see builddispatchindirect.compute
                uint2 pixelCoord = (tileCoord + uint2((quadVertex+1) & 1, (quadVertex >> 1) & 1)) * tileSize;

#if defined(UNITY_STEREO_INSTANCING_ENABLED)
                // With XR single-pass, all tiles from the indirect buffer are processed so we need to discard them if they don't match the current eye index
                uint tile_StereoEyeIndex = tileIndex >> TILE_INDEX_SHIFT_EYE;
                if (unity_StereoEyeIndex != tile_StereoEyeIndex)
                    variant = -1;
#endif

                float2 clipCoord = (pixelCoord * _ScreenSize.zw) * 2.0 - 1.0;
                clipCoord.y *= -1;

                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = float4(clipCoord, 0, 1.0);
                output.variant = variant;

                output.texcoord = clipCoord * 0.5 + 0.5;
                output.texcoord.y = 1.0 - output.texcoord.y;
                return output;
            }
#else
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
                output.variant = 0; // unused
                return output;
            }
#endif

            float4 AlphaBlend(float4 c0, float4 c1) // c1 over c0
            {
                return float4(lerp(c0.rgb, c1.rgb, c1.a), c0.a + c1.a - c0.a * c1.a);
            }

            float4 OverlayHeatMap(uint2 pixCoord, uint n)
            {
                const float4 kRadarColors[12] =
                {
                    float4(0.0, 0.0, 0.0, 0.0),   // black
                    float4(0.0, 0.0, 0.6, 0.5),   // dark blue
                    float4(0.0, 0.0, 0.9, 0.5),   // blue
                    float4(0.0, 0.6, 0.9, 0.5),   // light blue
                    float4(0.0, 0.9, 0.9, 0.5),   // cyan
                    float4(0.0, 0.9, 0.6, 0.5),   // blueish green
                    float4(0.0, 0.9, 0.0, 0.5),   // green
                    float4(0.6, 0.9, 0.0, 0.5),   // yellowish green
                    float4(0.9, 0.9, 0.0, 0.5),   // yellow
                    float4(0.9, 0.6, 0.0, 0.5),   // orange
                    float4(0.9, 0.0, 0.0, 0.5),   // red
                    float4(1.0, 0.0, 0.0, 0.9)    // strong red
                };

                float maxNrLightsPerTile = 31; // TODO: setup a constant for that

                int colorIndex = n == 0 ? 0 : (1 + (int)floor(10 * (log2((float)n) / log2(maxNrLightsPerTile))));
                colorIndex = colorIndex < 0 ? 0 : colorIndex;
                float4 col = colorIndex > 11 ? float4(1.0, 1.0, 1.0, 1.0) : kRadarColors[colorIndex];

                int2 coord = pixCoord - int2(1, 1);

                float4 color = float4(PositivePow(col.xyz, 2.2), 0.3 * col.w);
                if (n >= 0)
                {
                    if (SampleDebugFontNumber(coord, n))        // Shadow
                        color = float4(0, 0, 0, 1);
                    if (SampleDebugFontNumber(coord + 1, n))    // Text
                        color = float4(1, 1, 1, 1);
                }
                return color;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // For debug shaders, Viewport can be at a non zero (x,y) but the pipeline render targets all starts at (0,0)
                // input.positionCS in in pixel coordinate relative to the render target origin so they will be offsted compared to internal render textures
                // To solve that, we compute pixel coordinates from full screen quad texture coordinates which start correctly at (0,0)
                uint2 pixelCoord = uint2(input.texcoord.xy * _ScreenSize.xy);

                float depth = GetTileDepth(pixelCoord);

                PositionInputs posInput = GetPositionInput(pixelCoord.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, pixelCoord / GetTileSize());

                int2 tileCoord = (float2)pixelCoord / GetTileSize();
                int2 mouseTileCoord = _MousePixelCoord.xy / GetTileSize();
                int2 offsetInTile = pixelCoord - tileCoord * GetTileSize();

                int n = 0;
#if defined(SHOW_LIGHT_CATEGORIES) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER)
                for (int category = 0; category < LIGHTCATEGORY_COUNT; category++)
                {
                    uint mask = 1u << category;
                    if (mask & _ViewTilesFlags)
                    {
                    #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
                        if (category == LIGHTCATEGORY_PROBE_VOLUME)
                        {
                        #if defined(USE_CLUSTERED_LIGHTLIST)
                            // If evaluating probe volumes during material pass, their data is only avaibile in clustered.
                            // To accurately reflect this, if a user has selected to view the count inside of the tiled list,
                            // the count should be zero.
                            uint start;
                            uint count;
                            ProbeVolumeGetCountAndStart(posInput, start, count);
                            n += count;
                        #endif
                        }
                        else
                    #endif
                        {
                            uint start;
                            uint count;
                            GetCountAndStart(posInput, category, start, count);
                            n += count;
                        }
                    }
                }
                if (n == 0)
                    n = -1;
#else
                n = input.variant;
#endif

                float4 result = float4(0.0, 0.0, 0.0, 0.0);

#ifdef DISABLE_TILE_MODE
                // Tile debug mode is not supported in MSAA (only cluster)
                int maxLights = 32;
                const int textSize = 23;
                const int text[textSize] = {'N', 'o', 't', ' ', 's', 'u', 'p', 'p', 'o', 'r', 't', 'e', 'd', ' ', 'w', 'i', 't', 'h', ' ', 'M', 'S', 'A', 'A'};
                if (input.positionCS.y < DEBUG_FONT_TEXT_HEIGHT)
                {
                    float4 result2 = float4(.1,.1,.1,.9);

                    uint2 unormCoord = input.positionCS.xy;
                    float3 textColor = float3(0.5f, 0.5f, 0.5f);
                    uint2 textLocation = uint2(0, 0);
                    for (int i = 0; i < textSize; i++)
                        DrawCharacter(text[i], textColor, unormCoord, textLocation, result2.rgb, 1, text[i] >= 97 ? 7 : 10);

                    result = AlphaBlend(result, result2);
                }
#else
                // Tile overlap counter
                if (n >= 0)
                {
                    result = OverlayHeatMap(int2(posInput.positionSS.xy) & (GetTileSize() - 1), n);
                }

#if defined(SHOW_LIGHT_CATEGORIES) && !defined(LIGHTLOOP_DISABLE_TILE_AND_CLUSTER)
                // Highlight selected tile
                if (all(mouseTileCoord == tileCoord))
                {
                    bool border = any(offsetInTile == 0 || offsetInTile == (int)GetTileSize() - 1);
                    float4 result2 = float4(1.0, 1.0, 1.0, border ? 1.0 : 0.5);
                    result = AlphaBlend(result, result2);
                }

                // Print light lists for selected tile at the bottom of the screen
                int maxLights = 32;
                if (tileCoord.y < LIGHTCATEGORY_COUNT && tileCoord.x < maxLights + 3)
                {
                    float depthMouse = GetTileDepth(_MousePixelCoord.xy);

                    PositionInputs mousePosInput = GetPositionInput(_MousePixelCoord.xy, _ScreenSize.zw, depthMouse, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, mouseTileCoord);

                    uint category = (LIGHTCATEGORY_COUNT - 1) - tileCoord.y;
                    uint start;
                    uint count;

                #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
                    if (category == LIGHTCATEGORY_PROBE_VOLUME)
                    {
                    #if defined(USE_CLUSTERED_LIGHTLIST)
                        ProbeVolumeGetCountAndStart(mousePosInput, start, count);
                        n += count;
                    #endif
                    }
                    else
                #endif
                    {
                        GetCountAndStart(mousePosInput, category, start, count);
                    }

                    float4 result2 = float4(.1,.1,.1,.9);
                    int2 fontCoord = int2(pixelCoord.x, offsetInTile.y);
                    int lightListIndex = tileCoord.x - 2;

                    int n = -1;
                    if(tileCoord.x == 0)
                    {
                        n = (int)count;
                    }
                    else if(lightListIndex >= 0 && lightListIndex < (int)count)
                    {
                    #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
                        if (category == LIGHTCATEGORY_PROBE_VOLUME)
                        {
                        #if defined(USE_CLUSTERED_LIGHTLIST)
                            n = ProbeVolumeFetchIndex(start, lightListIndex);
                        #endif
                        }
                        else
                    #endif
                        {
                            n = FetchIndex(start, lightListIndex);
                        }
                    }

                    if (n >= 0)
                    {
                        if (SampleDebugFontNumber(offsetInTile, n))
                            result2 = float4(0.0, 0.0, 0.0, 1.0);
                        if (SampleDebugFontNumber(offsetInTile + 1, n))
                            result2 = float4(1.0, 1.0, 1.0, 1.0);
                    }

                    result = AlphaBlend(result, result2);
                }
#endif
#endif

                return result;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
