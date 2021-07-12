Shader "Hidden/Debug/PlanarReflectionProbePreview"
{
    Properties
    {
        _MainTex("_MainTex", 2D) = "white" {}
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

            #pragma editor_sync_compilation

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
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

            TEXTURE2D(_MainTex);

            float4x4 _CaptureVPMatrix;
            float3 _CapturePositionWS;
            float3 _CameraPositionWS;
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
                float3 viewDirWS = _CameraPositionWS - i.positionWS;
                float3 reflectViewDirWS = reflect(-viewDirWS, i.normalWS);
                float3 projectedPositionCaptureSpace = i.positionWS + normalize(reflectViewDirWS) * 65504 - _CapturePositionWS;
                float3 ndc = ComputeNormalizedDeviceCoordinatesWithZ(projectedPositionCaptureSpace, _CaptureVPMatrix);
                float4 color = SAMPLE_TEXTURE2D_LOD(_MainTex, s_trilinear_clamp_sampler, ndc.xy, _MipLevel);
                color.a = any(ndc.xyz < 0) || any(ndc.xyz > 1) ? 0.0 : 1.0;
                color.rgb *= color.a;

                color = color * exp2(_Exposure) * GetCurrentExposureMultiplier();

                return float4(color);
            }
            ENDHLSL
        }
    }
}
