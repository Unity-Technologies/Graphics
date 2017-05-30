Shader "Hidden/HDRenderPipeline/DrawTransmittanceGraph"
{
    Properties
    {
        [HideInInspector] _StdDev1("", Vector)   = (0, 0, 0, 0)
        [HideInInspector] _StdDev2("", Vector)   = (0, 0, 0, 0)
        [HideInInspector] _LerpWeight("", Float) = 0
        [HideInInspector] _TintColor("", Vector) = (0, 0, 0, 0)
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

            float4 _SurfaceAlbedo, _ShapeParameter, _ThicknessRemap;
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
                float  d = (_ThicknessRemap.x + input.texcoord.x * (_ThicknessRemap.y - _ThicknessRemap.x));
                float3 S = _ShapeParameter.rgb;
                float3 T = 0.5 * exp(-d * S) + 0.5 * exp(-d * S * (1.0 / 3.0));

                return float4(T * _SurfaceAlbedo.rgb, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
