Shader "Hidden/Universal/CoreBlit"
{
    HLSLINCLUDE

        #pragma target 2.0
        #pragma editor_sync_compilation
        #pragma multi_compile _ DISABLE_TEXTURE2D_X_ARRAY
        #pragma multi_compile _ BLIT_SINGLE_SLICE
        // Core.hlsl for XR dependencies
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        // DebuggingFullscreen.hlsl for URP debug draw
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
        // Color.hlsl for color space conversion
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        // Specialized blit with URP debug draw support and color space conversion support
        // Keep in sync with BlitHDROverlay.shader
        half4 FragmentURPBlit(Varyings input, SamplerState blitsampler)
        {
            half4 color = FragBlit(input, blitsampler);

            #ifdef _LINEAR_TO_SRGB_CONVERSION
            color = LinearToSRGB(color);
            #endif

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

        // 0: Nearest
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 1: Bilinear
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBilinear
            ENDHLSL
        }

        // 2: Nearest quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 3: Bilinear quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinear
            ENDHLSL
        }

        // 4: Nearest quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearest
            ENDHLSL
        }

        // 5: Bilinear quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinear
            ENDHLSL
        }

        // 6: Nearest quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearestRepeat
            ENDHLSL
        }

        // 7: Bilinear quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinearRepeat
            ENDHLSL
        }

        // 8: Bilinear quad with padding (for OctahedralTexture)
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragOctahedralBilinearRepeat
            ENDHLSL
        }

        /// Version 4, 5, 6, 7 with Alpha Blending 0.5
        // 9: Nearest quad with padding alpha blend (4 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearest
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 10: Bilinear quad with padding alpha blend (5 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinear
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 11: Nearest quad with padding alpha blend (6 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragNearestRepeat
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 12: Bilinear quad with padding alpha blend (7 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragBilinearRepeat
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 13: Bilinear quad with padding alpha blend (for OctahedralTexture) (8 with alpha blend)
        Pass
        {
            ZWrite Off ZTest Always Blend DstColor Zero Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuadPadding
                #pragma fragment FragOctahedralBilinearRepeat
                #define WITH_ALPHA_BLEND
            ENDHLSL
        }

        // 14. Project Cube to Octahedral 2d quad
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragOctahedralProject
            ENDHLSL
        }

        // 15. Project Cube to Octahedral 2d quad with luminance (grayscale), RGBA to YYYY
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragOctahedralProjectLuminance
            ENDHLSL
        }

        // 16. Project Cube to Octahedral 2d quad with with A to RGBA (AAAA)
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragOctahedralProjectAlphaToRGBA
            ENDHLSL
        }

        // 17. Project Cube to Octahedral 2d quad with with R to RGBA (RRRR)
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragOctahedralProjectRedToRGBA
            ENDHLSL
        }

        // 18. Bilinear quad with luminance (grayscale), RGBA to YYYY
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinearLuminance
            ENDHLSL
        }

        // 19. Bilinear quad with A to RGBA (AAAA)
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinearAlphaToRGBA
            ENDHLSL
        }

        // 20. Bilinear quad with R to RGBA (RRRR)
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex VertQuad
                #pragma fragment FragBilinearRedToRGBA
            ENDHLSL
        }

        // 21. Nearest project cube to octahedral 2d quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma multi_compile_local _ BLIT_DECODE_HDR
                #pragma vertex VertQuadPadding
                #pragma fragment FragOctahedralProjectNearestRepeat
            ENDHLSL
        }

        // 22. Bilinear project cube to octahedral 2d quad with padding
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma multi_compile_local _ BLIT_DECODE_HDR
                #pragma vertex VertQuadPadding
                #pragma fragment FragOctahedralProjectBilinearRepeat
            ENDHLSL
        }

        // 23: Bilinear blit with debug draw and color space conversion support
        Pass
        {
            Name "BilinearDebugDraw"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentURPBlitBilinearSampler
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            half4 FragmentURPBlitBilinearSampler(Varyings input) : SV_Target
            {
                return FragmentURPBlit(input, sampler_LinearClamp);
            }
            ENDHLSL
        }

        // 24: Nearest blit with debug draw and color space conversion support
        Pass
        {
            Name "NearestDebugDraw"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentURPBlitPointSampler
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            half4 FragmentURPBlitPointSampler(Varyings input) : SV_Target
            {
                return FragmentURPBlit(input, sampler_PointClamp);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
