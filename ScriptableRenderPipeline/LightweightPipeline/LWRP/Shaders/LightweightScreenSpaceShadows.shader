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

        TEXTURE2D(_CameraDepthTexture);
        SAMPLER(sampler_CameraDepthTexture);

        struct VertexInput
        {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
            uint   id     : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Interpolators
        {
            half4  pos          : SV_POSITION;
            half2  uv           : TEXCOORD0;
            
            //Perspective Case
            float3 ray          : TEXCOORD1;

            //Orthographic Case
            float3 orthoPosNear : TEXCOORD2;
            float3 orthoPosFar  : TEXCOORD3;
            
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float3 ComputeViewSpacePositionGeometric(Interpolators i)
        {
            float zDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);
            float depth  = Linear01Depth(zDepth, _ZBufferParams);

        #ifdef UNITY_REVERSED_Z
            zDepth = 1 - zDepth;
        #endif

            //Perspective Case
            float3 vposPersp = i.ray * depth;

            //Orthographics Case
            float3 vposOrtho = lerp(i.orthoPosNear, i.orthoPosFar, zDepth);

            return lerp(vposPersp, vposOrtho, unity_OrthoParams.w);
        }

        Interpolators Vertex(VertexInput i)
        {
            Interpolators o;
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_TRANSFER_INSTANCE_ID(i, o);

            o.pos = TransformObjectToHClip(i.vertex.xyz);
            o.uv  = i.uv;

            //Perspective Case
            o.ray = _FrustumCorners[i.id].xyz; 

            //Orthographic Case
            float4 clipPos = o.pos;
            clipPos.y *= _ProjectionParams.x;
            float3 orthoPosNear = mul(unity_CameraInvProjection, float4(clipPos.x, clipPos.y, -1, 1)).xyz;
            float3 orthoPosFar  = mul(unity_CameraInvProjection, float4(clipPos.x, clipPos.y,  1, 1)).xyz;
            orthoPosNear.z *= -1;
            orthoPosFar.z  *= -1;
            o.orthoPosNear = orthoPosNear;
            o.orthoPosFar  = orthoPosFar;

            return o;
        }

        half Fragment(Interpolators i) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(i);

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