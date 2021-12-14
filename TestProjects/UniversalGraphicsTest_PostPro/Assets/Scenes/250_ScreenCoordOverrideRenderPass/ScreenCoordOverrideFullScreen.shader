Shader "Hidden/Test/ScreenCoordOverrideFullScreen"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "ScreenCoordOverrideFullScreen"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile _ SCREEN_COORD_OVERRIDE

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ScreenCoordOverride.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            TEXTURE2D_X(_SourceTex);
            SAMPLER(sampler_SourceTex);

            half4 Fragment (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 baseColor = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv);
                float2 transformedUV = SCREEN_COORD_APPLY_SCALEBIAS(input.uv);
                return half4(lerp(baseColor.rgb, float3(transformedUV, 0), 0.5), 1);
            }

            ENDHLSL
        }
    }
}
