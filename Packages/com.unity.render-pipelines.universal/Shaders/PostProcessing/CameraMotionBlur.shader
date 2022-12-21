Shader "Hidden/Universal Render Pipeline/CameraMotionBlur"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#if defined(USING_STEREO_MATRICES)
            float4x4 _ViewProjMStereo[2];
            float4x4 _PrevViewProjMStereo[2];
#define _ViewProjM _ViewProjMStereo[unity_StereoEyeIndex]
#define _PrevViewProjM  _PrevViewProjMStereo[unity_StereoEyeIndex]
#else
        float4x4 _ViewProjM;
        float4x4 _PrevViewProjM;
#endif
        half _Intensity;
        half _Clamp;
        half4 _SourceSize;

        struct VaryingsCMB
        {
            float4 positionCS    : SV_POSITION;
            float4 texcoord      : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        VaryingsCMB VertCMB(Attributes input)
        {
            VaryingsCMB output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
            float4 pos = input.positionOS;
            float2 uv  = input.uv;
#else
            float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
            float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
#endif

            output.positionCS  = pos;
            output.texcoord.xy = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;

            float4 projPos = output.positionCS * 0.5;
            projPos.xy = projPos.xy + projPos.w;
            output.texcoord.zw = projPos.xy;

            return output;
        }

        half2 ClampVelocity(half2 velocity, half maxVelocity)
        {
            half len = length(velocity);
            return (len > 0.0) ? min(len, maxVelocity) * (velocity * rcp(len)) : 0.0;
        }

        // Per-pixel camera velocity
        half2 GetCameraVelocity(float4 uv)
        {
            #if UNITY_REVERSED_Z
                half depth = SampleSceneDepth(uv.xy).x;
            #else
                half depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv.xy).x);
            #endif

            float4 worldPos = float4(ComputeWorldSpacePosition(uv.xy, depth, UNITY_MATRIX_I_VP), 1.0);

            float4 prevClipPos = mul(_PrevViewProjM, worldPos);
            float4 curClipPos = mul(_ViewProjM, worldPos);

            half2 prevPosCS = prevClipPos.xy / prevClipPos.w;
            half2 curPosCS = curClipPos.xy / curClipPos.w;

            // Backwards motion vectors
            half2 velocity = (prevPosCS - curPosCS);
            #if UNITY_UV_STARTS_AT_TOP
                velocity.y = -velocity.y;
            #endif
            return ClampVelocity(velocity, _Clamp);
        }

        half3 GatherSample(half sampleNumber, half2 velocity, half invSampleCount, float2 centerUV, half randomVal, half velocitySign)
        {
            half  offsetLength = (sampleNumber + 0.5h) + (velocitySign * (randomVal - 0.5h));
            float2 sampleUV = centerUV + (offsetLength * invSampleCount) * velocity * velocitySign;
            return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUV).xyz;
        }

        half4 DoMotionBlur(VaryingsCMB input, int iterations)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord.xy);
            half2 velocity = GetCameraVelocity(float4(uv, input.texcoord.zw)) * _Intensity;
            half randomVal = InterleavedGradientNoise(uv * _SourceSize.xy, 0);
            half invSampleCount = rcp(iterations * 2.0);

            half3 color = 0.0;

            UNITY_UNROLL
            for (int i = 0; i < iterations; i++)
            {
                color += GatherSample(i, velocity, invSampleCount, uv, randomVal, -1.0);
                color += GatherSample(i, velocity, invSampleCount, uv, randomVal,  1.0);
            }

            return half4(color * invSampleCount, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Camera Motion Blur - Low Quality"

            HLSLPROGRAM

                #pragma vertex VertCMB
                #pragma fragment FragCMB

                half4 FragCMB(VaryingsCMB input) : SV_Target
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
                #pragma fragment FragCMB

                half4 FragCMB(VaryingsCMB input) : SV_Target
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
                #pragma fragment FragCMB

                half4 FragCMB(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 4);
                }

            ENDHLSL
        }
    }
}
