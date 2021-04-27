Shader "Hidden/HDRP/DrawDiffusionProfile"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/EditorShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _ShapeParam; float _MaxRadius; // See 'DiffusionProfile'

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            struct Attributes
            {
                float3 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                // We still use the legacy matrices in the editor GUI
                output.vertex   = mul(unity_MatrixVP, float4(input.vertex, 1));
                output.texcoord = input.texcoord.xy;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Profile display does not use premultiplied S.
                float  r = (2 * length(input.texcoord - 0.5)) * _MaxRadius;
                float3 S = _ShapeParam.rgb;
                float3 M = S * (exp(-r * S) + exp(-r * S * (1.0 / 3.0))) / (8 * PI * r);
                float3 A = _MaxRadius / S;

                // N.b.: we multiply by the surface albedo of the actual geometry during shading.
                // Apply gamma for visualization only. Do not apply gamma to the color.
                return float4(sqrt(M) * A, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
