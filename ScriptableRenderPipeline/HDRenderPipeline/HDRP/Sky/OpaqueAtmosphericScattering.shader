Shader "Hidden/HDRenderPipeline/OpaqueAtmosphericScattering"
{
    SubShader
    {
        Pass
        {
            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal

            #pragma vertex Vert
            #pragma fragment Frag

            // #pragma enable_d3d11_debug_symbols

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "../ShaderVariables.hlsl"
            #include "AtmosphericScattering/AtmosphericScattering.hlsl"

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
                float depth = LOAD_TEXTURE2D(_MainDepthTexture, input.positionCS.xy).x;
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP);

                if (depth == UNITY_RAW_FAR_CLIP_VALUE)
                {
                    // When a pixel is at far plane, the world space coordinate reconstruction is not reliable.
                    // So in order to have a valid position (for example for height fog) we just consider that the sky is a sphere centered on camera with a radius of 5km (arbitrarily chosen value!)
                    // And recompute the position on the sphere with the current camera direction.
                    float3 viewDirection = -GetWorldSpaceNormalizeViewDir(posInput.positionWS) * 5000.0f;
                    posInput.positionWS = GetPrimaryCameraPosition() + viewDirection;
                }

                return EvaluateAtmosphericScattering(posInput);
            }
            ENDHLSL
        }
    }
}
