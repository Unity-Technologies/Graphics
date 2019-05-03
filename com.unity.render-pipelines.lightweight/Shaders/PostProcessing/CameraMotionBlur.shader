Shader "Hidden/Lightweight Render Pipeline/CameraMotionBlur"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
    }

    HLSLINCLUDE

        #pragma prefer_hlslcc gles

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.lightweight/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D(_MainTex);
        TEXTURE2D_FLOAT(_CameraDepthTexture);

        float4x4 _ViewProjM;
        float4x4 _PrevViewProjM;
        float _Intensity;
        float _Clamp;
        float4 _MainTex_TexelSize;

        struct VaryingsCMB
        {
            float4 positionCS    : SV_POSITION;
            float4 uv            : TEXCOORD0;
        };

        VaryingsCMB VertCMB(Attributes input)
        {
            VaryingsCMB output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

            float4 projPos = output.positionCS * 0.5;
            projPos.xy = projPos.xy + projPos.w;

            output.uv.xy = input.uv;
            output.uv.zw = projPos.xy;

            return output;
        }

        float2 ClampVelocity(float2 velocity, float maxVelocity)
        {
            float len = length(velocity);
            return (len > 0.0) ? min(len, maxVelocity) * (velocity * rcp(len)) : 0.0;
        }

        // Per-pixel camera velocity
        float2 GetCameraVelocity(float4 uv)
        {
            float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_PointClamp, uv.xy);

        #if UNITY_REVERSED_Z
            depth = 1.0 - depth;
        #endif

            depth = 2.0 * depth - 1.0;

            float3 viewPos = ComputeViewSpacePosition(uv.zw, depth, unity_CameraInvProjection);
            float4 worldPos = float4(mul(unity_CameraToWorld, float4(viewPos, 1.0)).xyz, 1.0);
            float4 prevPos = worldPos;

            float4 prevClipPos = mul(_PrevViewProjM, prevPos);
            float4 curClipPos = mul(_ViewProjM, worldPos);

            float2 prevPosCS = prevClipPos.xy / prevClipPos.w;
            float2 curPosCS = curClipPos.xy / curClipPos.w;

            return ClampVelocity(prevPosCS - curPosCS, _Clamp);
        }

        float3 GatherSample(float sampleNumber, float2 velocity, float invSampleCount, float2 centerUV, float randomVal, float velocitySign)
        {
            float  offsetLength = (sampleNumber + 0.5) + (velocitySign * (randomVal - 0.5));
            float2 sampleUV = centerUV + (offsetLength * invSampleCount) * velocity * velocitySign;
            return SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, sampleUV).xyz;
        }

        half4 DoMotionBlur(VaryingsCMB input, int iterations)
        {
            float2 velocity = GetCameraVelocity(input.uv) * _Intensity;
            float randomVal = InterleavedGradientNoise(input.uv.xy * _MainTex_TexelSize.zw, 0);
            float invSampleCount = rcp(iterations * 2.0);

            half3 color = 0.0;

            for (int i = 0; i < iterations; i++)
            {
                color += GatherSample(i, velocity, invSampleCount, input.uv.xy, randomVal, -1.0);
                color += GatherSample(i, velocity, invSampleCount, input.uv.xy, randomVal,  1.0);
            }

            return half4(color * invSampleCount, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Camera Motion Blur - Low Quality"

            HLSLPROGRAM

                #pragma vertex VertCMB
                #pragma fragment Frag

                half4 Frag(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 2);
                }

            ENDHLSL
        }

        Pass
        {
            Name "Camera Motion Blur - Medium Quality"

            HLSLPROGRAM

                #pragma vertex VertCMB
                #pragma fragment Frag

                half4 Frag(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 3);
                }

            ENDHLSL
        }

        Pass
        {
            Name "Camera Motion Blur - High Quality"

            HLSLPROGRAM

                #pragma vertex VertCMB
                #pragma fragment Frag

                half4 Frag(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 4);
                }

            ENDHLSL
        }
    }
}
