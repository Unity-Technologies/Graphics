Shader "Hidden/Debug/ReflectionProbePreview"
{
    Properties
    {
        _Cubemap("_Cubemap", Cube) = "white" {}
        _CameraWorldPosition("_CameraWorldPosition", Vector) = (1,1,1,1)
        _MipLevel("_MipLevel", Range(0.0,7.0)) = 0.0
        _Exposure("_Exposure", Range(-10.0,10.0)) = 0.0

    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" "Queue" = "Transparent" }
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode" = "Forward" }

            HLSLPROGRAM

            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma editor_sync_compilation

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD0;
            };

            TEXTURECUBE(_Cubemap);
            SAMPLER(sampler_Cubemap);
            float4 _Cubemap_HDR;

            float3 _CameraWorldPosition;
            float _MipLevel;
            float _Exposure;

            v2f vert(appdata v)
            {
                v2f o;
                // Transform local to world before custom vertex code
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 V = normalize(i.positionWS - GetPrimaryCameraPosition());
                float3 R = reflect(V, normalize(i.normalWS));
                float4 color = SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, R, _MipLevel).rgba;
                color.rgb = DecodeHDREnvironment(color, _Cubemap_HDR);
                color = color * exp2(_Exposure) * GetCurrentExposureMultiplier();

                return float4(color);
            }
            ENDHLSL
        }
    }
}
