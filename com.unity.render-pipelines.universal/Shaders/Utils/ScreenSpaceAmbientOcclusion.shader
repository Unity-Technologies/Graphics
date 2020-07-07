Shader "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion"
{
    HLSLINCLUDE
        #pragma exclude_renderers d3d11_9x

        //Keep compiler quiet about Shadows.hlsl.
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        half4 _ScaleBiasRt;
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
            output.positionCS.y *= _ScaleBiasRt.x;
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
                #pragma multi_compile_local _SOURCE_DEPTH _SOURCE_DEPTH_NORMALS _SOURCE_GBUFFER
                #pragma multi_compile_local _RECONSTRUCT_NORMAL_LOW _RECONSTRUCT_NORMAL_MEDIUM _RECONSTRUCT_NORMAL_HIGH
                #pragma multi_compile_local _ _ORTHOGRAPHIC
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
                #define BLUR_SAMPLE_CENTER_NORMAL
                #pragma multi_compile_local _ _ORTHOGRAPHIC
                #pragma multi_compile_local _SOURCE_DEPTH _SOURCE_DEPTH_NORMALS _SOURCE_GBUFFER
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
    }
}
