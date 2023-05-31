Shader "Hidden/Universal Render Pipeline/Scaling Setup"
{
    HLSLINCLUDE
        #pragma multi_compile_local_fragment _ _FXAA
        #pragma multi_compile_local_fragment _ _GAMMA_20 _GAMMA_20_AND_HDR_INPUT

        #if defined(_GAMMA_20_AND_HDR_INPUT)
        #define _GAMMA_20 1
        #define HDR_INPUT 1
        #endif

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        float4 _SourceSize;
        float4 _HDROutputLuminanceParams;
        #define PaperWhite _HDROutputLuminanceParams.z
        #define OneOverPaperWhite _HDROutputLuminanceParams.w

        half4 FragScalingSetup(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float2 positionNDC = uv;
            int2   positionSS = uv * _SourceSize.xy;

            half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).xyz;

#if _FXAA
            color = ApplyFXAA(color, positionNDC, positionSS, _SourceSize, _BlitTexture, PaperWhite, OneOverPaperWhite);
#endif

#if _GAMMA_20 && !UNITY_COLORSPACE_GAMMA
            #ifdef HDR_INPUT
            // In HDR output mode, the colors are expressed in nits, which can go up to 10k nits.
            // We divide by the display max nits to get a max value closer to 1.0 but it could still be > 1.0.
            // Finally, use FastTonemap() to squash everything < 1.0.
            color = FastTonemap(color * OneOverPaperWhite);
            #endif
            // EASU expects perceptually encoded color data so either encode to gamma 2.0 here if the input
            // data is linear, or let it pass through unchanged if it's already gamma encoded.
            color = LinearToGamma20(color);
#endif

            return half4(color, 1.0);
        }

    ENDHLSL

    ///
    /// Scaling Setup Shader
    ///
    /// This shader is used to perform any operations that need to place before image scaling occurs.
    /// It is not expected to be executed unless image scaling is active.
    ///
    /// Supported Operations:
    ///
    /// FXAA
    /// The FXAA shader does not support mismatched input and output dimensions so it must be run before any image
    /// scaling takes place.
    ///
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "ScalingSetup"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragScalingSetup
            ENDHLSL
        }
    }
}
