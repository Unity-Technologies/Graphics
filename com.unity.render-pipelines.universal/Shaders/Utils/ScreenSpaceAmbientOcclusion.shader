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
            half4  positionCS   : SV_POSITION;
            half4  uv           : TEXCOORD0;
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
                #pragma vertex   VertDefault
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
                #pragma vertex   VertDefault
                #pragma fragment SSAO
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 2 - Occlusion estimation with G-Buffer
        Pass
        {
            Name "SSAO_OcclusionWithGBuffer"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment SSAO
                #pragma multi_compile _ APPLY_FORWARD_FOG
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #define SOURCE_GBUFFER
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 3 - Separable blur (horizontal pass) with CameraDepthTexture
        Pass
        {
            Name "SSAO_HorizontalBlurWithCameraDepthTexture"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_DEPTHNORMALS
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 4 - Separable blur (horizontal pass) with CameraDepthNormalsTexture
        Pass
        {
            Name "SSAO_HorizontalBlurWithCameraDepthNormalsTexture"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_DEPTHNORMALS
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 5 - Separable blur (horizontal pass) with G-Buffer
        Pass
        {
            Name "SSAO_GBuffer"

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_GBUFFER
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
                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define BLUR_VERTICAL
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

        // 8 - Final composition (ambient only mode)
        Pass
        {
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragCompositionGBuffer
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        // 9 - Debug overlay
        Pass
        {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragDebugOverlay
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SSAO.hlsl"
            ENDHLSL
        }

        /*Pass
        {
            Name "DepthBlur"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex Vertex
            #pragma fragment FragBoxDownsample

            TEXTURE2D(_ScreenSpaceAOTexture);
            SAMPLER(sampler_ScreenSpaceAOTexture);
            float4 _ScreenSpaceAOTexture_TexelSize;

            float _SampleOffset;

            half4 FragBoxDownsample(Varyings input) : SV_Target
            {
                half4 col = DepthBlur(TEXTURE2D_ARGS(_ScreenSpaceAOTexture, sampler_ScreenSpaceAOTexture), input.uv, _ScreenSpaceAOTexture_TexelSize.xy);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }*/
    }
}
