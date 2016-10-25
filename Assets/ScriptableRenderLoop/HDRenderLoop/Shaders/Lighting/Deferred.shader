Shader "Hidden/HDRenderLoop/Deferred"
{
    Properties
    {
        _SrcBlend("", Float) = 1
        _DstBlend("", Float) = 1
    }

    SubShader
    {

        Pass
        {
            ZWrite Off
            Blend[_SrcBlend][_DstBlend]

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

            #pragma vertex VertDeferred
            #pragma fragment FragDeferred

            // Chose supported lighting architecture in case of deferred rendering
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS
            #pragma multi_compile SHADOWFILTERING_FIXED_SIZE_PCF 

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Common.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderConfig.cs"

            // CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_DISNEYGXX
            // TODO: Currently a users that add a new deferred material must also add it manually here... Need to think about it. Maybe add a multicompile inside a file in Material directory to include here ?
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/Lighting.hlsl" // This include Material.hlsl
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            DECLARE_GBUFFER_TEXTURE(_CameraGBufferTexture);
            DECLARE_GBUFFER_BAKE_LIGHTING(_CameraGBufferTexture);

            UNITY_DECLARE_TEX2D(_CameraDepthTexture);

            float4x4 _InvViewProjMatrix;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHS : SV_POSITION;
            };

            Varyings VertDeferred(Attributes input)
            {
                // TODO: implement SV_vertexID full screen quad
                // Lights are draw as one fullscreen quad
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionHS = TransformWorldToHClip(positionWS);

                return output;
            }

            float4 FragDeferred(Varyings input) : SV_Target
            {
                Coordinate coord = GetCoordinate(input.positionHS.xy, _ScreenSize.zw);

                // No need to manage inverse depth, this is handled by the projection matrix
                float depth = _CameraDepthTexture.Load(uint3(coord.unPositionSS, 0)).x;
                float3 positionWS = UnprojectToWorld(depth, coord.positionSS, _InvViewProjMatrix);
                float3 V = GetWorldSpaceNormalizeViewDir(positionWS);

                FETCH_GBUFFER(gbuffer, _CameraGBufferTexture, coord.unPositionSS);
                BSDFData bsdfData = DECODE_FROM_GBUFFER(gbuffer);

                PreLightData preLightData = GetPreLightData(V, positionWS, coord, bsdfData);

                float4 diffuseLighting;
                float4 specularLighting;
                FETCH_BAKE_LIGHTING_GBUFFER(gbuffer, _CameraGBufferTexture, coord.unPositionSS);
                float3 bakeDiffuseLighting = DECODE_BAKE_LIGHTING_FROM_GBUFFER(gbuffer);
                LightLoop(V, positionWS, preLightData, bsdfData, bakeDiffuseLighting, diffuseLighting, specularLighting);

                return float4(diffuseLighting.rgb + specularLighting.rgb, 1.0);
            }

        ENDHLSL
        }

    }
    Fallback Off
}
