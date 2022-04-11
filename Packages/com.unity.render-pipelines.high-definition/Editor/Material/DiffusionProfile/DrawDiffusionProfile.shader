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
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/DiffusionProfile/DiffusionProfile.hlsl"
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
                float  r = _MaxRadius * 0.5 * length(input.texcoord - 0.5); // (-0.25 * R, 0.25 * R)
                float3 S = _ShapeParam.rgb;
                float3 M;

                // Gamma in previews is weird...
                S = S * S;
                M = EvalBurleyDiffusionProfile(r, S) / r; // Divide by 'r' since we are not integrating in polar coords
                return float4(sqrt(M), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
