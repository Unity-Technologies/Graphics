Shader "Hidden/Custom/LWtest/HalftoneOpaque"
{
    Properties
    {
        _Pattern ("Pattern", 2D) = "grey" {}
        [IntRange]_Steps ("Steps", Range(1, 10)) = 4
    }
    HLSLINCLUDE

        #include "../../../../../../../PostProcessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        //Texture2D _Pattern;
        //Texture2D _MainTex;
        //SamplerState sampler_MainTex;
        //SamplerState sampler_Pattern;
        TEXTURE2D_SAMPLER2D(_Pattern, sampler_Pattern);
        float _Blend;
        float _Scale;
        float _Steps;

        float4 Frag(VaryingsDefault i) : SV_Target
        {

            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            float luminance = 1 + dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750)) * 0.25;
            float3 pattern = SAMPLE_TEXTURE2D(_Pattern, sampler_Pattern, i.texcoord * (_ScreenParams.xy * _Scale * luminance)).rgb;
            color.rgb = round(((pattern.rrr)-_Blend) + (color * _Steps)) / _Steps;
            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
