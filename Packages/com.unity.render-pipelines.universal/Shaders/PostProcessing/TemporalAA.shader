Shader "Hidden/Universal Render Pipeline/TemporalAA"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles

        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl" // Depends on URP/Core.hlsl
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#ifndef TAA_YCOCG
#define TAA_YCOCG 1
#endif

#ifndef TAA_GAMMA_SPACE_POST
    #if UNITY_NO_LINEAR_COLORSPACE
        #define TAA_GAMMA_SPACE_POST 1
    else
        #define TAA_GAMMA_SPACE_POST 0
    #endif
#endif

#ifndef TAA_PERCEPTUAL_SPACE
#define TAA_PERCEPTUAL_SPACE 1
#endif

        TEXTURE2D_X(_TaaMotionVectorTex);
        TEXTURE2D_X(_TaaAccumulationTex);

        CBUFFER_START(TemporalAAData)
            float4 _BlitTexture_TexelSize;          // (1/w, 1/h, w, h) "SourceSize"
            float4 _TaaMotionVectorTex_TexelSize;   // (1/w, 1/h, w, h)
            float4 _TaaAccumulationTex_TexelSize;   // (1/w, 1/h, w, h)

            half _TaaFrameInfluence;
        CBUFFER_END

        // Per-pixel camera backwards velocity
        half2 GetCameraVelocityWithOffset(float2 uv, half2 depthOffsetUv)
        {
            // Unity motion vectors are forward motion vectors in screen UV space
            half2 offsetUv = SAMPLE_TEXTURE2D_X(_TaaMotionVectorTex, sampler_LinearClamp, uv + _TaaMotionVectorTex_TexelSize.xy * depthOffsetUv).xy;
            return -offsetUv;
        }

        void AdjustBestDepthOffset(inout half bestDepth, inout half bestX, inout half bestY, float2 uv, half currX, half currY)
        {
            // half precision should be fine, as we are only concerned about choosing the better value along sharp edges, so it's
            // acceptable to have banding on continuous surfaces
            half depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, uv.xy + _BlitTexture_TexelSize.xy * half2(currX, currY)).r;

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
#if TAA_YCOCG
            // We work in YCoCg hence the luminance is in the first channel.
            return color.x;
#else
            return Luminance(color.xyz);
#endif
        }

        float PerceptualWeight(float3 c)
        {
#if TAA_PERCEPTUAL_SPACE
            return rcp(GetLuma(c) + 1.0);
#else
            return 1;
#endif
        }

        float PerceptualInvWeight(float3 c)
        {
#if TAA_PERCEPTUAL_SPACE
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
#if TAA_GAMMA_SPACE_POST
            return src*src;
#else
            return src;
#endif
        }

        half3 LinearToPostFxSpace(float3 src)
        {
#if TAA_GAMMA_SPACE_POST
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
#if TAA_YCOCG
            half3 dst = RGBToYCoCg(linColor);
#else
            half3 dst = src;
#endif
            return dst;
        }

        half3 WorkingSpaceToScene(half3 src)
        {
#if TAA_YCOCG
            half3 linColor = YCoCgToRGB(src);
#else
            half3 linColor = src;
#endif

            half3 dst = LinearToPostFxSpace(linColor);
            return dst;
        }

        void AdjustColorBox(inout half3 boxMin, inout half3 boxMax, inout half3 moment1, inout half3 moment2, float2 uv, half currX, half currY)
        {
            half3 color = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + _BlitTexture_TexelSize.xy * float2(currX, currY)).xyz);
            boxMin = min(color, boxMin);
            boxMax = max(color, boxMax);
            moment1 += color;
            moment2 += color * color;
        }

        half3 ApplyHistoryColorLerp(half3 workingAccumColor, half3 workingCenterColor, float t)
        {
            half3 perceptualAccumColor = WorkingToPerceptual(workingAccumColor);
            half3 perceptualCenterColor = WorkingToPerceptual(workingCenterColor);

            half3 perceptualDstColor = lerp(perceptualAccumColor, perceptualCenterColor, t);
            half3 workingDstColor = PerceptualToWorking(perceptualDstColor);

            return workingDstColor;
        }

        // From Filmic SMAA presentation[Jimenez 2016]
        // A bit more verbose that it needs to be, but makes it a bit better at latency hiding
        // (half version of HDRP impl)
        half3 HistoryBicubic5TapHalf(TEXTURE2D_X(HistoryTexture), float2 UV, float4 historyBufferInfo)
        {
            float2 samplePos = UV * historyBufferInfo.xy;
            float2 tc1 = floor(samplePos - 0.5) + 0.5;
            half2 f = samplePos - tc1;
            half2 f2 = f * f;
            half2 f3 = f * f2;

            half c = 0.5;

            half2 w0 = -c         * f3 +  2.0 * c         * f2 - c * f;
            half2 w1 =  (2.0 - c) * f3 - (3.0 - c)        * f2          + 1.0;
            half2 w2 = -(2.0 - c) * f3 + (3.0 - 2.0 * c)  * f2 + c * f;
            half2 w3 = c          * f3 - c                * f2;

            half2 w12 = w1 + w2;
            float2 tc0 = historyBufferInfo.zw   * (tc1 - 1.0);
            float2 tc3 = historyBufferInfo.zw   * (tc1 + 2.0);
            float2 tc12 = historyBufferInfo.zw  * (tc1 + w2 / w12);

            half3 s0 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(HistoryTexture, sampler_LinearClamp, float2(tc12.x, tc0.y)).xyz);
            half3 s1 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(HistoryTexture, sampler_LinearClamp, float2(tc0.x, tc12.y)).xyz);
            half3 s2 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(HistoryTexture, sampler_LinearClamp, float2(tc12.x, tc12.y)).xyz);
            half3 s3 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(HistoryTexture, sampler_LinearClamp, float2(tc3.x, tc12.y)).xyz);
            half3 s4 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(HistoryTexture, sampler_LinearClamp, float2(tc12.x, tc3.y)).xyz);

            half cw0 = (w12.x * w0.y);
            half cw1 = (w0.x * w12.y);
            half cw2 = (w12.x * w12.y);
            half cw3 = (w3.x * w12.y);
            half cw4 = (w12.x *  w3.y);

            s0 *= cw0;
            s1 *= cw1;
            s2 *= cw2;
            s3 *= cw3;
            s4 *= cw4;

            half3 historyFiltered = s0 + s1 + s2 + s3 + s4;
            half weightSum = cw0 + cw1 + cw2 + cw3 + cw4;

            half3 filteredVal = historyFiltered * rcp(weightSum);

            return filteredVal;
        }

        // From Playdead's TAA
        // (half version of HDRP impl)
        half3 ClipToAABBCenter(half3 history, half3 minimum, half3 maximum)
        {
            // note: only clips towards aabb center (but fast!)
            half3 center  = 0.5 * (maximum + minimum);
            half3 extents = 0.5 * (maximum - minimum);

            // This is actually `distance`, however the keyword is reserved
            half3 offset = history - center;
            half3 v_unit = offset.xyz / max(extents.xyz, HALF_MIN);
            half3 absUnit = abs(v_unit);
            half maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);
            if (maxUnit > 1.0)
                return center + (offset / maxUnit);
            else
                return history;
        }

        // clampQuality:
        //     0: Cross (5 taps)
        //     1: 3x3 (9 taps)
        //     2: Variance + MinMax 3x3 (9 taps)
        //
        // motionQuality:
        //     0: None
        //     1: 5 taps
        //     2: 9 taps
        // historyQuality:
        //     0: Bilinear
        //     1: Bilinear + discard history for UVs out of buffer
        //     2: Bicubic (5 taps)
        half4 DoTemporalAA(Varyings input, int clampQuality, int motionQuality, int historyQuality)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            half3 colorCenter = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + _BlitTexture_TexelSize.xy * float2(0.0, 0.0)).xyz);

            half3 boxMax = colorCenter;
            half3 boxMin = colorCenter;
            half3 moment1 = colorCenter;
            half3 moment2 = colorCenter * colorCenter;

            AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, 0.0f, -1.0f);
            AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, -1.0f, 0.0f);
            AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, 1.0f, 0.0f);
            AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, 0.0f, 1.0f);

            if (clampQuality >= 1)
            {
                AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, -1.0f, -1.0f);
                AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, 1.0f, -1.0f);
                AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, -1.0f, 1.0f);
                AdjustColorBox(boxMin, boxMax, moment1, moment2, uv, 1.0f, 1.0f);
            }

            if(clampQuality >= 2)
            {
                half perSample = 1 / half(9);
                half3 mean = moment1 * perSample;
                half3 stdDev = sqrt(abs(moment2 * perSample - mean * mean));

                half devScale = half(0.9);
                half3 devMin = mean - devScale * stdDev;
                half3 devMax = mean + devScale * stdDev;

                boxMin = max(boxMin, devMin);
                boxMax = min(boxMax, devMax);
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

            half2 depthOffsetUv = half2(bestOffsetX, bestOffsetY);
            half2 velocity = GetCameraVelocityWithOffset(uv, depthOffsetUv);

            half3 accum = (historyQuality >= 2) ?
                HistoryBicubic5TapHalf(_TaaAccumulationTex, uv + velocity * float2(1, 1), _TaaAccumulationTex_TexelSize.zwxy) :
                SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(_TaaAccumulationTex, sampler_LinearClamp, uv + velocity * float2(1, 1)).xyz);

            half3 clampAccum = clamp(accum, boxMin, boxMax);

            // Discard (some) history when outside of history buffer (e.g. camera jump)
            half frameInfluence = ((historyQuality >= 1) && any(abs(uv - 0.5 + velocity) > 0.5)) ? 1 : _TaaFrameInfluence;

            half3 workingColor = ApplyHistoryColorLerp(clampAccum, colorCenter, frameInfluence);

            half3 dstSceneColor = WorkingSpaceToScene(workingColor);
            return half4(dstSceneColor, 1.0);
        }


        half4 DoCopy(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord.xy);
            half3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).xyz;

            return half4(color, 1.0f);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Blend Off Cull Off

        Pass
        {
            Name "TemporalAA - Accumulate - Quality Very Low"

            HLSLPROGRAM

                // TODO: could be cheaper, use RGB instead of YCoCg

                #pragma vertex Vert
                #pragma fragment TaaFrag

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 0, 0, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Accumulate - Quality Low"

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment TaaFrag

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 0, 1, 1);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Accumulate - Quality Medium"

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment TaaFrag

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 2, 2, 1);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Accumulate - Quality High"

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment TaaFrag

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 2, 2, 2);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Copy History"

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment TaaFrag

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoCopy(input);
                }

            ENDHLSL
        }
    }
}
