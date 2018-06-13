Shader "Hidden/LightweightPipeline/ScreenSpaceShadows"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}

        HLSLINCLUDE

        // Note: Screenspace shadow resolve is only performed when shadow cascades are enabled
        // Shadow cascades require cascade index and shadowCoord to be computed on pixel.
        #define _SHADOWS_CASCADE

        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        //Keep compiler quiet about Shadows.hlsl.
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "CoreRP/ShaderLibrary/EntityLighting.hlsl"
        #include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "LWRP/ShaderLibrary/Core.hlsl"
        #include "LWRP/ShaderLibrary/Shadows.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
        TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
        TEXTURE2D_FLOAT(_CameraDepthTexture);
#endif

        SAMPLER(sampler_CameraDepthTexture);

        struct VertexInput
        {
            float4 vertex   : POSITION;
            float2 texcoord : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Interpolators
        {
            half4  pos      : SV_POSITION;
            half4  texcoord : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Interpolators Vertex(VertexInput i)
        {
            Interpolators o;
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.pos = TransformObjectToHClip(i.vertex.xyz);

            float4 projPos = o.pos * 0.5;
            projPos.xy = projPos.xy + projPos.w;

            o.texcoord.xy = UnityStereoTransformScreenSpaceTex(i.texcoord);
            o.texcoord.zw = projPos.xy;

            return o;
        }

        half4 Fragment(Interpolators i) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
            float deviceDepth = SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord.xy, unity_StereoEyeIndex).r;
#else
            float deviceDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord.xy);
#endif

#if UNITY_REVERSED_Z
            deviceDepth = 1 - deviceDepth;
#endif
            deviceDepth = 2 * deviceDepth - 1; //NOTE: Currently must massage depth before computing CS position.

            float3 vpos = ComputeViewSpacePosition(i.texcoord.zw, deviceDepth, unity_CameraInvProjection);
            float3 wpos = mul(unity_CameraToWorld, float4(vpos, 1)).xyz;

            //Fetch shadow coordinates for cascade.
            float4 coords = TransformWorldToShadowCoord(wpos);

            // Screenspace shadowmap is only used for directional lights which use orthogonal projection.
            ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
            half shadowStrength = GetMainLightShadowStrength();
            return SampleShadowmap(coords, TEXTURE2D_PARAM(_DirectionalShadowmapTexture, sampler_DirectionalShadowmapTexture), shadowSamplingData, shadowStrength);
        }

        ENDHLSL

        Pass
        {
            Name "Default"
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
