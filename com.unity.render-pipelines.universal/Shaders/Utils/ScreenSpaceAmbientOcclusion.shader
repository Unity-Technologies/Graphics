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
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #define SOURCE_DEPTH
                #pragma multi_compile_local _RECONSTRUCT_NORMAL_LOW _RECONSTRUCT_NORMAL_MEDIUM _RECONSTRUCT_NORMAL_HIGH
                #pragma multi_compile_local _ _ORTHOGRAPHIC
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 1 - Occlusion estimation with CameraDepthNormalTexture
        Pass
        {
            Name "SSAO_DepthNormals_Occlusion"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #define SOURCE_DEPTH_NORMALS
                #pragma multi_compile_local _ _ORTHOGRAPHIC
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 2 - Occlusion estimation with G-Buffer
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

        // 3 - KawaseBlur
        Pass
        {
            Name "KawaseBlur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment KawaseBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 4 - Separable blur (horizontal pass) with G-Buffer
        Pass
        {
            Name "Horizontal Blur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_GBUFFER
                #define BLUR_HORIZONTAL
                //#define BLUR_SAMPLE_CENTER_NORMAL
                #define SOURCE_DEPTH_NORMALS
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

            ENDHLSL
        }

        // 5 - Separable blur (vertical pass)
        Pass
        {
            Name "Vertical Blur"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define BLUR_VERTICAL
                //#define BLUR_SAMPLE_CENTER_NORMAL
                #define SOURCE_DEPTH_NORMALS
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

            ENDHLSL
        }

        // 6 - Final composition
        Pass
        {
            Name "Final Composition"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragComposition
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"

            ENDHLSL
        }
    }
}
