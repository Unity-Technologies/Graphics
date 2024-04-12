#ifndef UNIVERSAL_TEMPORAL_AA
#define UNIVERSAL_TEMPORAL_AA

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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
        float4 _TaaMotionVectorTex_TexelSize;   // (1/w, 1/h, w, h)
        float4 _TaaAccumulationTex_TexelSize;   // (1/w, 1/h, w, h)

        float _TaaFilterWeights[9];

        half _TaaFrameInfluence;
        half _TaaVarianceClampScale;
    CBUFFER_END

    // Per-pixel camera backwards velocity
    half2 GetVelocityWithOffset(float2 uv, half2 depthOffsetUv)
    {
        // Unity motion vectors are forward motion vectors in screen UV space
        half2 offsetUv = SAMPLE_TEXTURE2D_X(_TaaMotionVectorTex, sampler_LinearClamp, uv + _TaaMotionVectorTex_TexelSize.xy * depthOffsetUv).xy;
        return -offsetUv;
    }

    void AdjustBestDepthOffset(inout half bestDepth, inout half bestX, inout half bestY, float2 uv, half currX, half currY)
    {
        // Half precision should be fine, as we are only concerned about choosing the better value along sharp edges, so it's
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

    float4 WorkingToPerceptual(float4 c)
    {
        float scale = PerceptualWeight(c.xyz);
        return c * scale;
    }

    float4 PerceptualToWorking(float4 c)
    {
        float scale = PerceptualInvWeight(c.xyz);
        return c * scale;
    }

    half4 PostFxSpaceToLinear(float4 src)
    {
// gamma 2.0 is a good enough approximation
#if TAA_GAMMA_SPACE_POST
        return half4(src.xyz * src.xyz, src.w);
#else
        return src;
#endif
    }

    half4 LinearToPostFxSpace(float4 src)
    {
#if TAA_GAMMA_SPACE_POST
        return half4(sqrt(src.xyz), src.w);
#else
        return src;
#endif
    }

    // Working Space: The color space that we will do the calculation in.
    // Scene: The incoming/outgoing scene color. Either linear or gamma space
    half4 SceneToWorkingSpace(half4 src)
    {
        half4 linColor = PostFxSpaceToLinear(src);
#if TAA_YCOCG
        half4 dst = half4(RGBToYCoCg(linColor.xyz), linColor.w);
#else
        half4 dst = src;
#endif
        return dst;
    }

    half4 WorkingSpaceToScene(half4 src)
    {
#if TAA_YCOCG
        half4 linColor = half4(YCoCgToRGB(src.xyz), src.w);
#else
        half4 linColor = src;
#endif

        half4 dst = LinearToPostFxSpace(linColor);
        return dst;
    }

    half4 SampleColorPoint(float2 uv, float2 texelOffset)
    {
        return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + _BlitTexture_TexelSize.xy * texelOffset);
    }

    half4 SampleColorLinear(float2 uv, float2 texelOffset)
    {
        return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + _BlitTexture_TexelSize.xy * texelOffset);
    }

    void AdjustColorBox(inout half4 boxMin, inout half4 boxMax, inout half4 moment1, inout half4 moment2, float2 uv, half currX, half currY)
    {
        half4 color = SceneToWorkingSpace(SampleColorPoint(uv, float2(currX, currY)));
        boxMin = min(color, boxMin);
        boxMax = max(color, boxMax);
        moment1 += color;
        moment2 += color * color;
    }

    half4 ApplyHistoryColorLerp(half4 workingAccumColor, half4 workingCenterColor, float t)
    {
        half4 perceptualAccumColor = WorkingToPerceptual(workingAccumColor);
        half4 perceptualCenterColor = WorkingToPerceptual(workingCenterColor);

        half4 perceptualDstColor = lerp(perceptualAccumColor, perceptualCenterColor, t);
        half4 workingDstColor = PerceptualToWorking(perceptualDstColor);

        return workingDstColor;
    }

    // From Filmic SMAA presentation[Jimenez 2016]
    // A bit more verbose that it needs to be, but makes it a bit better at latency hiding
    // (half version based on HDRP impl)
    half4 SampleBicubic5TapHalf(TEXTURE2D_X(sourceTexture), float2 UV, float4 sourceTexture_TexelSize)
    {
        const float2 sourceTextureSize = sourceTexture_TexelSize.zw;
        const float2 sourceTexelSize = sourceTexture_TexelSize.xy;

        float2 samplePos = UV * sourceTextureSize;
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
        float2 tc0 = sourceTexelSize  * (tc1 - 1.0);
        float2 tc3 = sourceTexelSize  * (tc1 + 2.0);
        float2 tc12 = sourceTexelSize * (tc1 + w2 / w12);

        half4 s0 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(sourceTexture, sampler_LinearClamp, float2(tc12.x, tc0.y)));
        half4 s1 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(sourceTexture, sampler_LinearClamp, float2(tc0.x, tc12.y)));
        half4 s2 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(sourceTexture, sampler_LinearClamp, float2(tc12.x, tc12.y)));
        half4 s3 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(sourceTexture, sampler_LinearClamp, float2(tc3.x, tc12.y)));
        half4 s4 = SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(sourceTexture, sampler_LinearClamp, float2(tc12.x, tc3.y)));

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

        half4 historyFiltered = s0 + s1 + s2 + s3 + s4;
        half weightSum = cw0 + cw1 + cw2 + cw3 + cw4;

        half4 filteredVal = historyFiltered * rcp(weightSum);

        return filteredVal;
    }

    // From Playdead's TAA
    // (half version of HDRP impl)
    //
    // Small color-volume min size seems to produce flicker/noise in YCoCg space, that can't be seen in RGB,
    // when using low precision (RGB111110f) color textures.
    half4 ClipToAABBCenter(half4 history, half4 minimum, half4 maximum)
    {
        // note: only clips towards aabb center (but fast!)
        half4 center  = 0.5 * (maximum + minimum);
        half4 extents = max(0.5 * (maximum - minimum), HALF_MIN);   // Epsilon to avoid precision issues with empty volume.

        // This is actually `distance`, however the keyword is reserved
        half4 offset = history - center;
        half3 v_unit = offset.xyz / extents.xyz;
        half3 absUnit = abs(v_unit);
        half maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);
        if (maxUnit > 1.0)
            return center + (offset / maxUnit);
        else
            return history;
    }

    // Based on HDRP
    half4 FilterColor(float2 uv, float weights[9])
    {
        half4 filtered = weights[0] * PostFxSpaceToLinear(SampleColorPoint(uv, float2(0.0, 0.0f)));

        filtered += weights[1] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(0.0f, 1.0)));
        filtered += weights[2] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(1.0f, 0.0f)));
        filtered += weights[3] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(-1.0f, 0.0f)));
        filtered += weights[4] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(0.0f, -1.0f)));

        filtered += weights[5] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(-1.0f, 1.0f)));
        filtered += weights[6] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(1.0f, -1.0f)));
        filtered += weights[7] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(1.0f, 1.0f)));
        filtered += weights[8] * PostFxSpaceToLinear(SampleColorPoint(uv,float2(-1.0f, -1.0f)));
        #if TAA_YCOCG
            return half4(RGBToYCoCg(filtered.xyz), filtered.w);
        #else
            return filtered;
        #endif
    }

    // clampQuality:
    //     0: Cross (5 taps)
    //     1: 3x3 (9 taps)
    //     2: Variance + MinMax 3x3 (9 taps)
    //     3: Variance Clipping
    //
    // motionQuality:
    //     0: None
    //     1: 5 taps
    //     2: 9 taps
    // historyQuality:
    //     0: Bilinear
    //     1: Bilinear + discard history for UVs out of buffer
    //     2: Bicubic (5 taps)
    half4 DoTemporalAA(Varyings input, int clampQuality, int motionQuality, int historyQuality, int centralFiltering)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // uv is exactly on input pixel center (x + 0.5, y + 0.5)
        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        half4 colorCenter;
        if(centralFiltering >= 1)
            colorCenter = FilterColor(uv, _TaaFilterWeights);
        else
            colorCenter = SceneToWorkingSpace(SampleColorPoint( uv, float2(0,0)));  // Point == Linear as uv == input pixel center.

        half4 boxMax = colorCenter;
        half4 boxMin = colorCenter;
        half4 moment1 = colorCenter;
        half4 moment2 = colorCenter * colorCenter;

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
            half4 mean = moment1 * perSample;
            half4 stdDev = sqrt(abs(moment2 * perSample - mean * mean));

            half devScale = _TaaVarianceClampScale;
            half4 devMin = mean - devScale * stdDev;
            half4 devMax = mean + devScale * stdDev;

            // Ensure that the variance color box is not worse than simple neighborhood color box.
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
        half2 velocity = GetVelocityWithOffset(uv, depthOffsetUv);

        float2 historyUv = uv + velocity * float2(1, 1);
        half4 accumulation = (historyQuality >= 2) ?
            SampleBicubic5TapHalf(_TaaAccumulationTex, historyUv, _TaaAccumulationTex_TexelSize.xyzw) :
            SceneToWorkingSpace(SAMPLE_TEXTURE2D_X(_TaaAccumulationTex, sampler_LinearClamp, historyUv));

        half4 clampedAccumulation = (clampQuality >= 3) ? ClipToAABBCenter(accumulation, boxMin, boxMax) : clamp(accumulation, boxMin, boxMax);

        // Discard (some) history when outside of history buffer (e.g. camera jump)
        half frameInfluence = ((historyQuality >= 1) && any(abs(uv - 0.5 + velocity) > 0.5)) ? 1 : _TaaFrameInfluence;

        half4 workingColor = ApplyHistoryColorLerp(clampedAccumulation, colorCenter, frameInfluence);

        half4 dstSceneColor = WorkingSpaceToScene(workingColor);

        #if _ENABLE_ALPHA_OUTPUT
            return max(dstSceneColor, 0.0);
        #else
            // NOTE: The compiler should eliminate .w computation since it doesn't affect the output.
            return half4(max(dstSceneColor.xyz, 0.0), 1.0);
        #endif
    }


    half4 DoCopy(Varyings input)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord.xy);
        half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

        return color;
    }
#endif
