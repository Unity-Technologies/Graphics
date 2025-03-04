Shader "Hidden/Universal Render Pipeline/Scaling Setup"
{
    HLSLINCLUDE
        #pragma multi_compile_local_fragment _ _FXAA
        #pragma multi_compile_local_fragment _ _GAMMA_20 _GAMMA_20_AND_HDR_INPUT
        #pragma multi_compile_local_fragment _ _ENABLE_ALPHA_OUTPUT

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

            half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
#if _FXAA
    #if _ENABLE_ALPHA_OUTPUT
            // When alpha processing is enabled, FXAA should not affect pixels with zero alpha
            UNITY_BRANCH
            if(color.a > 0)
                color.rgb = ApplyFXAA(color.rgb, positionNDC, positionSS, _SourceSize, _BlitTexture, PaperWhite, OneOverPaperWhite);
    #else
            color.rgb = ApplyFXAA(color.rgb, positionNDC, positionSS, _SourceSize, _BlitTexture, PaperWhite, OneOverPaperWhite);
    #endif
#endif

#if _GAMMA_20 && !UNITY_COLORSPACE_GAMMA
            #ifdef HDR_INPUT
            // In HDR output mode, the colors are expressed in nits, which can go up to 10k nits.
            // We divide by the display max nits to get a max value closer to 1.0 but it could still be > 1.0.
            // Finally, use FastTonemap() to squash everything < 1.0.
            color = half4(FastTonemap(color.rgb * OneOverPaperWhite), color.a);
            #endif
            // EASU expects perceptually encoded color data so either encode to gamma 2.0 here if the input
            // data is linear, or let it pass through unchanged if it's already gamma encoded.
            color = LinearToGamma20(color);
#endif

#if _ENABLE_ALPHA_OUTPUT
            // Alpha will lose precision and band due to low bits in alpha.
            // Should be fine for alpha test mask.
            // Or a simple 2-bit dither can be done.
            // uint2 b = positionSS & 1;
            // uint dp = b.y ? (b.x ? 1 : 3) : (b.x ? 2 : 0);
            // half d = ((dp / 3.0) - 0.5) * 0.25;
            // color.a += d;
            return color;
#else
            return half4(color.rgb, 1.0);
#endif
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
