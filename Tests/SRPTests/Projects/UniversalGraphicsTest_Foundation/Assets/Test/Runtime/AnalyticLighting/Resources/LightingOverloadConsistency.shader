Shader "Hidden/Test/LightingOverloadConsistency"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _SPECULARHIGHLIGHTS_OFF

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float _SpecularHighlightsOffArg;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            // RGB is |canonical - deprecated| for the same inputs; any non-zero value means the
            // deprecated convenience overload no longer forwards correctly. Alpha carries the
            // canonical green channel as a positive control that the quad actually drew.
            half4 Frag(Varyings input) : SV_Target
            {
                half alpha = half(1.0);
                BRDFData brdfData;
                InitializeBRDFData(half3(1.0, 0.8, 0.6), half(0.0), half3(0.0, 0.0, 0.0), half(0.5), alpha, brdfData);

                // Normal deliberately differs from the view direction so argument swaps cannot cancel out.
                half3 normalWS = half3(0.0, 0.38268343, 0.92387953);
                half3 viewDirWS = half3(0.0, -0.31622776, 0.9486833);
                half3 lightDir = half3(0.0, 0.70710678, 0.70710678);
                half3 lightColor = half3(1.0, 0.95, 0.9);

                Light light = (Light)0;
                light.color = lightColor;
                light.direction = lightDir;
                // Not 1.0, so a dropped or mis-forwarded attenuation cannot read as no difference.
                light.distanceAttenuation = 0.5;
                light.shadowAttenuation = half(1.0);

                bool specularHighlightsOff = _SpecularHighlightsOffArg != 0.0;

                half3 canonical = LightingPhysicallyBased(brdfData, light, normalWS, viewDirWS, specularHighlightsOff);
                half3 deprecated = LightingPhysicallyBased(brdfData, lightColor, lightDir, 0.5, normalWS, viewDirWS, specularHighlightsOff);
                return half4(abs(canonical - deprecated), canonical.g);
            }
            ENDHLSL
        }
    }
}
