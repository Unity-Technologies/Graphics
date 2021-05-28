Shader "Hidden/HDRP/DebugViewMaterialGBuffer"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

            #define DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

            // Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
            // deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem),
            // the deferred shader will require to use multicompile.
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // input.positionCS is SV_Position
                float depth = LoadCameraDepth(input.positionCS.xy);
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

                BSDFData bsdfData;
                BuiltinData builtinData;
                DECODE_FROM_GBUFFER(posInput.positionSS, UINT_MAX, bsdfData, builtinData);

                // Init to not expected value
                float3 result = float3(-666.0, 0.0, 0.0);
                bool needLinearToSRGB = false;

                // Reminder: _DebugViewMaterialArray[i]
                //   i==0 -> the size used in the buffer
                //   i>0  -> the index used (0 value means nothing)
                // The index stored in this buffer could either be
                //   - a gBufferIndex (always stored in _DebugViewMaterialArray[1] as only one supported)
                //   - a property index which is different for each kind of material even if reflecting the same thing (see MaterialSharedProperty)
                // So here if the buffer is of size zero, it is the same as if we give in a 0 buffer index.
                int bufferIndex = _DebugViewMaterialArray[0].x >= 1 ? _DebugViewMaterialArray[1].x : 0;
                if (bufferIndex == DEBUGVIEWGBUFFER_DEPTH)
                {
                    float linearDepth = frac(posInput.linearDepth * 0.1);
                    result = linearDepth.xxx;
                }
                // Caution: This value is not the same than the builtin data bakeDiffuseLighting. It also include emissive and multiply by the albedo
                else if (bufferIndex == DEBUGVIEWGBUFFER_BAKE_DIFFUSE_LIGHTING_WITH_ALBEDO_PLUS_EMISSIVE)
                {
                    result = builtinData.bakeDiffuseLighting;
                    result *= GetCurrentExposureMultiplier();
                    needLinearToSRGB = true;
                }
#ifdef SHADOWS_SHADOWMASK
                else if (bufferIndex == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK0)
                {
                    result = builtinData.shadowMask0.xxx;
                }
                else if (bufferIndex == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK1)
                {
                    result = builtinData.shadowMask1.xxx;
                }
                else if (bufferIndex == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK2)
                {
                    result = builtinData.shadowMask2.xxx;
                }
                else if (bufferIndex == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK3)
                {
                    result = builtinData.shadowMask3.xxx;
                }
#endif

                GetBSDFDataDebug(bufferIndex, bsdfData, result, needLinearToSRGB);

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
