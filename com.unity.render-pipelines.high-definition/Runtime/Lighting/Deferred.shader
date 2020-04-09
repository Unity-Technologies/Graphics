Shader "Hidden/HDRP/Deferred"
{
    Properties
    {
        [HideInInspector] _StencilMask("_StencilMask", Int) = 6 // StencilUsage.RequiresDeferredLighting | StencilUsage.SubsurfaceScattering
        [HideInInspector] _StencilRef("", Int) = 0
        [HideInInspector] _StencilCmp("", Int) = 3
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Stencil
            {
                ReadMask [_StencilMask]
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
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER

            // Split lighting is utilized during the SSS pass.
            #pragma multi_compile _ OUTPUT_SPLIT_LIGHTING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DEBUG_DISPLAY

            #define USE_FPTL_LIGHTLIST // deferred opaque always use FPTL

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
                uint vertexID : SV_VertexID;
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
                float depth = LoadCameraDepth(input.positionCS.xy);

                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, uint2(input.positionCS.xy) / GetTileSize());
                float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

                BSDFData bsdfData;
                BuiltinData builtinData;
                DECODE_FROM_GBUFFER(posInput.positionSS, UINT_MAX, bsdfData, builtinData);

                PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

                float3 diffuseLighting;
                float3 specularLighting;
                LightLoop(V, posInput, preLightData, bsdfData, builtinData, LIGHT_FEATURE_MASK_FLAGS_OPAQUE, diffuseLighting, specularLighting);

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
