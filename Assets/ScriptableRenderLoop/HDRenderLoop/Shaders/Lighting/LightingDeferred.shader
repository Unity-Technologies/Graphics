Shader "Hidden/Unity/LightingDeferred"
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

            // CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_DISNEYGXX
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Lighting.hlsl" // This include Material.hlsl
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderVariables.hlsl"

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

                // NOTE: Currently calling the forward loop, same code... :)
                float4 diffuseLighting;
                float4 specularLighting;
                ForwardLighting(V, positionWS, preLightData, bsdfData, diffuseLighting, specularLighting);

                FETCH_BAKE_LIGHTING_GBUFFER(gbuffer, _CameraGBufferTexture, coord.unPositionSS);
                diffuseLighting.rgb += DECODE_BAKE_LIGHTING_FROM_GBUFFER(gbuffer);

                return float4(diffuseLighting.rgb + specularLighting.rgb, 1.0);
            }

        ENDHLSL
        }

    }
    Fallback Off
}
