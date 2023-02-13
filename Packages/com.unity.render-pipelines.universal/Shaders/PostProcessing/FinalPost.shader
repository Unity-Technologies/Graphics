Shader "Hidden/Universal Render Pipeline/FinalPost"
{
    HLSLINCLUDE
        #pragma multi_compile_local_fragment _ _POINT_SAMPLING _RCAS
        #pragma multi_compile_local_fragment _ _FXAA
        #pragma multi_compile_local_fragment _ _FILM_GRAIN
        #pragma multi_compile_local_fragment _ _DITHERING
        #pragma multi_compile_local_fragment _ _LINEAR_TO_SRGB_CONVERSION
        #pragma multi_compile_fragment _ DEBUG_DISPLAY
        #pragma multi_compile_fragment _ SCREEN_COORD_OVERRIDE
        #pragma multi_compile_local_fragment _ HDR_COLORSPACE_CONVERSION HDR_ENCODING HDR_COLORSPACE_CONVERSION_AND_ENCODING

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ScreenCoordOverride.hlsl"
#if defined(HDR_COLORSPACE_CONVERSION) || defined(HDR_ENCODING) || defined(HDR_COLORSPACE_CONVERSION_AND_ENCODING)
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"
#endif
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D(_Grain_Texture);
        TEXTURE2D(_BlueNoise_Texture);
        TEXTURE2D_X(_OverlayUITexture);

        float4 _SourceSize;
        float2 _Grain_Params;
        float4 _Grain_TilingParams;
        float4 _Dithering_Params;
        float4 _HDROutputLuminanceParams;

        #if SHADER_TARGET >= 45
            #define FSR_INPUT_TEXTURE _BlitTexture
            #define FSR_INPUT_SAMPLER sampler_LinearClamp

            #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/FSRCommon.hlsl"
        #endif

        #define GrainIntensity          _Grain_Params.x
        #define GrainResponse           _Grain_Params.y
        #define GrainScale              _Grain_TilingParams.xy
        #define GrainOffset             _Grain_TilingParams.zw

        #define DitheringScale          _Dithering_Params.xy
        #define DitheringOffset         _Dithering_Params.zw

        #define MinNits                 _HDROutputLuminanceParams.x
        #define MaxNits                 _HDROutputLuminanceParams.y
        #define PaperWhite              _HDROutputLuminanceParams.z

        half4 FragFinalPost(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float2 positionNDC = uv;
            int2   positionSS  = uv * _SourceSize.xy;

            #if _POINT_SAMPLING
            half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).xyz;
            #elif _RCAS && SHADER_TARGET >= 45
            half3 color = ApplyRCAS(positionSS);
            // When Unity is configured to use gamma color encoding, we must convert back from linear after RCAS is performed.
            // (The input color data for this shader variant is always linearly encoded because RCAS requires it)
            #if UNITY_COLORSPACE_GAMMA
            color = GetLinearToSRGB(color);
            #endif
            #else
            half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).xyz;
            #endif

            #if _FXAA
            {
                color = ApplyFXAA(color, positionNDC, positionSS, _SourceSize, _BlitTexture);
            }
            #endif

            #if _FILM_GRAIN
            {
                color = ApplyGrain(color, SCREEN_COORD_APPLY_SCALEBIAS(positionNDC), TEXTURE2D_ARGS(_Grain_Texture, sampler_LinearRepeat), GrainIntensity, GrainResponse, GrainScale, GrainOffset);
            }
            #endif

            #if _LINEAR_TO_SRGB_CONVERSION
            {
                color = LinearToSRGB(color);
            }
            #endif

            #if _DITHERING
            {
                color = ApplyDithering(color, SCREEN_COORD_APPLY_SCALEBIAS(positionNDC), TEXTURE2D_ARGS(_BlueNoise_Texture, sampler_PointRepeat), DitheringScale, DitheringOffset);
            }
            #endif

            #ifdef HDR_COLORSPACE_CONVERSION
            {
                color.rgb = RotateRec709ToOutputSpace(color.rgb) * PaperWhite;
            }
            #endif

            #ifdef HDR_ENCODING
            {
                float4 uiSample = SAMPLE_TEXTURE2D_X(_OverlayUITexture, sampler_PointClamp, input.texcoord);
                color.rgb = SceneUIComposition(uiSample, color.rgb, PaperWhite, MaxNits);
                color.rgb = OETF(color.rgb);
            }
            #endif

            half4 finalColor = half4(color, 1);

            #if defined(DEBUG_DISPLAY)
            half4 debugColor = 0;

            if(CanDebugOverrideOutputColor(finalColor, uv, debugColor))
            {
                return debugColor;
            }
            #endif

            return finalColor;
        }

    ENDHLSL

    /// Standard FinalPost shader variant with support for FSR
    /// Note: FSR requires shader target 4.5 because it relies on texture gather instructions
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "FinalPost"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragFinalPost
                #pragma target 4.5
            ENDHLSL
        }
    }

    /// Fallback version of FinalPost shader which lacks support for FSR
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "FinalPost"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragFinalPost
            ENDHLSL
        }
    }
}
