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
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

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
            color.rgb = OETF(color.rgb, MaxNits);
#endif
            return color;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" }

        // 0: Nearest
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNearestHDR

                float4 FragNearestHDR(Varyings input) : SV_Target
                {
                    float4 color = FragNearest(input);
                    float4 uiSample = SAMPLE_TEXTURE2D_X(_OverlayUITexture, sampler_PointClamp, input.texcoord);
                    return SceneComposition(color, uiSample);
                }
            ENDHLSL
        }

        // 1: Bilinear
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBilinearHDR

                float4 FragBilinearHDR(Varyings input) : SV_Target
                {                                                           
                    float4 color = FragBilinear(input);
                    float4 uiSample = SAMPLE_TEXTURE2D_X(_OverlayUITexture, sampler_PointClamp, input.texcoord);
                    return SceneComposition(color, uiSample);
                }
            ENDHLSL
        }
    }

    Fallback Off
}
