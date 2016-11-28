Shader "Hidden/HDRenderLoop/Deferred"
{
    Properties
    {
        // We need to be able to control the blend mode for deferred shader in case we do multiple pass
        _SrcBlend("", Float) = 1
        _DstBlend("", Float) = 1
    }

    SubShader
    {

        Pass
        {
            ZWrite Off
            Blend Off
            Blend[_SrcBlend][_DstBlend]

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

            #pragma vertex VertDeferred
            #pragma fragment FragDeferred

            // Chose supported lighting architecture in case of deferred rendering
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            //#pragma multi_compile SHADOWFILTERING_FIXED_SIZE_PCF 

            // TODO: Workflow problem here, I would like to only generate variant for the LIGHTLOOP_TILE_PASS case, not the LIGHTLOOP_SINGLE_PASS case. This must be on lightloop side and include here.... (Can we codition
            #pragma multi_compile LIGHTLOOP_TILE_DIRECT LIGHTLOOP_TILE_INDIRECT LIGHTLOOP_TILE_ALL
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

            DECLARE_GBUFFER_TEXTURE(_GBufferTexture);
 
 			TEXTURE2D(_CameraDepthTexture);
			SAMPLER2D(sampler_CameraDepthTexture);

            float4x4 _InvViewProjMatrix;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings VertDeferred(Attributes input)
            {
                // TODO: implement SV_vertexID full screen quad
                // Lights are draw as one fullscreen quad
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);

                return output;
            }

            float4 FragDeferred(Varyings input) : SV_Target
            {
				float4 unPositionSS = input.positionCS; // as input we have the vpos
                Coordinate coord = GetCoordinate(unPositionSS.xy, _ScreenSize.zw);

                // No need to manage inverse depth, this is handled by the projection matrix
                float depth = LOAD_TEXTURE2D(_CameraDepthTexture, coord.unPositionSS).x;
                float3 positionWS = UnprojectToWorld(depth, coord.positionSS, _InvViewProjMatrix);
                float3 V = GetWorldSpaceNormalizeViewDir(positionWS);

                FETCH_GBUFFER(gbuffer, _GBufferTexture, coord.unPositionSS);
                BSDFData bsdfData;
                float3 bakeDiffuseLighting;
                DECODE_FROM_GBUFFER(gbuffer, bsdfData, bakeDiffuseLighting);

                PreLightData preLightData = GetPreLightData(V, positionWS, coord, bsdfData);

                float3 diffuseLighting;
                float3 specularLighting;
                LightLoop(V, positionWS, coord, preLightData, bsdfData, bakeDiffuseLighting, diffuseLighting, specularLighting);

                return float4(diffuseLighting + specularLighting, 1.0);
            }

        ENDHLSL
        }

    }
    Fallback Off
}
