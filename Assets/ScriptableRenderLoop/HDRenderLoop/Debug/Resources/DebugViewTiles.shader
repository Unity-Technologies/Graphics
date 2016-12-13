Shader "Hidden/HDRenderLoop/DebugViewTiles"
{
    SubShader
    {

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #define LIGHTLOOP_TILE_PASS 1            
            #define LIGHTLOOP_TILE_ALL	1

            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Common.hlsl"

            // Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
            // deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem), 
            // the deferred shader will require to use multicompile.
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/ShaderConfig.cs.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/ShaderVariables.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Lighting/Lighting.hlsl" // This include Material.hlsl

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER2D(sampler_CameraDepthTexture);

            float4x4 _InvViewProjMatrix;

            uint _ViewTilesFlags;
            float2 _MousePixelCoord;


            float4 Vert(float3 positionOS : POSITION): SV_POSITION
            {
                return TransformWorldToHClip(TransformObjectToWorld(positionOS));
            }

            float4 AlphaBlend(float4 c0, float4 c1)	// c1 over c0
            {
                return float4(lerp(c0.rgb, c1.rgb, c1.a), c0.a + c1.a - c0.a * c1.a);
            }

            float4 OverlayHeatMap(uint2 pixCoord, uint numLights)
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

                int colorIndex = numLights == 0 ? 0 : (1 + (int)floor(10 * (log2((float)numLights) / log2(maxNrLightsPerTile))));
                colorIndex = colorIndex < 0 ? 0 : colorIndex;
                float4 col = colorIndex > 11 ? float4(1.0, 1.0, 1.0, 1.0) : kRadarColors[colorIndex];

                int2 coord = pixCoord - int2(1, 1);

                float4 color = float4(pow(col.xyz, 2.2), 0.3*col.w);
                if (numLights > 0)
                {
                    if (SampleDebugFontNumber(coord, numLights))		// Shadow
                        color = float4(0, 0, 0, 1);
                    if (SampleDebugFontNumber(coord + 1, numLights))	// Text
                        color = float4(1, 1, 1, 1);
                }
                return color;
            }

            float4 Frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                Coordinate coord = GetCoordinate(positionCS.xy, _ScreenSize.zw);
                
                int2 pixelCoord = coord.unPositionSS.xy;
                int2 tileCoord = pixelCoord / TILE_SIZE;
                int2 mouseTileCoord = _MousePixelCoord / TILE_SIZE;
                int2 offsetInTile = pixelCoord - tileCoord * TILE_SIZE;
                
#ifdef USE_CLUSTERED_LIGHTLIST
                // Perform same calculation than in deferred.shader
                float depth = LOAD_TEXTURE2D(_CameraDepthTexture, coord.unPositionSS).x;
                float3 positionWS = UnprojectToWorld(depth, coord.positionSS, _InvViewProjMatrix);
                float linearDepth = TransformWorldToView(positionWS).z; // View space linear depth
#else
                float linearDepth = 0.0; // unused
#endif

                int n = 0;
                for (int category = 0; category < LIGHTCATEGORY_COUNT; category++)
                {
                    uint mask = 1u << category;
                    if (mask & _ViewTilesFlags)
                    {
                        uint start;
                        uint count;
                        GetCountAndStart(coord, category, linearDepth, start, count);
                        n += count;
                    }
                }
                
                float4 result = 0.0f;

				// Tile overlap counter
                if (n > 0)
                {
                    result = OverlayHeatMap(int2(coord.unPositionSS.xy) & (TILE_SIZE - 1), n);
                }

				// Highlight selected tile
                if(all(mouseTileCoord == tileCoord))
                {
                    bool border = any(offsetInTile == 0 || offsetInTile == TILE_SIZE - 1);
                    float4 result2 = float4(1, 1, 1, border ? 1.0f : 0.5);
                    result = AlphaBlend(result, result2);
                }

                // Print light lists for selected tile at the bottom of the screen
                int maxLights = 32;
                if(tileCoord.y < LIGHTCATEGORY_COUNT && tileCoord.x < maxLights + 3)
                {
                    Coordinate mouseCoord = GetCoordinate(_MousePixelCoord, _ScreenSize.zw);

                    int category = (LIGHTCATEGORY_COUNT - 1) - tileCoord.y;
                    int start;
                    int count;
                    GetCountAndStart(mouseCoord, category, linearDepth, start, count);

                    float4 result2 = float4(.1,.1,.1,.9);
                    int2 fontCoord = int2(pixelCoord.x, offsetInTile.y);
                    int lightListIndex = tileCoord.x - 2;

                    int n = -1;
                    if(tileCoord.x == 0)
                    {
                        n = count;
                    }
                    else if(lightListIndex >= 0 && lightListIndex < count)
                    {
                        n = FetchIndex(start, lightListIndex);
                    }

                    if(n >= 0)
                    {
                        if(SampleDebugFontNumber(offsetInTile, n))
                            result2 = float4(0, 0, 0, 1);
                        if(SampleDebugFontNumber(offsetInTile + 1, n))
                            result2 = float4(1, 1, 1, 1);
                    }

                    result = AlphaBlend(result, result2);
                }

                return result;
            }

            ENDHLSL
        }
    }
    Fallback Off
}