Shader "Test/OutputColorsToMRTs"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS       : POSITION;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            struct FragmentOutput
            {
                half4 dest0 : SV_Target0;
                half4 dest1 : SV_Target1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vertexInput.positionCS;
                return output;
            }

            FragmentOutput frag(Varyings input) : SV_Target
            {
                FragmentOutput o;
                o.dest0 = half4(0, 0, 1, 1);
                o.dest1 = half4(1, 0, 0, 1);
                return o;
            }

            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}
