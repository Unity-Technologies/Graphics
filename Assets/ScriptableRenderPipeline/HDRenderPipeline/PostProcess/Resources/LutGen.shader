Shader "Hidden/HDRenderPipeline/LutGen"
{
    HLSLINCLUDE

        #pragma target 4.5
        #include "ShaderLibrary/Common.hlsl"
        #include "HDRenderPipeline/ShaderVariables.hlsl"
        #include "ColorGrading.hlsl"

        float4 _LutParams;

        struct Attributes 
        {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct Varyings 
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.vertex = TransformWorldToHClip(input.vertex);
            output.texcoord = input.texcoord.xy;
            return output;
        }

        float4 Frag(Varyings input) : SV_Target
        {
            // 2D strip lut
            float2 uv = input.texcoord - _LutParams.yz;
            float3 color;
            color.r = frac(uv.x * _LutParams.x);
            color.b = uv.x - color.r / _LutParams.x;
            color.g = uv.y;

            // Lut is in LogC
            float3 colorLogC = color * _LutParams.w;

            // Switch back to unity linear
            float3 colorLinear = LogCToLinear(colorLogC);

            return float4(colorLinear, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }

    Fallback Off
}
