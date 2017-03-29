Shader "Hidden/HDRenderPipeline/DebugViewMaterialGBuffer"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha // We will lerp only the values that are valid

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #include "../../../ShaderLibrary/Common.hlsl"
            #include "../../../ShaderLibrary/Color.hlsl"

            // CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_LIT
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "../../ShaderConfig.cs.hlsl"
            #include "../../ShaderVariables.hlsl"
            #include "../../Debug/DebugViewMaterial.cs.hlsl"
            #include "../../Material/Material.hlsl"

            DECLARE_GBUFFER_TEXTURE(_GBufferTexture);

            int         _DebugViewMaterial;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                // TODO: implement SV_vertexID full screen quad
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // input.positionCS is SV_Position
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, uint2(0, 0));
                float depth = LOAD_TEXTURE2D(_MainDepthTexture, posInput.unPositionSS).x;
                UpdatePositionInput(depth, _InvViewProjMatrix, _ViewProjMatrix, posInput);

                FETCH_GBUFFER(gbuffer, _GBufferTexture, posInput.unPositionSS);
                BSDFData bsdfData;
                float3 bakeDiffuseLighting;
                DECODE_FROM_GBUFFER(gbuffer, bsdfData, bakeDiffuseLighting);

                // Init to not expected value
                float3 result = float3(-666.0, 0.0, 0.0);
                bool needLinearToSRGB = false;

                if (_DebugViewMaterial == DEBUGVIEWGBUFFER_DEPTH)
                {
                    float linearDepth = frac(posInput.depthVS * 0.1);
                    result = linearDepth.xxx;
                }
                // Caution: This value is not the same than the builtin data bakeDiffuseLighting. It also include emissive and multiply by the albedo
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_DIFFUSE_LIGHTING_WITH_ALBEDO_PLUS_EMISSIVE)
                {
                    // TODO: require a remap
                    // TODO: we should not gamma correct, but easier to debug for now without correct high range value
                    result = bakeDiffuseLighting; needLinearToSRGB = true;
                }

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
