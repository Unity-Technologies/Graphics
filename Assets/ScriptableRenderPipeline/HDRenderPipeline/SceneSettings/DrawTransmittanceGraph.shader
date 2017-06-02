Shader "Hidden/HDRenderPipeline/DrawTransmittanceGraph"
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

            #include "../../ShaderLibrary/Common.hlsl"
            #include "../../ShaderLibrary/Color.hlsl"
            #include "../ShaderVariables.hlsl"
            #define UNITY_MATERIAL_LIT // Needs to be defined before including Material.hlsl
            #include "../Material/Material.hlsl"

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
                float3 T = ComputeTransmittance(_ShapeParameter.rgb, _SurfaceAlbedo.rgb, d, 1);

                return float4(T, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
