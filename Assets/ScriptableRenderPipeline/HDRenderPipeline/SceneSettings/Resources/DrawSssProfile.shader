Shader "Hidden/HDRenderPipeline/DrawSssProfile"
{
    SubShader
    {
        Pass
        {
            Cull   Off
            ZTest  Off
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 metal // TEMP: until we go further in dev

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile SSS_MODEL_BASIC SSS_MODEL_DISNEY

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "../../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderVariables.hlsl"
        #ifdef SSS_MODEL_BASIC
            #include "../../Material/Lit/SubsurfaceScatteringProfile.cs.hlsl"
        #endif

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

        #ifdef SSS_MODEL_DISNEY
            float4 _ShapeParam; float _MaxRadius; // See 'SubsurfaceScatteringProfile'
        #else
            float4 _StdDev1, _StdDev2;
            float _LerpWeight, _MaxRadius; // See 'SubsurfaceScatteringParameters'
        #endif

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
                output.vertex   = TransformWorldToHClip(input.vertex);
                output.texcoord = input.texcoord.xy;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
            #ifdef SSS_MODEL_DISNEY
                float  r = (2 * length(input.texcoord - 0.5)) * _MaxRadius;
                float3 S = _ShapeParam.rgb;
                float3 M = S * (exp(-r * S) + exp(-r * S * (1.0 / 3.0))) / (8 * PI * r);
                float3 A = _MaxRadius / S;

                // N.b.: we multiply by the surface albedo of the actual geometry during shading.
                // Apply gamma for visualization only. Do not apply gamma to the color.
                return float4(pow(M, 1.0 / 3.0) * A, 1);
            #else
                float  r    = (2 * length(input.texcoord - 0.5)) * _MaxRadius * SSS_BASIC_DISTANCE_SCALE;
                float3 var1 = _StdDev1.rgb * _StdDev1.rgb;
                float3 var2 = _StdDev2.rgb * _StdDev2.rgb;

                // Evaluate the linear combination of two 2D Gaussians instead of
                // product of the linear combination of two normalized 1D Gaussians
                // since we do not want to bother artists with the lack of radial symmetry.

                float3 magnitude = lerp(exp(-r * r / (2 * var1)) / (TWO_PI * var1),
                                        exp(-r * r / (2 * var2)) / (TWO_PI * var2), _LerpWeight);

                return float4(magnitude, 1);
            #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}
