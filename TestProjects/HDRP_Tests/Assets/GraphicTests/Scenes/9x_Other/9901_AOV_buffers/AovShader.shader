// Recolor from Kino post processing effect suite

Shader "Hidden/HdrpAovTest/AovShader"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "AovShader.hlsl"
            ENDHLSL
        }
    }
}
