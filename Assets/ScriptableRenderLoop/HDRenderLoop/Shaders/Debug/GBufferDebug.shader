Shader "Hidden/Unity/GBufferDebug" 
{
    SubShader
    {

        Pass
        {
            ZWrite Off
            Blend One Zero

            HLSLPROGRAM
            #pragma target 5.0
            #pragma only_renderers d3d11 // TEMP: unitl we go futher in dev

            #pragma vertex VertDeferred
            #pragma fragment FragDeferred

            // CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_DISNEYGXX
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/Lighting.hlsl" // This include Material.hlsl
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/ShaderVariables.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Debug/DebugCommon.hlsl"
            #include "Assets/ScriptableRenderLoop/ShaderLibrary/Color.hlsl"

            DECLARE_GBUFFER_TEXTURE(_CameraGBufferTexture);
            DECLARE_GBUFFER_BAKE_LIGHTING(_CameraGBufferTexture);
            Texture2D	_CameraDepthTexture;
            float4		_ScreenSize;
            float		_DebugMode;
            float4x4	_InvViewProjMatrix;

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

                float depth = _CameraDepthTexture.Load(uint3(coord.unPositionSS, 0)).x;

                FETCH_GBUFFER(gbuffer, _CameraGBufferTexture, coord.unPositionSS);
                BSDFData bsdfData = DECODE_FROM_GBUFFER(gbuffer);

                float3 result = float3(1.0, 1.0, 0.0);
                bool outputIsLinear = false;

                if (_DebugMode == GBufferDebugDiffuseColor)
                {
                    result = bsdfData.diffuseColor;
                }
                else if (_DebugMode == GBufferDebugNormal)
                {
                    result = bsdfData.normalWS * 0.5 + 0.5;
                    outputIsLinear = true;
                }
                else if (_DebugMode == GBufferDebugDepth)
                {
                    float linearDepth = frac(LinearEyeDepth(depth, _ZBufferParams) * 0.1);
                    result = linearDepth.xxx;
                    outputIsLinear = true;
                }
                else if (_DebugMode == GBufferDebugBakedDiffuse)
                {
                    FETCH_BAKE_LIGHTING_GBUFFER(gbuffer, _CameraGBufferTexture, coord.unPositionSS);
                    result = DECODE_BAKE_LIGHTING_FROM_GBUFFER(gbuffer);
                    outputIsLinear = true;
                }
                else if (_DebugMode == GBufferDebugSpecularColor)
                {
                    result = bsdfData.fresnel0;
                }
                else if (_DebugMode == GBufferDebugSpecularOcclustion)
                {
                    result = bsdfData.specularOcclusion.xxx;
                    outputIsLinear = true;
                }
                else if (_DebugMode == GBufferDebugSmoothness)
                {
                    result = (1.0 - bsdfData.perceptualRoughness).xxx;
                    outputIsLinear = true;
                }
                else if (_DebugMode == GBufferDebugMaterialId)
                {
                    result = bsdfData.materialId.xxx;
                    outputIsLinear = true;
                }
                if (outputIsLinear)
                    result = SRGBToLinear(max(0, result));

                return float4(result, 1.0);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
