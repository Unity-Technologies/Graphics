Shader "Hidden/Universal/BlitHDROverlay"
{
    HLSLINCLUDE
        #pragma target 2.0
        #pragma editor_sync_compilation
        #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY
        #pragma multi_compile _ BLIT_SINGLE_SLICE
        #pragma multi_compile_local_fragment _ HDR_COLORSPACE_CONVERSION HDR_ENCODING HDR_COLORSPACE_CONVERSION_AND_ENCODING

        // Core.hlsl for XR dependencies
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        // Color.hlsl and HDROutput.hlsl for color space conversion and encoding
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"
        // DebuggingFullscreen.hlsl for URP debug draw
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"

        TEXTURE2D_X(_OverlayUITexture);

        float4 _HDROutputLuminanceParams;

        #define MinNits    _HDROutputLuminanceParams.x
        #define MaxNits    _HDROutputLuminanceParams.y
        #define PaperWhite _HDROutputLuminanceParams.z

        float4 SceneComposition(float4 color, float4 uiSample)
        {
#if defined(HDR_COLORSPACE_CONVERSION)
            color.rgb = RotateRec709ToOutputSpace(color.rgb) * PaperWhite;
#endif

#if defined(HDR_ENCODING)
            color.rgb = SceneUIComposition(uiSample, color.rgb, PaperWhite, MaxNits);
            color.rgb = OETF(color.rgb);
#endif
            return color;
        }

        float4 FragBlitHDR(Varyings input, SamplerState s)
        {
            float4 color = FragBlit(input, s);

            float4 uiSample = SAMPLE_TEXTURE2D_X(_OverlayUITexture, sampler_PointClamp, input.texcoord);
            return SceneComposition(color, uiSample);
        }

        // Specialized blit with URP debug draw support and UI overlay support for HDR output
        // Keep in sync with CoreBlit.shader
        half4 FragmentURPBlitHDR(Varyings input, SamplerState blitsampler)
        {
            half4 color = FragBlitHDR(input, blitsampler);
            
            #if defined(DEBUG_DISPLAY)
            half4 debugColor = 0;
            float2 uv = input.texcoord;
            if (CanDebugOverrideOutputColor(color, uv, debugColor))
            {
                return debugColor;
            }
            #endif

            return color;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }
        
        // 0: Bilinear blit with debug draw support
        Pass
        {
            Name "BilinearDebugDraw"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentURPBlitBilinearSampler
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            half4 FragmentURPBlitBilinearSampler(Varyings input) : SV_Target
            {
                return FragmentURPBlitHDR(input, sampler_LinearClamp);
            }
            ENDHLSL
        }

        // 1: Nearest blit with debug draw support
        Pass
        {
            Name "NearestDebugDraw"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentURPBlitPointSampler
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            half4 FragmentURPBlitPointSampler(Varyings input) : SV_Target
            {
                return FragmentURPBlitHDR(input, sampler_PointClamp);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
