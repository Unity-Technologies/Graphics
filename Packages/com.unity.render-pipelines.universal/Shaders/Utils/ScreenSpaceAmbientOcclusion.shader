Shader "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion"
{
    HLSLINCLUDE
        #pragma editor_sync_compilation
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        Cull Off ZWrite Off ZTest Always

        // ------------------------------------------------------------------
        // Ambient Occlusion
        // ------------------------------------------------------------------

        // 0 - Occlusion estimation
        Pass
        {
            Name "SSAO_Occlusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment SSAO
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
                #pragma multi_compile_local_fragment _INTERLEAVED_GRADIENT _BLUE_NOISE
                #pragma multi_compile_local_fragment _SOURCE_DEPTH_LOW _SOURCE_DEPTH_MEDIUM _SOURCE_DEPTH_HIGH _SOURCE_DEPTH_NORMALS
                #pragma multi_compile_local_fragment _ _ORTHOGRAPHIC
                #pragma multi_compile_local_fragment _SAMPLE_COUNT_LOW _SAMPLE_COUNT_MEDIUM _SAMPLE_COUNT_HIGH
                #pragma multi_compile_fragment _ _FOVEATED_RENDERING_NON_UNIFORM_RASTER

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Bilateral Blur
        // ------------------------------------------------------------------

        // 1 - Horizontal
        Pass
        {
            Name "SSAO_Bilateral_HorizontalBlur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment HorizontalBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 2 - Vertical
        Pass
        {
            Name "SSAO_Bilateral_VerticalBlur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment VerticalBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 3 - Final
        Pass
        {
            Name "SSAO_Bilateral_FinalBlur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FinalBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 4 - After Opaque
        Pass
        {
            Name "SSAO_Bilateral_FinalBlur_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBilateralAfterOpaque

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

                half4 FragBilateralAfterOpaque(Varyings input) : SV_Target
                {
                    half ao = FinalBlur(input).r;
                    return half4(0.0, 0.0, 0.0, ao);
                }

            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Gaussian Blur
        // ------------------------------------------------------------------

        // 5 - Horizontal
        Pass
        {
            Name "SSAO_Gaussian_HorizontalBlur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment HorizontalGaussianBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 6 - Vertical
        Pass
        {
            Name "SSAO_Gaussian_VerticalBlur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment VerticalGaussianBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 7 - After Opaque
        Pass
        {
            Name "SSAO_Gaussian_VerticalBlur_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragGaussianAfterOpaque

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

                half4 FragGaussianAfterOpaque(Varyings input) : SV_Target
                {
                    half ao = VerticalGaussianBlur(input);
                    return half4(0.0, 0.0, 0.0, ao);
                }

            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Kawase Blur
        // ------------------------------------------------------------------

        // 8 - Kawase Blur
        Pass
        {
            Name "SSAO_Kawase"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment KawaseBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 9 - After Opaque Kawase
        Pass
        {
            Name "SSAO_Kawase_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragKawaseAfterOpaque

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

                half4 FragKawaseAfterOpaque(Varyings input) : SV_Target
                {
                    half ao = KawaseBlur(input);
                    return half4(0.0, 0.0, 0.0, ao);
                }

            ENDHLSL
        }
    }
}
