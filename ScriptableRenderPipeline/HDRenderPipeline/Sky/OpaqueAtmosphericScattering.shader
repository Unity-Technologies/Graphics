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

            #pragma enable_d3d11_debug_symbols

            #include "ShaderLibrary/Common.hlsl"
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
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
                float depth = LOAD_TEXTURE2D(_MainDepthTexture, posInput.positionSS).x;
                UpdatePositionInput(depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP, posInput);

                return EvaluateAtmosphericScattering(posInput);
            }
            ENDHLSL
        }
    }
}
