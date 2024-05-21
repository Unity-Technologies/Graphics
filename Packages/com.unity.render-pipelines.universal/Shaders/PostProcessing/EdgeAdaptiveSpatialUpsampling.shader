Shader "Hidden/Universal Render Pipeline/Edge Adaptive Spatial Upsampling"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        float4 _SourceSize;

        #define FSR_INPUT_TEXTURE _BlitTexture
        #define FSR_INPUT_SAMPLER sampler_LinearClamp

        #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/FSRCommon.hlsl"

        half4 FragEASU(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            uint2 integerUv = uv * _ScreenParams.xy;

            half3 color = ApplyEASU(integerUv);

            // Convert to linearly encoded color before we pass our output over to RCAS
#if UNITY_COLORSPACE_GAMMA
            color = GetSRGBToLinear(color);
#else
            color = Gamma20ToLinear(color);
#endif

#if _ENABLE_ALPHA_OUTPUT
            half alpha = SAMPLE_TEXTURE2D_X_LOD(FSR_INPUT_TEXTURE, FSR_INPUT_SAMPLER, uv, 0.0).a;
#else
            half alpha = 1.0;
#endif

            return half4(color, alpha);
        }

    ENDHLSL

    /// Shader that performs the EASU (upscaling) component of the two part FidelityFX Super Resolution technique
    /// The second part of the technique (RCAS) is handled in the FinalPost shader
    /// Note: This shader requires shader target 4.5 because it relies on texture gather instructions
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "EASU"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragEASU
                #pragma target 4.5

                #pragma multi_compile_local_fragment _ _ENABLE_ALPHA_OUTPUT
            ENDHLSL
        }
    }

    Fallback Off
}
