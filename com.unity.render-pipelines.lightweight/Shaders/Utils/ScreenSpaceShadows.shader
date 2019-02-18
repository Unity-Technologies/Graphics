Shader "Hidden/Lightweight Render Pipeline/ScreenSpaceShadows"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}

        HLSLINCLUDE

        // Note: Screenspace shadow resolve is only performed when shadow cascades are enabled
        // Shadow cascades require cascade index and shadowCoord to be computed on pixel.
        #define _MAIN_LIGHT_SHADOWS_CASCADE

        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        //Keep compiler quiet about Shadows.hlsl.
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Shadows.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
        TEXTURE2D_FLOAT(_CameraDepthTexture);
#endif

        SAMPLER(sampler_CameraDepthTexture);

        struct Attributes
        {
            float4 positionOS   : POSITION;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            half4  positionCS   : SV_POSITION;
            half4  uv           : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vertex(Attributes input)
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

        half4 Fragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            float deviceDepth = SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy, unity_StereoEyeIndex).r;
#else
            float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv.xy);
#endif

#if UNITY_REVERSED_Z
            deviceDepth = 1 - deviceDepth;
#endif
            deviceDepth = 2 * deviceDepth - 1; //NOTE: Currently must massage depth before computing CS position.

            float3 vpos = ComputeViewSpacePosition(input.uv.zw, deviceDepth, unity_CameraInvProjection);
            float3 wpos = mul(unity_CameraToWorld, float4(vpos, 1)).xyz;

            //Fetch shadow coordinates for cascade.
            float4 coords = TransformWorldToShadowCoord(wpos);

            // Screenspace shadowmap is only used for directional lights which use orthogonal projection.
            ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
            half shadowStrength = GetMainLightShadowStrength();
            return SampleShadowmap(coords, TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowSamplingData, shadowStrength, false);
        }

        ENDHLSL

        Pass
        {
            Name "ScreenSpaceShadows"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex   Vertex
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}
