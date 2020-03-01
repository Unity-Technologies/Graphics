Shader "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion"
{
    HLSLINCLUDE
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x

        //Keep compiler quiet about Shadows.hlsl.
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    
        struct Attributes
        {
            float4 positionOS   : POSITION;
            float2 uv     : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4  positionCS   : SV_POSITION;
            float4  uv           : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings VertDefault(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

            float4 projPos = output.positionCS * 0.5;
            projPos.xy = projPos.xy + projPos.w;

            output.uv.xy = UnityStereoTransformScreenSpaceTex(input.uv);
            output.uv.zw = projPos.xy;

            return output;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // ------------------------------------------------------------------
        // Depth only passes
        // ------------------------------------------------------------------
        
        // 0 - Occlusion estimation with CameraDepthTexture
        Pass
        {
            Name "SSAO_OcclusionWithCameraDepthTexture"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #define SOURCE_DEPTH
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 1 - Separable blur (horizontal pass) with CameraDepthTexture
        Pass
        {
            Name "SSAO_HorizontalBlurWithCameraDepthTexture"

            HLSLPROGRAM
                #define SOURCE_DEPTH
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 2 - Separable blur (vertical pass) with CameraDepthTexture
        Pass
        {
            Name "SSAO_VerticalBlurWithCameraDepthTexture"
            
            HLSLPROGRAM
                #define SOURCE_DEPTH
                #define BLUR_VERTICAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Depth & Normals passes
        // ------------------------------------------------------------------

        // 3 - Occlusion estimation with CameraDepthNormalTexture
        Pass
        {
            Name "SSAO_OcclusionWithCameraDepthNormalTexture"

            HLSLPROGRAM
                #define SOURCE_DEPTH_NORMALS
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 4 - Separable blur (horizontal pass) with CameraDepthNormalsTexture
        Pass
        {
            Name "SSAO_HorizontalBlurWithCameraDepthNormalsTexture"

            HLSLPROGRAM
                #define SOURCE_DEPTHNORMALS
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 5 - Separable blur (vertical pass) with CameraDepthNormalsTexture
        Pass
        {
            Name "SSAO_VerticalBlurWithCameraDepthNormalsTexture"
            
            HLSLPROGRAM
                #define SOURCE_DEPTHNORMALS
                #define BLUR_VERTICAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // GBuffer only passes
        // ------------------------------------------------------------------

        // 6 - Occlusion estimation with G-Buffer
        Pass
        {
            Name "SSAO_OcclusionWithGBuffer"

            HLSLPROGRAM
                #define SOURCE_GBUFFER
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 7 - Separable blur (horizontal pass) with G-Buffer
        Pass
        {
            Name "SSAO_GBuffer"

            HLSLPROGRAM
                #define SOURCE_GBUFFER
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 8 - Separable blur (vertical pass) with G-Buffer
        Pass
        {
            Name "SSAO_VerticalBlurWithCameraDepthNormalsTexture"
            
            HLSLPROGRAM
                #define SOURCE_GBUFFER
                #define BLUR_VERTICAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Final Composition passes
        // ------------------------------------------------------------------

        // 9 - Final composition
        Pass
        {
            Name "SSAO_FinalComposition"
            //Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragComposition
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 10 - Final composition with G-Buffer
        Pass
        {
            Name "SSAO_FinalComposition"
            //Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM
                #define SOURCE_GBUFFER
                #pragma vertex VertDefault
                #pragma fragment FragCompositionGBuffer
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }
    }
}
