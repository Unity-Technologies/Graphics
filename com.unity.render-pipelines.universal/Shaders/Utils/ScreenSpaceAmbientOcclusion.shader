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
            float2 texcoord     : TEXCOORD0;

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

            output.uv.xy = UnityStereoTransformScreenSpaceTex(input.texcoord);
            output.uv.zw = projPos.xy;

            return output;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

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

        // 1 - Occlusion estimation with CameraDepthNormalTexture
        Pass
        {
            Name "SSAO_OcclusionWithCameraDepthNormalTexture"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
                #define SOURCE_DEPTH_NORMALS
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 2 - Occlusion estimation with G-Buffer
        Pass
        {
            Name "SSAO_OcclusionWithGBuffer"

            HLSLPROGRAM
                #define SOURCE_GBUFFER
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #pragma multi_compile _ APPLY_FORWARD_FOG
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 3 - Separable blur (horizontal pass) with CameraDepthTexture
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

        // 5 - Separable blur (horizontal pass) with G-Buffer
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

        // 6 - Separable blur (vertical pass)
        Pass
        {
            Name "SSAO_VerticalBlurWithCameraDepthNormalsTexture"
            
            HLSLPROGRAM
                #define BLUR_VERTICAL
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 7 - Final composition
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
    }
}
