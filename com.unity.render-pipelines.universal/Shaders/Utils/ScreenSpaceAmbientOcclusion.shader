Shader "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion"
{
    HLSLINCLUDE
        #pragma editor_sync_compilation
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionHCS   : POSITION;
            float2 uv           : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4  positionCS  : SV_POSITION;
            float2  uv          : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings VertDefault(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // Note: The pass is setup with a mesh already in CS
            // Therefore, we can just output vertex position
            output.positionCS = float4(input.positionHCS.xyz, 1.0);

            #if UNITY_UV_STARTS_AT_TOP
            output.positionCS.y *= _ScaleBiasRt.x;
            #endif

            output.uv = input.uv;

            // Add a small epsilon to avoid artifacts when reconstructing the normals
            output.uv += 1.0e-6;

            return output;
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        Cull Off ZWrite Off ZTest Always

        // ------------------------------------------------------------------
        // Depth only passes
        // ------------------------------------------------------------------

        // 0 - Occlusion estimation with CameraDepthTexture
        Pass
        {
            Name "SSAO_Occlusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
                #pragma multi_compile_local _SOURCE_DEPTH _SOURCE_DEPTH_NORMALS
                #pragma multi_compile_local _RECONSTRUCT_NORMAL_LOW _RECONSTRUCT_NORMAL_MEDIUM _RECONSTRUCT_NORMAL_HIGH
                #pragma multi_compile_local _BLUE_NOISE _KEIJIRO _OLD_BLUE_NOISE _OLD
                #pragma multi_compile_local _ _ONLY_AO
                #pragma multi_compile_local _ _ORTHOGRAPHIC
                #pragma multi_compile_local _SAMPLE_COUNT4 _SAMPLE_COUNT6 _SAMPLE_COUNT8 _SAMPLE_COUNT10 _SAMPLE_COUNT12

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 1 - Horizontal Blur
        Pass
        {
            Name "SSAO_HorizontalBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment HorizontalBlur
                #define BLUR_SAMPLE_CENTER_NORMAL  // TODO: this is causing extra fp32 operations on mobile. In general makes blur more expensive. Remove it?
                #pragma multi_compile_local _ _ORTHOGRAPHIC
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
                #pragma multi_compile_local _SOURCE_DEPTH _SOURCE_DEPTH_NORMALS
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 2 - Vertical Blur
        Pass
        {
            Name "SSAO_VerticalBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment VerticalBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 3 - Final Blur
        Pass
        {
            Name "SSAO_FinalBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FinalBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 4 - Horizontal + Vertical Blur
        Pass
        {
            Name "SSAO_HorizontalVerticalBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment HorizontalVerticalBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // GAUSSIAN BLUR

        // 5 - Horizontal  Gaussian Blur
        Pass
        {
            Name "SSAO_HorizontalBlurGaussian"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment HorizontalGaussianBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 6 - Vertical Gaussian Blur
        Pass
        {
            Name "SSAO_VerticalBlurGaussian"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment VerticalGaussianBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 7 - Horizontal + Vertical Gaussian Blur
        Pass
        {
            Name "SSAO_HorizontalVerticalBlurGaussian"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment HorizontalVerticalGaussianBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 8 - Upsample
        Pass
        {
            Name "SSAO_Upsample"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment Upsample
                #pragma target 4.5
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 9 - Kawase Blur
        Pass
        {
            Name "SSAO_Kawase"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment KawaseBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 10 - Dual Kawase Blur
        Pass
        {
            Name "SSAO_DualKawase"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment DualKawaseBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 11 - Dual Filtering Downsample
        Pass
        {
            Name "SSAO_DualFilteringDownsample"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment DualFilteringDownsample
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 12 - Dual Filtering Upsample
        Pass
        {
            Name "SSAO_DualFilteringUpsample"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment DualFilteringUpsample
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 4 - After Opaque
        Pass
        {
            Name "SSAO_AfterOpaque"

            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One SrcAlpha, Zero One
            BlendOp Add, Add

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragAfterOpaque
                #define _SCREEN_SPACE_OCCLUSION

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

                half4 FragAfterOpaque(Varyings input) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(input.uv);
                    half occlusion = aoFactor.indirectAmbientOcclusion;
                    return half4(0.0, 0.0, 0.0, occlusion);
                }

            ENDHLSL
        }
    }
}
