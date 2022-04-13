Shader "Hidden/HDRP/DeferredTile"
{
    Properties
    {
        [HideInInspector] _StencilMask("_StencilMask", Int) = 6 // StencilUsage.RequiresDeferredLighting | StencilUsage.SubsurfaceScattering
        [HideInInspector] _StencilRef("_StencilRef", Int) = 0
        [HideInInspector] _StencilCmp("_StencilCmp", Int) = 3
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Pass 0 - Shader Variants (tiling)"

            // Stencil is used to skip background/sky pixels.
            Stencil
            {
                ReadMask[_StencilMask]
                Ref  [_StencilRef]
                Comp [_StencilCmp]
                Pass Keep
            }

            ZWrite Off
            ZTest  Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fragment _ OUTPUT_SPLIT_LIGHTING // Split lighting is utilized during the SSS pass.
            #pragma multi_compile_fragment  _ SHADOWS_SHADOWMASK /// Variant with and without shadowmask
            #pragma multi_compile_local_fragment VARIANT0 VARIANT1 VARIANT2 VARIANT3 VARIANT4 VARIANT5 VARIANT6 VARIANT7 VARIANT8 VARIANT9 VARIANT10 VARIANT11 VARIANT12 VARIANT13 VARIANT14 VARIANT15 VARIANT16 VARIANT17 VARIANT18 VARIANT19 VARIANT20 VARIANT21 VARIANT22 VARIANT23 VARIANT24 VARIANT25 VARIANT26 VARIANT27 VARIANT28
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH

            #define USE_INDIRECT    // otherwise TileVariantToFeatureFlags() will not be defined in Lit.hlsl!!!

            #define USE_FPTL_LIGHTLIST 1 // deferred opaque always use FPTL

            #ifdef VARIANT0
            #define VARIANT 0
            #endif
            #ifdef VARIANT1
            #define VARIANT 1
            #endif
            #ifdef VARIANT2
            #define VARIANT 2
            #endif
            #ifdef VARIANT3
            #define VARIANT 3
            #endif
            #ifdef VARIANT4
            #define VARIANT 4
            #endif
            #ifdef VARIANT5
            #define VARIANT 5
            #endif
            #ifdef VARIANT6
            #define VARIANT 6
            #endif
            #ifdef VARIANT7
            #define VARIANT 7
            #endif
            #ifdef VARIANT8
            #define VARIANT 8
            #endif
            #ifdef VARIANT9
            #define VARIANT 9
            #endif
            #ifdef VARIANT10
            #define VARIANT 10
            #endif
            #ifdef VARIANT11
            #define VARIANT 11
            #endif
            #ifdef VARIANT12
            #define VARIANT 12
            #endif
            #ifdef VARIANT13
            #define VARIANT 13
            #endif
            #ifdef VARIANT14
            #define VARIANT 14
            #endif
            #ifdef VARIANT15
            #define VARIANT 15
            #endif
            #ifdef VARIANT16
            #define VARIANT 16
            #endif
            #ifdef VARIANT17
            #define VARIANT 17
            #endif
            #ifdef VARIANT18
            #define VARIANT 18
            #endif
            #ifdef VARIANT19
            #define VARIANT 19
            #endif
            #ifdef VARIANT20
            #define VARIANT 20
            #endif
            #ifdef VARIANT21
            #define VARIANT 21
            #endif
            #ifdef VARIANT22
            #define VARIANT 22
            #endif
            #ifdef VARIANT23
            #define VARIANT 23
            #endif
            #ifdef VARIANT24
            #define VARIANT 24
            #endif
            #ifdef VARIANT25
            #define VARIANT 25
            #endif
            #ifdef VARIANT26
            #define VARIANT 26
            #endif
            #ifdef VARIANT27
            #define VARIANT 27
            #endif
            #ifdef VARIANT28
            #define VARIANT 28
            #endif

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #define SHADERPASS SHADERPASS_DEFERRED_LIGHTING

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

            // The light loop (or lighting architecture) is in charge to:
            // - Define light list
            // - Define the light loop
            // - Setup the constant/data
            // - Do the reflection hierarchy
            // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

            #define HAS_LIGHTLOOP
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

            // Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
            // deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem),
            // the deferred shader will require to use multicompile.
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            //#define ENABLE_RAYTRACING
            #ifdef ENABLE_RAYTRACING
            CBUFFER_START(UnityDeferred)
                // Uniform variables that defines if we shall be using the shadow area texture or not
                int _RaytracedAreaShadow;
            CBUFFER_END
            #endif

            #ifndef SHADER_STAGE_FRAGMENT
            #undef  VARIANT // Prevent the chance of redefinition (1372256).
            #define VARIANT 28
            #endif

            uint g_TileListOffset;
            StructuredBuffer<uint> g_TileList;

            struct Attributes
            {
                uint vertexID  : SV_VertexID;
                uint instID    : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation uint3 tileIndexAndCoord : TEXCOORD0;
            };

            struct Outputs
            {
            #ifdef OUTPUT_SPLIT_LIGHTING
                float4 specularLighting : SV_Target0;
                float3 diffuseLighting  : SV_Target1;
            #else
                float4 combinedLighting : SV_Target0;
            #endif
            };

            Varyings Vert(Attributes input)
            {
                uint  tilePackIndex = g_TileList[g_TileListOffset + input.instID];
                uint2 tileCoord = uint2((tilePackIndex >> TILE_INDEX_SHIFT_X) & TILE_INDEX_MASK, (tilePackIndex >> TILE_INDEX_SHIFT_Y) & TILE_INDEX_MASK); // see builddispatchindirect.compute
                uint2 pixelCoord  = tileCoord * GetTileSize();

                uint screenWidth  = (uint)_ScreenSize.x;
                uint numTilesX    = (screenWidth + (TILE_SIZE_FPTL) - 1) / TILE_SIZE_FPTL;
                uint tileIndex    = tileCoord.x + tileCoord.y * numTilesX;

                // This handles both "real quad" and "2 triangles" cases: remaps {0, 1, 2, 3, 4, 5} into {0, 1, 2, 3, 0, 2}.
                uint quadIndex = (input.vertexID & 0x03) + (input.vertexID >> 2) * (input.vertexID & 0x01);
                float2 pp = GetQuadVertexPosition(quadIndex).xy;
                pixelCoord += uint2(pp.xy * TILE_SIZE_FPTL);

                Varyings output;
                output.positionCS = float4((pixelCoord * _ScreenSize.zw) * 2.0 - 1.0, 0, 1);
                // Tiles coordinates always start at upper-left corner of the screen (y axis down).
                // Clip-space coordinatea always have y axis up. Hence, we must always flip y.
                output.positionCS.y *= -1.0;
                output.tileIndexAndCoord = uint3(tileIndex, tileCoord);

                return output;
            }

            Outputs Frag(Varyings input)
            {
                // This need to stay in sync with deferred.compute

                uint tileIndex = input.tileIndexAndCoord.x;
                uint2 tileCoord = input.tileIndexAndCoord.yz;
                uint featureFlags = TileVariantToFeatureFlags(VARIANT, tileIndex);

                float depth = LoadCameraDepth(input.positionCS.xy).x;
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, tileCoord);

                float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

                BSDFData bsdfData;
                BuiltinData builtinData;
                DECODE_FROM_GBUFFER(posInput.positionSS, featureFlags, bsdfData, builtinData);

                PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

                LightLoopOutput lightLoopOutput;
                LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

                // Alias
                float3 diffuseLighting = lightLoopOutput.diffuseLighting;
                float3 specularLighting = lightLoopOutput.specularLighting;

                diffuseLighting *= GetCurrentExposureMultiplier();
                specularLighting *= GetCurrentExposureMultiplier();

                Outputs outputs;

            #ifdef OUTPUT_SPLIT_LIGHTING
                if (_EnableSubsurfaceScattering != 0 && ShouldOutputSplitLighting(bsdfData))
                {
                    outputs.specularLighting = float4(specularLighting, 1.0);
                    outputs.diffuseLighting  = TagLightingForSSS(diffuseLighting);
                }
                else
                {
                    outputs.specularLighting = float4(diffuseLighting + specularLighting, 1.0);
                    outputs.diffuseLighting  = 0;
                }
            #else
                outputs.combinedLighting = float4(diffuseLighting + specularLighting, 1.0);
            #endif

                return outputs;
            }

            ENDHLSL
        }

        // Debug modes (fullscreen)
        Pass
        {
            Name "Pass 1 - Debug (fullscreen)"

            // Stencil is used to skip background/sky pixels.
            Stencil
            {
                ReadMask[_StencilMask]
                Ref  [_StencilRef]
                Comp [_StencilCmp]
                Pass Keep
            }

            ZWrite Off
            ZTest  Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fragment _ OUTPUT_SPLIT_LIGHTING
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK /// Variant with and without shadowmask
            #pragma multi_compile_fragment PROBE_VOLUMES_OFF PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
            #pragma multi_compile_fragment SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH SHADOW_VERY_HIGH

            #define USE_FPTL_LIGHTLIST 1 // deferred opaque always use FPTL

            #ifdef DEBUG_DISPLAY
                // Don't care about this warning in debug
            #   pragma warning( disable : 4714 ) // sum of temp registers and indexable temp registers times 256 threads exceeds the recommended total 16384.  Performance may be reduced at kernel
            #endif

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #define SHADERPASS SHADERPASS_DEFERRED_LIGHTING

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

        #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #endif

            // The light loop (or lighting architecture) is in charge to:
            // - Define light list
            // - Define the light loop
            // - Setup the constant/data
            // - Do the reflection hierarchy
            // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

            #define HAS_LIGHTLOOP
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

            // Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
            // deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem),
            // the deferred shader will require to use multicompile.
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            //#define ENABLE_RAYTRACING
            #ifdef ENABLE_RAYTRACING
            CBUFFER_START(UnityDeferred)
                // Uniform variables that defines if we shall be using the shadow area texture or not
                int _RaytracedAreaShadow;
            CBUFFER_END
            #endif

            struct Attributes
            {
                uint vertexID  : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct Outputs
            {
            #ifdef OUTPUT_SPLIT_LIGHTING
                float4 specularLighting : SV_Target0;
                float3 diffuseLighting  : SV_Target1;
            #else
                float4 combinedLighting : SV_Target0;
            #endif
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            Outputs Frag(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // This need to stay in sync with deferred.compute

                // input.positionCS is SV_Position
                float depth = LoadCameraDepth(input.positionCS.xy).x;
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, uint2(input.positionCS.xy) / GetTileSize());

                float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

                BSDFData bsdfData;
                BuiltinData builtinData;
                DECODE_FROM_GBUFFER(posInput.positionSS, UINT_MAX, bsdfData, builtinData);

                PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

                LightLoopOutput lightLoopOutput;
                LightLoop(V, posInput, preLightData, bsdfData, builtinData, LIGHT_FEATURE_MASK_FLAGS_OPAQUE, lightLoopOutput);

                // Alias
                float3 diffuseLighting = lightLoopOutput.diffuseLighting;
                float3 specularLighting = lightLoopOutput.specularLighting;

                diffuseLighting *= GetCurrentExposureMultiplier();
                specularLighting *= GetCurrentExposureMultiplier();

                Outputs outputs;

            #ifdef OUTPUT_SPLIT_LIGHTING
                if (_EnableSubsurfaceScattering != 0 && ShouldOutputSplitLighting(bsdfData))
                {
                    outputs.specularLighting = float4(specularLighting, 1.0);
                    outputs.diffuseLighting  = TagLightingForSSS(diffuseLighting);
                }
                else
                {
                    outputs.specularLighting = float4(diffuseLighting + specularLighting, 1.0);
                    outputs.diffuseLighting  = 0;
                }
            #else
                outputs.combinedLighting = float4(diffuseLighting + specularLighting, 1.0);
            #endif

                return outputs;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
