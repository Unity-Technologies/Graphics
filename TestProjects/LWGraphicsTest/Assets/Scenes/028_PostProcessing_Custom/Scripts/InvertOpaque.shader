Shader "Hidden/Custom/LWtest/InvertOpaque"
{
    HLSLINCLUDE

        #include "//Assets/PostProcessing/PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        float4 Frag(VaryingsDefault i) : SV_Target
        {

            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            return float4(1-color.rgb, color.a);
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
