Shader "Hidden/HDRenderPipeline/DebugViewMaterialGBuffer"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #include "CoreRP/ShaderLibrary/Common.hlsl"

            // CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_LIT
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "../ShaderVariables.hlsl"
            #define DEBUG_DISPLAY
            #include "../Debug/DebugDisplay.hlsl"
            #include "../Material/Material.hlsl"

            #ifdef SHADOWS_SHADOWMASK
            TEXTURE2D(_ShadowMaskTexture);
            #endif

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // input.positionCS is SV_Position
                float depth = LOAD_TEXTURE2D(_MainDepthTexture, input.positionCS.xy).x;
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP);

                BSDFData bsdfData;
                BakeLightingData bakeLightingData;
                DECODE_FROM_GBUFFER(posInput.positionSS, UINT_MAX, bsdfData, bakeLightingData.bakeDiffuseLighting);
                #ifdef SHADOWS_SHADOWMASK
                DecodeShadowMask(LOAD_TEXTURE2D(_ShadowMaskTexture, posInput.positionSS), bakeLightingData.bakeShadowMask);
                #endif

                // Init to not expected value
                float3 result = float3(-666.0, 0.0, 0.0);
                bool needLinearToSRGB = false;

                if (_DebugViewMaterial == DEBUGVIEWGBUFFER_DEPTH)
                {
                    float linearDepth = frac(posInput.linearDepth * 0.1);
                    result = linearDepth.xxx;
                }
                // Caution: This value is not the same than the builtin data bakeDiffuseLighting. It also include emissive and multiply by the albedo
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_DIFFUSE_LIGHTING_WITH_ALBEDO_PLUS_EMISSIVE)
                {
                    // TODO: require a remap
                    // TODO: we should not gamma correct, but easier to debug for now without correct high range value
                    result = bakeLightingData.bakeDiffuseLighting; needLinearToSRGB = true;
                }
                #ifdef SHADOWS_SHADOWMASK
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK0)
                {
                    result = bakeLightingData.bakeShadowMask.xxx;
                }
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK1)
                {
                    result = bakeLightingData.bakeShadowMask.yyy;
                }
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK2)
                {
                    result = bakeLightingData.bakeShadowMask.zzz;
                }
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK3)
                {
                    result = bakeLightingData.bakeShadowMask.www;
                }
                #endif

                GetBSDFDataDebug(_DebugViewMaterial, bsdfData, result, needLinearToSRGB);

                // f we haven't touch result, we don't blend it. This allow to have the GBuffer debug pass working with the regular forward debug pass.
                // The forward debug pass will write its value and then the deferred will overwrite only touched texels.
                if (result.x == -666.0)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }
                else
                {
                    // TEMP!
                    // For now, the final blit in the backbuffer performs an sRGB write
                    // So in the meantime we apply the inverse transform to linear data to compensate.
                    if (!needLinearToSRGB)
                        result = SRGBToLinear(max(0, result));

                    return float4(result, 1.0);
                }
            }

            ENDHLSL
        }

    }
    Fallback Off
}
