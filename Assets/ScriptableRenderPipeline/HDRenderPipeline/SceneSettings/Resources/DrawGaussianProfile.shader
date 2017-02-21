Shader "Hidden/HDRenderPipeline/DrawGaussianProfile"
{
    Properties
    {
        [HideInInspector] _StdDev1("", Color) = (0, 0, 0)
        [HideInInspector] _StdDev2("", Color) = (0, 0, 0)
        [HideInInspector] _LerpWeight("", Float) = 0
    }

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

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "ShaderLibrary/Common.hlsl"
            #include "ShaderLibrary/Color.hlsl"
            #include "HDRenderPipeline/ShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _StdDev1, _StdDev2; float _LerpWeight; // See 'SubsurfaceScatteringParameters'

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
                float  dist = length(2 * input.texcoord - 1);

                float3 var1 = _StdDev1.rgb * _StdDev1.rgb;
                float3 var2 = _StdDev2.rgb * _StdDev2.rgb;

                // Evaluate the linear combination of two 2D Gaussians instead of
                // product of a linear combination of two normalized 1D Gaussians
                // since we do not want to bother artists with the lack of radial symmetry.

                float3 magnitude = lerp(exp(-dist * dist / (2 * var1)) / (TWO_PI * var1),
                                        exp(-dist * dist / (2 * var2)) / (TWO_PI * var2), _LerpWeight);

                return float4(magnitude, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
