Shader "Hidden/HDPipeline/ScreenSpace/AmbientOcclusion"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: Ambient occlusion estimation
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Estimation.hlsl"
            ENDHLSL
        }

        // 1: Denoising (horizontal pass)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #define AO_DENOISE_HORIZONTAL
            #define AO_DENOISE_CENTER_NORMAL
            #include "Denoising.hlsl"
            ENDHLSL
        }

        // 2: Denoising (vertical pass)
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #define AO_DENOISE_VERTICAL
            #include "Denoising.hlsl"
            ENDHLSL
        }

        // 3: Final filtering
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Composition.hlsl"
            ENDHLSL
        }
    }
}
