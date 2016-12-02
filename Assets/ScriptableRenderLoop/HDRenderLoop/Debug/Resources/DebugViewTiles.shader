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

	        uint _ViewTilesFlags;

	        TEXTURE2D(_CameraDepthTexture);
	        SAMPLER2D(sampler_CameraDepthTexture);

	        float4 Vert(float3 positionOS : POSITION): SV_POSITION
	        {
		        return TransformWorldToHClip(TransformObjectToWorld(positionOS));
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

                #ifdef USE_CLUSTERED_LIGHTLIST
                float depth = LOAD_TEXTURE2D(_CameraDepthTexture, coord.unPositionSS).x;
                float linearDepth = GetLinearDepth(depth); // View space linear depth
                #else
                float linearDepth = 0.0; // unused
                #endif

		        int n = 0;
		        if (_ViewTilesFlags & DEBUGVIEWTILESFLAGS_DIRECT_LIGHTING)
		        {
			        uint punctualLightStart;
			        uint punctualLightCount;
			        GetCountAndStart(coord, DIRECT_LIGHT, linearDepth, punctualLightStart, punctualLightCount);
			        n += punctualLightCount;
		        }

		        if (_ViewTilesFlags & DEBUGVIEWTILESFLAGS_REFLECTION)
		        {
			        uint envLightStart;
			        uint envLightCount;
			        GetCountAndStart(coord, REFLECTION_LIGHT, linearDepth, envLightStart, envLightCount);
			        n += envLightCount;
		        }

		        if (n > 0)
		        {
			        return OverlayHeatMap(int2(coord.unPositionSS.xy) & 15, n);
		        }
		        else
		        {
			        return 0.0;
		        }
	        }

		    ENDHLSL
	    }
	}
	Fallback Off
}
