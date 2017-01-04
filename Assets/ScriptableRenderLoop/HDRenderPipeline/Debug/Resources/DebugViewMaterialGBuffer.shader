Shader "Hidden/HDRenderPipeline/DebugViewMaterialGBuffer"
{
    SubShader
    {
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha // We will lerp only the values that are valid

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Common.hlsl"
            #include "Color.hlsl"

            // CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_LIT
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderConfig.cs.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderPipeline/Debug/DebugViewMaterial.cs.hlsl"    
            #include "Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Material.hlsl"

            DECLARE_GBUFFER_TEXTURE(_GBufferTexture);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER2D(sampler_CameraDepthTexture);
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
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
                float depth = LOAD_TEXTURE2D(_CameraDepthTexture, posInput.unPositionSS).x;
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
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_DIFFUSE_LIGHTING)
                {
                    result = bakeDiffuseLighting;
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
