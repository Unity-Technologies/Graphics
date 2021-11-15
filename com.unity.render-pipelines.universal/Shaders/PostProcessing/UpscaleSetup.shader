Shader "Hidden/Universal Render Pipeline/Upscale Setup"
{
    HLSLINCLUDE
        #pragma multi_compile_local_fragment _ _FXAA
        #pragma multi_compile_local_fragment _ _GAMMA_20
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D_X(_SourceTex);
        float4 _SourceSize;

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            float2 positionNDC = uv;
            int2   positionSS = uv * _SourceSize.xy;

            half3 color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_PointClamp, uv).xyz;

#if _FXAA
            color = ApplyFXAA(color, positionNDC, positionSS, _SourceSize, _SourceTex);
#endif

#if _GAMMA_20
            // EASU expects the input image to be in gamma 2.0 color space so perform color space conversion
            // while we store the pixel data from the setup pass.
            color = LinearToGamma20(color);
#endif

            return half4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "UpscaleSetup"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
