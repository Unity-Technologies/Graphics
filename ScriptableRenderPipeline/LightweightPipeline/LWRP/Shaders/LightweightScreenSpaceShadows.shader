Shader "Hidden/LightweightPipeline/ScreenSpaceShadows"
{
    SubShader
    {
        Tags {}

        HLSLINCLUDE

        //Keep compiler quiet about Shadows.hlsl. 
        #include "CoreRP/ShaderLibrary/Common.hlsl"
        #include "CoreRP/ShaderLibrary/EntityLighting.hlsl"
        #include "CoreRP/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "LWRP/ShaderLibrary/Core.hlsl"
        #include "LWRP/ShaderLibrary/Shadows.hlsl"

        //Scene Depth
        TEXTURE2D(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);

        struct VertexInput
        {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
            uint   id     : SV_VertexID;
        };

        struct Interpolators
        {
            half4  pos    : SV_POSITION;
            half2  uv     : TEXCOORD0;
            float3 ray    : TEXCOORD1;
        };

        float3 ComputeViewSpacePositionGeometric(Interpolators i)
        {
            float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);
            return i.ray * Linear01Depth(depth, _ZBufferParams);
        }

        Interpolators Vertex(VertexInput i)
        {
            Interpolators o;
            o.pos = TransformObjectToHClip(i.vertex.xyz);
            o.uv  = i.uv;
            o.ray = _FrustumCorners[i.id].xyz; 
            return o;
        }

        half Fragment(Interpolators i) : SV_Target
        {
            //Reconstruct the world position.
            float3 vpos = ComputeViewSpacePositionGeometric(i); //TODO: Profile against unprojection method in core library.
            float3 wpos = mul(unity_CameraToWorld, float4(vpos, 1)).xyz;
            
            //Fetch shadow coordinates.
            float4 coords  = ComputeShadowCoord(wpos);

            return SampleShadowmap(coords);
        }

        ENDHLSL

        Pass
        {
            ZTest Always ZWrite Off

            HLSLPROGRAM

            // -------------------------------------
            // We have no good approach exposed to skip shader variants, e.g, ideally we would like to skip _CASCADE for all puctual lights
            // Lightweight combines light classification and shadows keywords to reduce shader variants.
            // Lightweight shader library declares defines based on these keywords to avoid having to check them in the shaders
            // Core.hlsl defines _MAIN_LIGHT_DIRECTIONAL and _MAIN_LIGHT_SPOT (point lights can't be main light)
            // Shadow.hlsl defines _SHADOWS_ENABLED, _SHADOWS_SOFT, _SHADOWS_CASCADE, _SHADOWS_PERSPECTIVE
            #pragma multi_compile _ _MAIN_LIGHT_DIRECTIONAL_SHADOW _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE _MAIN_LIGHT_DIRECTIONAL_SHADOW_SOFT _MAIN_LIGHT_DIRECTIONAL_SHADOW_CASCADE_SOFT _MAIN_LIGHT_SPOT_SHADOW _MAIN_LIGHT_SPOT_SHADOW_SOFT
            
            #pragma vertex   Vertex
            #pragma fragment Fragment
            ENDHLSL
        }
    }
}