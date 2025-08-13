Shader "Hidden/Core/CustomDepthBlit"
{
    SubShader
    {
        HLSLINCLUDE
        #define USE_FULL_PRECISION_BLIT_TEXTURE

        // For now we don't test XR in this graphics test 
        #undef UNITY_STEREO_INSTANCING_ENABLED
        #undef UNITY_STEREO_MULTIVIEW_ENABLED
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL


        Tags { "RenderType"="Opaque" }

        LOD 100
        Cull Off
        ZTest Always

        Pass
        {
            Name "Depth"
            ColorMask 0
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            float Frag (Varyings input) : SV_Depth
            {
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).r;
            }

            ENDHLSL
        }

        Pass
        {
            Name "Depth As Color"
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag (Varyings input) : SV_Target
            {
                float depth = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).r;
                return depth;
            }

            ENDHLSL
        }
    }
}
