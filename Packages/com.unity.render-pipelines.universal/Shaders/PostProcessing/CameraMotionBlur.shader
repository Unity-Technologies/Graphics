Shader "Hidden/Universal Render Pipeline/CameraMotionBlur"
{
    HLSLINCLUDE
        #pragma vertex VertCMB
        #pragma fragment FragCMB
        #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT

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

        TEXTURE2D_X(_MotionVectorTexture);

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

            float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
            float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

            output.positionCS  = pos;
            output.texcoord.xy = DYNAMIC_SCALING_APPLY_SCALEBIAS(uv);

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

        half2 GetVelocity(float2 uv)
        {
            // Unity motion vectors are forward motion vectors in screen UV space
            half2 offsetUv = SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_LinearClamp, uv).xy;
            return -offsetUv;
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

        half4 GatherSample(half sampleNumber, half2 velocity, half invSampleCount, float2 centerUV, half randomVal, half velocitySign)
        {
            half  offsetLength = (sampleNumber + 0.5h) + (velocitySign * (randomVal - 0.5h));
            float2 sampleUV = centerUV + (offsetLength * invSampleCount) * velocity * velocitySign;
            return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUV);
        }

        half4 DoMotionBlur(VaryingsCMB input, int iterations, int useMotionVectors)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord.xy);

            half2 velocity;
            if(useMotionVectors == 1)
            {
                velocity = GetVelocity(uv) * _Intensity;
                // Scale back to -1, 1 from 0..1 to match GetCameraVelocity. A workaround to keep existing visual look.
                // TODO: There's bug in GetCameraVelocity, which is using NDC and not UV
                velocity *= 2;
            }
            else
                velocity = GetCameraVelocity(float4(uv, input.texcoord.zw)) * _Intensity;

            half randomVal = InterleavedGradientNoise(uv * _SourceSize.xy, 0);
            half invSampleCount = rcp(iterations * 2.0);

            half4 color = 0.0;

            UNITY_UNROLL
            for (int i = 0; i < iterations; i++)
            {
                color += GatherSample(i, velocity, invSampleCount, uv, randomVal, -1.0);
                color += GatherSample(i, velocity, invSampleCount, uv, randomVal,  1.0);
            }

            #if _ENABLE_ALPHA_OUTPUT
                return color * invSampleCount;
            #else
                  // NOTE: Rely on the compiler to eliminate .w computation above
                return half4(color.xyz * invSampleCount, 1.0);
            #endif
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

                half4 FragCMB(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 2, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "Camera Motion Blur - Medium Quality"

            HLSLPROGRAM

                half4 FragCMB(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 3, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "Camera Motion Blur - High Quality"

            HLSLPROGRAM

                half4 FragCMB(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 4, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "Camera And Object Motion Blur - Low Quality"

            HLSLPROGRAM

                half4 FragCMB(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 2, 1);
                }

            ENDHLSL
        }

        Pass
        {
            Name "Camera And Object Motion Blur - Medium Quality"

            HLSLPROGRAM

                half4 FragCMB(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 3, 1);
                }

            ENDHLSL
        }

        Pass
        {
            Name "Camera And Object Motion Blur - High Quality"

            HLSLPROGRAM

                half4 FragCMB(VaryingsCMB input) : SV_Target
                {
                    return DoMotionBlur(input, 4, 1);
                }

            ENDHLSL
        }
    }
}
