Shader "Hidden/Test/ScreenCoordOverrideFullScreen"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ScreenCoordOverride.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            #pragma multi_compile _ SCREEN_COORD_OVERRIDE

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float2 transformedUV = SCREEN_COORD_APPLY_SCALEBIAS(input.uv);
                return float4(lerp(baseColor.rgb, float3(transformedUV, 0), 0.5), 1);
            }

            ENDHLSL
        }
    }
}
