Shader "Hidden/HDRenderPipeline/DrawTransmittanceGraph"
{
    Properties
    {
        [HideInInspector] _StdDev1("", Color) = (0, 0, 0)
        [HideInInspector] _StdDev2("", Color) = (0, 0, 0)
        [HideInInspector] _LerpWeight("", Float) = 0
        [HideInInspector] _ThicknessScale("", Float) = 0
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

            #include "../../../ShaderLibrary/Common.hlsl"
            #include "../../../ShaderLibrary/Color.hlsl"
            #include "../../ShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _StdDev1, _StdDev2, _ThicknessRemap;
            float _LerpWeight; // See 'SubsurfaceScatteringParameters'

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
                float thickness = _ThicknessRemap.x + input.texcoord.x * (_ThicknessRemap.y - _ThicknessRemap.x);
                float t2        = thickness * thickness;

                float3 var1 = _StdDev1.rgb * _StdDev1.rgb;
                float3 var2 = _StdDev2.rgb * _StdDev2.rgb;

                // See ComputeTransmittance() in Lit.hlsl for more details.
                float3 transmittance = lerp(exp(-t2 * 0.5 * rcp(var1)),
                                            exp(-t2 * 0.5 * rcp(var2)), _LerpWeight);

                return float4(transmittance, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
