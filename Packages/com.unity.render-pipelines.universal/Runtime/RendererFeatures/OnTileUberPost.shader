Shader "OnTileUberPost"
{
    HLSLINCLUDE
    #pragma multi_compile_local_fragment _ _HDR_GRADING _TONEMAP_ACES _TONEMAP_NEUTRAL
    #pragma multi_compile_local_fragment _ _FILM_GRAIN
    #pragma multi_compile_local_fragment _ _DITHERING
    #pragma multi_compile_local_fragment _ _ENABLE_ALPHA_OUTPUT
    #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ScreenCoordOverride.hlsl"


    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

    TEXTURE2D(_InternalLut);
    TEXTURE2D(_UserLut);

    TEXTURE2D(_BlueNoise_Texture);
    TEXTURE2D(_Grain_Texture);

    float4 _Lut_Params;
    float4 _UserLut_Params;

    half4 _Vignette_Params1;
    float4 _Vignette_Params2;
#ifdef USING_STEREO_MATRICES
    float4 _Vignette_ParamsXR;
#endif
    float2 _Grain_Params;
    float4 _Grain_TilingParams;
    float4 _Dithering_Params;
    float4 _HDROutputLuminanceParams;

    #define VignetteColor           _Vignette_Params1.xyz
    #ifdef USING_STEREO_MATRICES
    #define VignetteCenterEye0      _Vignette_ParamsXR.xy
    #define VignetteCenterEye1      _Vignette_ParamsXR.zw
    #else
    #define VignetteCenter          _Vignette_Params2.xy
    #endif
    #define VignetteIntensity       _Vignette_Params2.z
    #define VignetteSmoothness      _Vignette_Params2.w
    #define VignetteRoundness       _Vignette_Params1.w

    #define LutParams               _Lut_Params.xyz
    #define PostExposure            _Lut_Params.w
    #define UserLutParams           _UserLut_Params.xyz
    #define UserLutContribution     _UserLut_Params.w

    #define GrainIntensity          _Grain_Params.x
    #define GrainResponse           _Grain_Params.y
    #define GrainScale              _Grain_TilingParams.xy
    #define GrainOffset             _Grain_TilingParams.zw

    #define DitheringScale          _Dithering_Params.xy
    #define DitheringOffset         _Dithering_Params.zw

    #define AlphaScale              1.0
    #define AlphaBias               0.0

    #define MinNits                 _HDROutputLuminanceParams.x
    #define MaxNits                 _HDROutputLuminanceParams.y
    #define PaperWhite              _HDROutputLuminanceParams.z
    #define OneOverPaperWhite       _HDROutputLuminanceParams.w

    half4 UberPost(half4 inputColor, float2 uv)
    {
        half3 color = inputColor.rgb;

        // To save on variants we use an uniform branch for vignette. This may have performance impact on lower end platforms
        UNITY_BRANCH
        if (VignetteIntensity > 0)
        {
        #ifdef USING_STEREO_MATRICES
            // With XR, the views can use asymmetric FOV which will have the center of each
            // view be at a different location.
            const float2 VignetteCenter = unity_StereoEyeIndex == 0 ? VignetteCenterEye0 : VignetteCenterEye1;
        #endif

            color = ApplyVignette(color, uv, VignetteCenter, VignetteIntensity, VignetteRoundness, VignetteSmoothness, VignetteColor);
        }

        // Color grading is always enabled when post-processing/uber is active
        {
            color = ApplyColorGrading(color, PostExposure, TEXTURE2D_ARGS(_InternalLut, sampler_LinearClamp), LutParams, TEXTURE2D_ARGS(_UserLut, sampler_LinearClamp), UserLutParams, UserLutContribution, PaperWhite, OneOverPaperWhite);
        }
        
        #if _FILM_GRAIN
        {
            color = ApplyGrain(color, uv, TEXTURE2D_ARGS(_Grain_Texture, sampler_LinearRepeat), GrainIntensity, GrainResponse, GrainScale, GrainOffset, OneOverPaperWhite);
        }
        #endif

        #if _DITHERING
        {
            color = ApplyDithering(color, uv, TEXTURE2D_ARGS(_BlueNoise_Texture, sampler_PointRepeat), DitheringScale, DitheringOffset, PaperWhite, OneOverPaperWhite);
        }
        #endif
       
#if _ENABLE_ALPHA_OUTPUT
        // Saturate is necessary to avoid issues when additive blending pushes the alpha over 1.
        return half4(color, saturate(inputColor.a));
#else
        return half4(color, 1);
#endif
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Name "OnTileUberPost"
            LOD 100
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragUberPost

                // Declares the framebuffer input as a texture 2d containing half.
                FRAMEBUFFER_INPUT_X_HALF(0);

                half4 FragUberPost(Varyings input) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    float2 uv = input.texcoord;

                    // NOTE: Hlsl specifies missing input.a to fill 1 (0 for .rgb).
                    // InputColor is a "bottom" layer for alpha output.
                    half4 inputColor = LOAD_FRAMEBUFFER_X_INPUT(0, input.positionCS.xy);
                    return UberPost(inputColor, uv);
                }
            ENDHLSL
        }

        Pass
        {
            Name "OnTileUberPostMSSoftware"
            LOD 100
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragUberPostMSSoftware
                #pragma target 5.0

                #pragma multi_compile _ _MSAA_2 _MSAA_4 
                #if defined(_MSAA_2)
                #define MSAA_SAMPLES 2
                #elif defined(_MSAA_4)
                #define MSAA_SAMPLES 4
                #else
                #define MSAA_SAMPLES 1
                #endif

                // Declares the framebuffer input as a texture 2d containing half.
                FRAMEBUFFER_INPUT_X_HALF_MS(0);

                half4 MSAAShaderResolveInputAttachment0(float2 pos, const int msaaSamples)
                {
                    half4 inputColor = half4(0.0, 0.0, 0.0, 0.0);

                    UNITY_UNROLL
                    for (int i = 0; i < msaaSamples; ++i) {
                        half4 col = LOAD_FRAMEBUFFER_INPUT_X_MS(0, i, pos);
                        inputColor = inputColor + col;
                    }
                    return inputColor / msaaSamples;
                }

                // Run run at fragment frequency and perform a software resolve of the MSAA
                // color samples and then run the post-process shader the average result. Faster but not as correct
                // as running it per-sample. The resultant output can either be written to a non MSAA surface as it has
                // been resolved or to a MSAA surface where all the samples will end up the same which some hardware
                // resolve operations will optimize due to detecting no difference in a fragment.
                half4 FragUberPostMSSoftware(Varyings input) : SV_Target0
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    float2 uv = input.texcoord;

                    // NOTE: Hlsl specifies missing input.a to fill 1 (0 for .rgb).
                    // InputColor is a "bottom" layer for alpha output.
                    half4 inputColor = MSAAShaderResolveInputAttachment0(input.positionCS.xy, MSAA_SAMPLES);
                    return UberPost(inputColor, uv);
                }
            ENDHLSL
        }

        Pass
        {
            Name "OnTileUberPostTextureReadVersion"
            LOD 100
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragUberPostTextureReadVersion
                #pragma target 5.0
                #pragma enable_d3d11_debug_symbols
                #pragma debug

                // Fallback shader to use when we can't keep things on tile, so usually in the editor when dealing with
                // MSAA source targets to a non MSAA destination we can't perform a software resolve in the shader and
                // so have to fall back to resolving the color target and reading it in as a texture.
                // Where we have a MSAA destination we can avoid this.
                half4 FragUberPostTextureReadVersion(Varyings input) : SV_Target0
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    float2 uv = SCREEN_COORD_APPLY_SCALEBIAS(UnityStereoTransformScreenSpaceTex(input.texcoord));
                    half4 inputColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                    return UberPost(inputColor, uv);
                }
            ENDHLSL
        }

        // Visibility Mesh Versions ------------------------------------------------------------------------------------
        Pass
        {
            Name "OnTileUberPostVisMesh"
            LOD 100
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.universal/Shaders/XR/XRVisibilityMeshHelper.hlsl"
                #pragma vertex VertVisibilityMeshXR
                #pragma fragment FragUberPost

                // Declares the framebuffer input as a texture 2d containing half.
                FRAMEBUFFER_INPUT_X_HALF(0);

                half4 FragUberPost(Varyings input) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    float2 uv = input.texcoord;

                    // NOTE: Hlsl specifies missing input.a to fill 1 (0 for .rgb).
                    // InputColor is a "bottom" layer for alpha output.
                    half4 inputColor = LOAD_FRAMEBUFFER_X_INPUT(0, input.positionCS.xy);
                    return UberPost(inputColor, uv);
                }
            ENDHLSL
        }

        Pass
        {
            Name "OnTileUberPostMSSoftwareVisMesh"
            LOD 100
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.universal/Shaders/XR/XRVisibilityMeshHelper.hlsl"
                #pragma vertex VertVisibilityMeshXR
                #pragma fragment FragUberPostMSSoftware
                #pragma target 5.0

                #pragma multi_compile _ _MSAA_2 _MSAA_4 
                #if defined(_MSAA_2)
                #define MSAA_SAMPLES 2
                #elif defined(_MSAA_4)
                #define MSAA_SAMPLES 4
                #else
                #define MSAA_SAMPLES 1
                #endif

                // Declares the framebuffer input as a texture 2d containing half.
                FRAMEBUFFER_INPUT_X_HALF_MS(0);
                
                half4 MSAAShaderResolveInputAttachment0(float2 pos, const int msaaSamples)
                {
                    half4 inputColor = half4(0.0, 0.0, 0.0, 0.0);

                    UNITY_UNROLL
                    for (int i = 0; i < msaaSamples; ++i) {
                        half4 col = LOAD_FRAMEBUFFER_INPUT_X_MS(0, i, pos);
                        inputColor = inputColor + col;
                    }
                    return inputColor / msaaSamples;
                }

                // Run at fragment frequency and perform a software resolve of the MSAA
                // color samples and then run the post-process shader the average result. Faster but not as correct
                // as running it per-sample. The resultant output can either be written to a non MSAA surface as it has
                // been resolved or to a MSAA surface where all the samples will end up the same which some hardware
                // resolve operations will optimize due to detecting no difference in a fragment.
                half4 FragUberPostMSSoftware(Varyings input) : SV_Target0
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    float2 uv = input.texcoord;

                    // NOTE: Hlsl specifies missing input.a to fill 1 (0 for .rgb).
                    // InputColor is a "bottom" layer for alpha output.
                    half4 inputColor = MSAAShaderResolveInputAttachment0(input.positionCS.xy, MSAA_SAMPLES);
                    return UberPost(inputColor, uv);
                }
            ENDHLSL
        }

        Pass
        {
            Name "OnTileUberPostTextureReadVersionVisMesh"
            LOD 100
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.universal/Shaders/XR/XRVisibilityMeshHelper.hlsl"
                #pragma vertex VertVisibilityMeshXR
                #pragma fragment FragUberPostTextureReadVersion
                #pragma target 5.0
                #pragma debug

                // Fallback shader to use when we can't keep things on tile, so usually in the editor when dealing with
                // MSAA source targets to a non MSAA destination we can't perform a software resolve in the shader and
                // so have to fall back to resolving the color target and reading it in as a texture.
                // Where we have a MSAA destination we can avoid this.
                half4 FragUberPostTextureReadVersion(Varyings input) : SV_Target0
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    float2 uv = SCREEN_COORD_APPLY_SCALEBIAS(UnityStereoTransformScreenSpaceTex(input.texcoord));
                    half4 inputColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                    return UberPost(inputColor, uv);
                }
            ENDHLSL
        }
    }
}
