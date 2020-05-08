Shader "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion"
{
    HLSLINCLUDE
        #pragma exclude_renderers d3d11_9x

        //Keep compiler quiet about Shadows.hlsl.
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        half4 _ScaleBiasRT;
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

            // We need to handle y-flip in a way that all existing shaders using _ProjectionParams.x work.
            // Otherwise we get flipping issues like this one (case https://issuetracker.unity3d.com/issues/lwrp-depth-texture-flipy)

            // Unity flips projection matrix in non-OpenGL platforms and when rendering to a render texture.
            // If URP is rendering to RT:
            //  - Source is upside down.
            // If URP is NOT rendering to RT neither rendering with OpenGL:
            //  - Source Depth is NOT flipped. (ProjectionParams.x == 1)
            output.positionCS = float4(input.positionHCS.xyz, 1.0);
            output.positionCS.y *= _ScaleBiasRT.x;
            output.uv = input.uv;

            // Add a small epsilon to avoid artifacts when reconstructing the normals
            output.uv += 1.0e-6;

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
            Name "SSAO_DepthOnly_Occlusion"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #define SOURCE_DEPTH
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #pragma multi_compile_local _RECONSTRUCT_NORMAL_LOW _RECONSTRUCT_NORMAL_MEDIUM _RECONSTRUCT_NORMAL_HIGH
                #pragma multi_compile_local _ _ORTHOGRAPHIC
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 1 - Separable blur (horizontal pass) with CameraDepthTexture
        Pass
        {
            Name "SSAO_DepthOnly_HorizontalBlur"

            HLSLPROGRAM
                #define SOURCE_DEPTH
                #define BLUR_HORIZONTAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 2 - Separable blur (vertical pass) with CameraDepthTexture
        Pass
        {
            Name "SSAO_DepthOnly_VerticalBlur"

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
            Name "SSAO_DepthNormals_Occlusion"

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
            Name "SSAO_DepthNormals_HorizontalBlur"

            HLSLPROGRAM
                #define SOURCE_DEPTHNORMALS
                #define BLUR_HORIZONTAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 5 - Separable blur (vertical pass) with CameraDepthNormalsTexture
        Pass
        {
            Name "SSAO_DepthNormals_VerticalBlur"

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
            Name "SSAO_GBuffer_Occlusion"

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
            Name "SSAO_GBuffer_HorizontalBlur"

            HLSLPROGRAM
                #define SOURCE_GBUFFER
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define BLUR_HORIZONTAL
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 8 - Separable blur (vertical pass) with G-Buffer
        Pass
        {
            Name "SSAO_GBuffer_VerticalBlur"

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

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragComposition
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 10 - Final composition with G-Buffer
        Pass
        {
            Name "SSAO_GBuffer_FinalComposition"

            HLSLPROGRAM
                #define SOURCE_GBUFFER
                #pragma vertex VertDefault
                #pragma fragment FragCompositionGBuffer
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }
    }
}
