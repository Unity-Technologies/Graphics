Shader "Hidden/Universal Render Pipeline/TemporalAA"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles

        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#define YCOCG 1


#ifndef YCOCG
#define YCOCG 1
#endif

#ifndef URP_GAMMA_SPACE_POST
#define URP_GAMMA_SPACE_POST 0
#endif

#ifndef PERCEPTUAL_SPACE
#define PERCEPTUAL_SPACE 1
#endif

        TEXTURE2D_X(_SourceTex);
        float4 _SourceTex_TexelSize;

        TEXTURE2D_X(_AccumulationTex);
#if defined(USING_STEREO_MATRICES)
        float4x4 _PrevViewProjMStereo[2];
#define _PrevViewProjM  _PrevViewProjMStereo[unity_StereoEyeIndex]
#define _ViewProjM unity_MatrixVP
#else
        float4x4 _ViewProjM;
        float4x4 _PrevViewProjM;
#endif
        half4 _SourceSize;

        half _TemporalAAFrameInfl;

        struct VaryingsCMB
        {
            float4 positionCS    : SV_POSITION;
            float4 uv            : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        VaryingsCMB VertCMB(Attributes input)
        {
            VaryingsCMB output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if _USE_DRAW_PROCEDURAL
            GetProceduralQuad(input.vertexID, output.positionCS, output.uv.xy);
#else
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv.xy = input.uv;
#endif
            float4 projPos = output.positionCS * 0.5;
            projPos.xy = projPos.xy + projPos.w;
            output.uv.zw = projPos.xy;

            return output;
        }

        // Per-pixel camera velocity
        half2 GetCameraVelocityWithOffset(float4 uv, half2 depthOffsetUv)
        {
            float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv.xy + _SourceTex_TexelSize.xy * depthOffsetUv).r;

        #if UNITY_REVERSED_Z
            depth = 1.0 - depth;
        #endif

            depth = 2.0 * depth - 1.0;

            float3 viewPos = ComputeViewSpacePosition(uv.zw, depth, unity_CameraInvProjection);
            float4 worldPos = float4(mul(unity_CameraToWorld, float4(viewPos, 1.0)).xyz, 1.0);
            float4 prevPos = worldPos;

            float4 prevClipPos = mul(_PrevViewProjM, prevPos);
            float4 curClipPos = mul(_ViewProjM, worldPos);

            half2 prevPosCS = prevClipPos.xy / prevClipPos.w;
            half2 curPosCS = curClipPos.xy / curClipPos.w;

            return prevPosCS - curPosCS;
        }

        half3 GatherSample(half sampleNumber, half2 velocity, half invSampleCount, float2 centerUV, half randomVal, half velocitySign)
        {
            half  offsetLength = (sampleNumber + 0.5h) + (velocitySign * (randomVal - 0.5h));
            float2 sampleUV = centerUV + (offsetLength * invSampleCount) * velocity * velocitySign;
            return SAMPLE_TEXTURE2D_X(_SourceTex, sampler_PointClamp, sampleUV).xyz;
        }

        void AdjustBestDepthOffset(inout half bestDepth, inout half bestX, inout half bestY, float2 uv, half currX, half currY)
        {
            // half precision should be fine, as we are only concerned about choosing the better value along sharp edges, so it's
            // acceptable to have banding on continuous surfaces
            half depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv.xy + _SourceTex_TexelSize.xy * half2(currX, currY)).r;

#if UNITY_REVERSED_Z
            depth = 1.0 - depth;
#endif

            bool isBest = depth < bestDepth;
            bestDepth = isBest ? depth : bestDepth;
            bestX = isBest ? currX : bestX;
            bestY = isBest ? currY : bestY;
        }

        float GetLuma(float3 color)
        {
#if YCOCG
            // We work in YCoCg hence the luminance is in the first channel.
            return color.x;
#else
            return Luminance(color.xyz);
#endif
        }

        float PerceptualWeight(float3 c)
        {
#if PERCEPTUAL_SPACE
            return rcp(GetLuma(c) + 1.0);
#else
            return 1;
#endif
        }

        float PerceptualInvWeight(float3 c)
        {
#if PERCEPTUAL_SPACE
            return rcp(1.0 - GetLuma(c));
#else
            return 1;
#endif
        }

        float3 WorkingToPerceptual(float3 c)
        {
            float scale = PerceptualWeight(c);
            return c * scale;
        }

        float3 PerceptualToWorking(float3 c)
        {
            float scale = PerceptualInvWeight(c);
            return c * scale;
        }

        half3 PostFxSpaceToLinear(float3 src)
        {
// gamma 2.0 is a good enough approximation
#if URP_GAMMA_SPACE_POST
            return src*src;
#else
            return src;
#endif
        }

        half3 LinearToPostFxSpace(float3 src)
        {
#if URP_GAMMA_SPACE_POST
            return sqrt(src);
#else
            return src;
#endif
        }

        // Working Space: The color space that we will do the calculation in.
        // Scene: The incoming/outgoing scene color. Either linear or gamma space
        half3 SceneToWorkingSpace(half3 src)
        {
            half3 linColor = PostFxSpaceToLinear(src);
#if YCOCG
            half3 dst = RGBToYCoCg(linColor);
#else
            half3 dst = src;
#endif
            return dst;
        }

        half3 WorkingSpaceToScene(half3 src)
        {
#if YCOCG
            half3 linColor = YCoCgToRGB(src);
#else
            half3 linColor = src;
#endif

            half3 dst = LinearToPostFxSpace(linColor);
            return dst;
        }


        void AdjustColorBox(inout half3 boxMin, inout half3 boxMax, float2 uv, half currX, half currY)
        {
            half3 color = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy * float2(currX, currY)));
            boxMin = min(color, boxMin);
            boxMax = max(color, boxMax);
        }

        half3 ApplyHistoryColorLerp(half3 workingAccumColor, half3 workingCenterColor, float t)
        {
            half3 perceptualAccumColor = WorkingToPerceptual(workingAccumColor);
            half3 perceptualCenterColor = WorkingToPerceptual(workingCenterColor);

            half3 perceptualDstColor = lerp(perceptualAccumColor, perceptualCenterColor, t);
            half3 workingDstColor = PerceptualToWorking(perceptualDstColor);

            //half3 dstColor = WorkingSpaceToScene(workingDstColor);
            return workingDstColor;
        }

        // clampQuality:
        //     0: Cross (5 taps)
        //     1: 3x3 (9 taps)
        // motionQuality:
        //     0: None
        //     1: 5 taps
        //     2: 9 taps
        half4 DoTemporalAA(VaryingsCMB input, int clampQuality, int motionQuality)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv.xy);
            half2 depthOffsetUv = 0.0f;

            half3 colorCenter = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy * float2(0.0, 0.0)));

            half3 boxMax = colorCenter;
            half3 boxMin = colorCenter;

            AdjustColorBox(boxMin, boxMax, uv, 0.0f, -1.0f);
            AdjustColorBox(boxMin, boxMax, uv, -1.0f, 0.0f);
            AdjustColorBox(boxMin, boxMax, uv, 1.0f, 0.0f);
            AdjustColorBox(boxMin, boxMax, uv, 0.0f, 1.0f);

            if (clampQuality >= 1)
            {
                AdjustColorBox(boxMin, boxMax, uv, -1.0f, -1.0f);
                AdjustColorBox(boxMin, boxMax, uv, 1.0f, -1.0f);
                AdjustColorBox(boxMin, boxMax, uv, -1.0f, 1.0f);
                AdjustColorBox(boxMin, boxMax, uv, 1.0f, 1.0f);
            }

            half bestOffsetX = 0.0f;
            half bestOffsetY = 0.0f;
            half bestDepth = 1.0f;
            if (motionQuality >= 1)
            {
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, 0.0f, 0.0f);
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, 1.0f, 0.0f);
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, 0.0f, -1.0f);
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, -1.0f, 0.0f);
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, 0.0f, 1.0f);
            }

            if (motionQuality >= 2)
            {
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, -1.0f, -1.0f);
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, 1.0f, -1.0f);
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, -1.0f, 1.0f);
                AdjustBestDepthOffset(bestDepth, bestOffsetX, bestOffsetY, uv, 1.0f, 1.0f);
            }


            depthOffsetUv = half2(bestOffsetX, bestOffsetY);

            half2 velocity = GetCameraVelocityWithOffset(float4(uv, input.uv.zw), depthOffsetUv);
            half randomVal = InterleavedGradientNoise(uv * _SourceSize.xy, 0);

            half3 accum = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(_AccumulationTex, sampler_LinearClamp, uv + 0.5 * velocity * float2(1, 1)).xyz);
            half3 clampAccum = clamp(accum, boxMin, boxMax);

            half3 workingColor = ApplyHistoryColorLerp(clampAccum, colorCenter, _TemporalAAFrameInfl);

#if 0
            float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv.xy).r;

#if UNITY_REVERSED_Z
            depth = 1.0 - depth;
#endif

            depth = 2.0 * depth - 1.0;
#endif

            //color = depthOffsetUv.x*.5+.5;
            //color = depth;
            //color = half3(saturate(velocity * 10.0f + 0.5f), 0.0f);
            half3 dstSceneColor = WorkingSpaceToScene(workingColor);

            return half4(dstSceneColor, 1.0);
        }


        half4 DoCopy(VaryingsCMB input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv.xy);

            // seems to require an extra flip
            uv.y = 1.0f - uv.y;

            half3 color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_PointClamp, uv).xyz;

            return half4(color, 1.0f);
        }


    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "TemporalAA - Accumulate"

            HLSLPROGRAM

                #pragma vertex VertCMB
                #pragma fragment Frag

                half4 Frag(VaryingsCMB input) : SV_Target
                {
                    return DoTemporalAA(input, 0, 2);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Copy"

            HLSLPROGRAM

                #pragma vertex VertCMB
                #pragma fragment Frag

                half4 Frag(VaryingsCMB input) : SV_Target
                {
                    return DoCopy(input);
                }

            ENDHLSL
        }

    }
}
