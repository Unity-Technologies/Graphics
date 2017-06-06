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

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "../../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _SurfaceAlbedo, _SurfaceShapeParam;
            float _ScatteringDistance; // See 'SubsurfaceScatteringProfile'

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
                float  r = (2 * length(input.texcoord - 0.5)) * _ScatteringDistance;
                float3 S = _SurfaceShapeParam.rgb;
                float3 M = S * (exp(-r * S) + exp(-r * S * (1.0 / 3.0))) / (8 * PI * r);

                // N.b.: we multiply by the surface albedo of the actual geometry during shading.
                // Apply gamma for visualization only. Do not apply gamma to the color.
                return float4(pow(M, 1.0 / 3.0) * _SurfaceAlbedo.rgb, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
