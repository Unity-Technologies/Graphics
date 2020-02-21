// ---------------- TODO LIST
// - Instead of exclude from TAA use reduced contribution of history?
// - IMPORTANT: TODO_FCC: Read quad across to have less samples. (reduce 2 sample in each neighbourhood that includes across X, across Y
//                        and 1 extra reduction if read in diagonal.


Shader "Hidden/HDRP/TAA2"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2
        [HideInInspector] _StencilMask("_StencilMask", Int) = 2
    }

    HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile_local _ ORTHOGRAPHIC
        #pragma multi_compile_local _ REDUCED_HISTORY_CONTRIB
        #pragma multi_compile_local _ ENABLE_ALPHA
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/TemporalAntialiasing.hlsl"


#undef CTYPE
#define CTYPE float3

        TEXTURE2D_X(_InputTexture);
        TEXTURE2D_X(_InputHistoryTexture);
        TEXTURE2D_X(_DepthTexture);
        RW_TEXTURE2D_X(CTYPE, _OutputHistoryTexture);

        struct Attributes
        {
            uint vertexID : SV_VertexID;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        // ---------------------------------------------------
        // Utilities functions, need to move later on.
        // ---------------------------------------------------

        float3 QuadReadAcrossX_3(float3 val, int2 positionSS)
        {
            return float3(QuadReadAcrossX(val.x, positionSS), QuadReadAcrossX(val.y, positionSS), QuadReadAcrossX(val.z, positionSS));
        }

        float3 QuadReadAcrossY_3(float3 val, int2 positionSS)
        {
            return float3(QuadReadAcrossY(val.x, positionSS), QuadReadAcrossY(val.y, positionSS), QuadReadAcrossY(val.z, positionSS));
        }


        float2 GetQuadOffset_other(int2 screenPos)
        {
            return float2(float(screenPos.x & 1) * 2.0 - 1.0, float(screenPos.y & 1) * 2.0 - 1.0);
        }
        #define DEPTH_NEIGHBOUR_RADIUS 1

        float2 GetClosestFragment2(float2 positionSS)
        {
            // This load in a cross shape. We can save one sample with quad reads
            float center = LoadCameraDepth(positionSS);

            float2 quadOffset = GetQuadOffset_other(positionSS);

            int2 fastOffset = int2(quadOffset.x > 0 ? -1 : 1, quadOffset.y > 0 ? 1 : -1);
            int2 offset1 = (quadOffset.x == quadOffset.y) ? int2(1, 1) : int2(-1, 1);
            int2 offset2 = (quadOffset.x == 0 && quadOffset.y == 1) ? int2(1, 1) : int2(-1, -1);
            int2 offset3 = (quadOffset.x == 0 && quadOffset.y == 0) ? int2(-1, 1) : int2(1, -1);

            float s0 = QuadReadAcrossDiagonal(center, positionSS);
            float s1 = LOAD_TEXTURE2D_X_LOD(_DepthTexture, positionSS + offset1, 0).r;
            float s2 = LOAD_TEXTURE2D_X_LOD(_DepthTexture, positionSS + offset2, 0).r; 
            float s3 = LOAD_TEXTURE2D_X_LOD(_DepthTexture, positionSS + offset3, 0).r;

            float3 closest = float3(0.0, 0.0, center);
            closest = lerp(closest, float3(fastOffset,  s0), COMPARE_DEPTH(s0, closest.z));
            closest = lerp(closest, float3(offset1,     s1), COMPARE_DEPTH(s1, closest.z));
            closest = lerp(closest, float3(offset2,     s2), COMPARE_DEPTH(s2, closest.z));
            closest = lerp(closest, float3(offset3,     s3), COMPARE_DEPTH(s3, closest.z));

            return positionSS + closest.xy;
        }

        float3 ReadAsYCoCg(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
        {
            float4 rgb = Fetch4(tex, coords, offset, scale);
            return RGBToYCoCg(rgb.xyz);
        }

        float3 MinColor(float3 a, float3 b, float3 c)
        {
            return float3(Min3(a.x, b.x, c.x),
                Min3(a.y, b.y, c.y),
                Min3(a.z, b.z, c.z));
        }

        float3 MaxColor(float3 a, float3 b, float3 c)
        {
            return float3(Max3(a.x, b.x, c.x),
                Max3(a.y, b.y, c.y),
                Max3(a.z, b.z, c.z));
        }

        // ---------------------------------------------------
        // History sampling 
        // ---------------------------------------------------

        // TODO: Check to avoid sampling outside the screen!

        #define BILINEAR 0
        #define OUR_BICUBIC 1
        #define BICUBIC_5TAP 2
        #define HISTORY_SAMPLING_METHOD BICUBIC_5TAP

        float3 HistoryLoad(float2 UV)
        {
            float4 rgb = Fetch4(_InputHistoryTexture, UV, 0.0, _RTHandleScaleHistory.xy);
            return rgb.xyz;
        }

        float3 HistoryBicubic4Tap(float2 UV)
        {
            // TODO Is using _ScreenSize correct here? 
            float2 TexSize = _ScreenSize.xy * rcp(_RTHandleScaleHistory.xy);
            float4 bicubicWnd = float4(TexSize, 1.0 / (TexSize));

            return SampleTexture2DBicubic(
                TEXTURE2D_X_ARGS(_InputHistoryTexture, s_linear_clamp_sampler),
                UV * _RTHandleScaleHistory.xy,
                bicubicWnd,
                (1.0f - 0.5f * _ScreenSize.zw) * _RTHandleScaleHistory.xy,
                unity_StereoEyeIndex).xyz;
        }

        float3 Bicubic(TEXTURE2D_X(tex), float2 UV)
        {
            float4 texSize = _ScreenSize * float4(_RTHandleScaleHistory.xy, rcp(_RTHandleScaleHistory.xy));
            const float Sharpening = 0.15;  // [-0.5, 0.5]

            float2 samplePos = UV * texSize.xy;
            float2 tc1 = floor(samplePos - 0.5) + 0.5;
            float2 f = samplePos - tc1;
            float2 f2 = f * f;
            float2 f3 = f * f2;

            // Catmull-Rom weights // TODO_FCC: REFACTOR, SIMPLIFY.
            const float c = 0.5 + Sharpening;
            float2 w0 = -(c)* f3 + (2.0 * c)        * f2 - (c * f);
            float2 w1 = (2.0 - c) * f3 - (3.0 - c)        * f2 + 1.0;
            float2 w2 = -(2.0 - c) * f3 + (3.0 - 2.0 * c) * f2 + (c * f);
            float2 w3 = (c)* f3 - (c)* f2;

            float2 w12 = w1 + w2;
            float2 tc0 = (tc1 - 1.0)      * texSize.zw;
            float2 tc3 = (tc1 + 2.0)      * texSize.zw;
            float2 tc12 = (tc1 + w2 / w12) * texSize.zw;

            float4 historyFiltered = float4(Fetch(tex, float2(tc12.x, tc0.y), 0.0, _RTHandleScaleHistory.xy), 1.0)  * (w12.x * w0.y) +
                float4(Fetch(tex, float2(tc0.x, tc12.y), 0.0, _RTHandleScaleHistory.xy), 1.0)  * (w0.x * w12.y) +
                float4(Fetch(tex, float2(tc12.x, tc12.y), 0.0, _RTHandleScaleHistory.xy), 1.0) * (w12.x * w12.y) +
                float4(Fetch(tex, float2(tc3.x, tc0.y), 0.0, _RTHandleScaleHistory.xy), 1.0)   * (w3.x * w12.y) +
                float4(Fetch(tex, float2(tc12.x, tc3.y), 0.0, _RTHandleScaleHistory.xy), 1.0)  * (w12.x *  w3.y);

            return historyFiltered.rgb * rcp(historyFiltered.a);
        }

        float3 HistoryBicubic5Tap(float2 UV)
        {
            float4 texSize = _ScreenSize * float4(_RTHandleScaleHistory.xy, rcp(_RTHandleScaleHistory.xy));
            const float Sharpening = 0.0f;  

            float2 samplePos = UV * texSize.xy;
            float2 tc1 = floor(samplePos - 0.5) + 0.5;
            float2 f = samplePos - tc1;
            float2 f2 = f * f;
            float2 f3 = f * f2;

            const float c = 0.5 + Sharpening;
            float2 w0 = -(c)* f3 + (2.0 * c)        * f2 - (c * f);
            float2 w1 = (2.0 - c) * f3 - (3.0 - c)        * f2 + 1.0;
            float2 w2 = -(2.0 - c) * f3 + (3.0 - 2.0 * c) * f2 + (c * f);
            float2 w3 = (c)* f3 - (c)* f2;

            float2 w12 = w1 + w2;
            float2 tc0 = (tc1 - 1.0)      * texSize.zw;
            float2 tc3 = (tc1 + 2.0)      * texSize.zw;
            float2 tc12 = (tc1 + w2 / w12) * texSize.zw;

            float4 historyFiltered = float4(Fetch(_InputHistoryTexture, float2(tc12.x, tc0.y), 0.0, _RTHandleScaleHistory.xy), 1.0)  * (w12.x * w0.y) +
                float4(Fetch(_InputHistoryTexture, float2(tc0.x, tc12.y), 0.0, _RTHandleScaleHistory.xy), 1.0)  * (w0.x * w12.y) +
                float4(Fetch(_InputHistoryTexture, float2(tc12.x, tc12.y), 0.0, _RTHandleScaleHistory.xy), 1.0) * (w12.x * w12.y) +
                float4(Fetch(_InputHistoryTexture, float2(tc3.x, tc0.y), 0.0, _RTHandleScaleHistory.xy), 1.0)   * (w3.x * w12.y) +
                float4(Fetch(_InputHistoryTexture, float2(tc12.x, tc3.y), 0.0, _RTHandleScaleHistory.xy), 1.0)  * (w12.x *  w3.y);

            return historyFiltered.rgb * rcp(historyFiltered.a);
        }

        float3 GetFilteredHistory(float2 UV)
        {
            float3 history = 0;

#if HISTORY_SAMPLING_METHOD == BILINEAR
            history = HistoryLoad(UV);
#elif HISTORY_SAMPLING_METHOD == OUR_BICUBIC
            history = HistoryBicubic4Tap(UV);
#elif HISTORY_SAMPLING_METHOD == BICUBIC_5TAP
            history = HistoryBicubic5Tap(UV);
#endif

            return RGBToYCoCg(history);
        }

    // ---------------------------------------------------
    // Neighbourhood color.
    // ---------------------------------------------------

        // CRUCIAL TO DO THE READ ACROSS!
        // NOTE (TODO_FCC) With the read across, wide neighbourhood would be a      5 samples
        //                                       plus small neighbourhood would  be 2
        //                                       cross small neighbourhood would be 3

        #define PLUS 0    // Faster! Can allow for read across twice (paying cost of 2 samples only)
        #define CROSS 1   // Can only do one fast read diagonal 
        #define SMALL_NEIGHBOURHOOD_SHAPE PLUS

        #define SMALL_NEIGHBOURHOOD_SIZE 4 
        // If 0, the neighbourhood is smaller (4 or 5, depends on shape), if 1 the neighbourhood is 9 samples (full 3x3)
        #define WIDE_NEIGHBOURHOOD 0

        // NOT TO CONFIG MANULLY!
        #define NEIGHBOUR_COUNT ((WIDE_NEIGHBOURHOOD == 0) ? 4 : 8)

        struct NeighbourhoodSamples
        {
            float3 neighbours[8];
            float3 central;
        };


        // TODO_FCC! Verify if actually we need to do the conversion on read or can be done only on the corners? 

        void GatherNeighbourhood(float2 UV, float2 positionSS, float3 centralColor, out NeighbourhoodSamples samples)
        {
            samples.neighbours = (float3[8])0; // TODO_FCC VERIFY

            samples.central = centralColor;

#if WIDE_NEIGHBOURHOOD

            samples.neighbours[0] = ReadAsYCoCg(_InputTexture, UV, float2( 1.0,  0.0), _RTHandleScale.xy);
            samples.neighbours[1] = ReadAsYCoCg(_InputTexture, UV, float2( 0.0,  1.0), _RTHandleScale.xy);
            samples.neighbours[2] = ReadAsYCoCg(_InputTexture, UV, float2(-1.0,  0.0), _RTHandleScale.xy);
            samples.neighbours[3] = ReadAsYCoCg(_InputTexture, UV, float2( 0.0, -1.0), _RTHandleScale.xy);
            samples.neighbours[4] = ReadAsYCoCg(_InputTexture, UV, float2( 1.0,  1.0), _RTHandleScale.xy);
            samples.neighbours[5] = ReadAsYCoCg(_InputTexture, UV, float2( 1.0, -1.0), _RTHandleScale.xy);
            samples.neighbours[6] = ReadAsYCoCg(_InputTexture, UV, float2(-1.0, -1.0), _RTHandleScale.xy);
            samples.neighbours[7] = ReadAsYCoCg(_InputTexture, UV, float2(-1.0,  1.0), _RTHandleScale.xy);

#else // !WIDE_NEIGHBOURHOOD

#if SMALL_NEIGHBOURHOOD_SHAPE == PLUS

            float2 quadOffset = GetQuadOffset_other(positionSS);

            samples.neighbours[0] = ReadAsYCoCg(_InputTexture, UV, float2(0.0f, quadOffset.y), _RTHandleScale.xy);
            samples.neighbours[1] = ReadAsYCoCg(_InputTexture, UV, float2(quadOffset.x, 0.0f), _RTHandleScale.xy);
            samples.neighbours[2] = QuadReadAcrossX_3(centralColor, positionSS);
            samples.neighbours[3] = QuadReadAcrossY_3(centralColor, positionSS);

#else // SMALL_NEIGHBOURHOOD_SHAPE == CROSS

            samples.neighbours[0] = ReadAsYCoCg(_InputTexture, UV, float2( 1.0,  1.0), _RTHandleScale.xy);
            samples.neighbours[1] = ReadAsYCoCg(_InputTexture, UV, float2( 1.0, -1.0), _RTHandleScale.xy);
            samples.neighbours[2] = ReadAsYCoCg(_InputTexture, UV, float2(-1.0, -1.0), _RTHandleScale.xy);
            samples.neighbours[3] = ReadAsYCoCg(_InputTexture, UV, float2(-1.0,  1.0), _RTHandleScale.xy);

#endif // SMALL_NEIGHBOURHOOD_SHAPE == 5

#endif // !WIDE_NEIGHBOURHOOD
        }


        void MinMaxNeighbourhood(NeighbourhoodSamples samples, out float3 minNeighbour, out float3 maxNeighbour)
        {
            // We always have at least the first 4 neighbours.
            minNeighbour = MinColor(samples.neighbours[0], samples.neighbours[1], samples.neighbours[2]);
            minNeighbour = MinColor(minNeighbour, samples.central, samples.neighbours[3]);

            maxNeighbour = MaxColor(samples.neighbours[0], samples.neighbours[1], samples.neighbours[2]);
            maxNeighbour = MaxColor(maxNeighbour, samples.central, samples.neighbours[3]);

#if WIDE_NEIGHBOURHOOD
            minNeighbour = MinColor(minNeighbour, samples.neighbours[4], samples.neighbours[5]);
            minNeighbour = MinColor(minNeighbour, samples.neighbours[6], samples.neighbours[7]);

            maxNeighbour = MaxColor(maxNeighbour, samples.neighbours[4], samples.neighbours[5]);
            maxNeighbour = MaxColor(maxNeighbour, samples.neighbours[6], samples.neighbours[7]);
#endif
        }

        void VarianceNeighbourhood(NeighbourhoodSamples samples, out float3 minNeighbour, out float3 maxNeighbour, out float stdDevOut)
        {
            float3 moment1 = samples.central;
            float3 moment2 = samples.central * samples.central;

            for (int i = 0; i < NEIGHBOUR_COUNT; ++i)
            {
                moment1 += samples.neighbours[i];
                moment2 += samples.neighbours[i] * samples.neighbours[i];
            }

            const int sampleCount = NEIGHBOUR_COUNT + 1;
            moment1 *= rcp(sampleCount);
            moment2 *= rcp(sampleCount);

            float3 stdDev = sqrt(abs(moment2 - moment1 * moment1));

            float stDevMultiplier = lerp(1.15, 2.0, saturate((stdDev - 0.1) / (0.5 - 0.1)));
            stDevMultiplier = 1.8;
            stdDevOut = stdDev;
            minNeighbour = moment1 - stDevMultiplier * stdDev;
            maxNeighbour = moment1 + stDevMultiplier * stdDev;
        }

#define MINMAX 0
#define VARIANCE 1
#define NEIGHBOUROOD_CORNER_METHOD VARIANCE

        void GetNeighbourhoodCorners(NeighbourhoodSamples samples, out float3 minNeighbour, out float3 maxNeighbour, out float stdDevOut)
        {
#if NEIGHBOUROOD_CORNER_METHOD == MINMAX
            MinMaxNeighbourhood(samples, minNeighbour, maxNeighbour);
#else
            VarianceNeighbourhood(samples, minNeighbour, maxNeighbour, stdDevOut);
#endif
        }

    // ------------------------------------------------------------------


        void FragTAA(Varyings input, out CTYPE outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float sharpenStrength = _TaaFrameInfo.x;
            float2 jitter = _TaaJitterStrength.zw;

            float2 uv = input.texcoord - jitter;

            float3 color = ReadAsYCoCg(_InputTexture, uv, 0.0, _RTHandleScale.xy);
            // Gather neigbourhood
            NeighbourhoodSamples samples;
            GatherNeighbourhood(uv, input.positionCS.xy, color, samples);

    #if defined(ORTHOGRAPHIC)
            // Don't dilate in ortho
            float2 closest = input.positionCS.xy;
    #else
            float2 closest = GetClosestFragment2(input.positionCS.xy);
    #endif

            float2 motionVector;
            DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, closest), motionVector);

            float3 history = GetFilteredHistory(input.texcoord - motionVector);

            // Find min/max
            float3 minNeighbour, maxNeighbour;
            float stdDevOut;
            GetNeighbourhoodCorners(samples, minNeighbour, maxNeighbour, stdDevOut);

            // Get luminance values (we are in YCoCg, so the luminance is the x channel)
            float colorLuma = color.x ;
            float historyLuma = history.x;


            // Clip history samples
    #if CLIP_AABB
            history = ClipToAABB2(history.xyz, minNeighbour.xyz, maxNeighbour.xyz);
    #else
            history = clamp(history, minNeighbour, maxNeighbour);
    #endif


            //flickerFactor = 1.0;
            // Blend color & history
            // Feedback weight from unbiased luminance diff (Timothy Lottes)
            float diff = abs(colorLuma - historyLuma) / Max3(0.2, colorLuma, historyLuma);
            float weight = 1.0 - diff * 1;
            float feedback = lerp(FEEDBACK_MIN, FEEDBACK_MAX, weight * weight);

            color.xyz = YCoCgToRGB(lerp(color.xyz, history.xyz, feedback));

            _OutputHistoryTexture[COORD_TEXTURE2D_X(input.positionCS.xy)] = color;
            outColor = color/* + float3(antiFlickerDist > 1.5 ? antiFlickerDist * 0.5 : 0, 0, 0)*/;
        }

        void FragExcludedTAA(Varyings input, out CTYPE outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 jitter = _TaaJitterStrength.zw;
            float2 uv = input.texcoord - jitter;

            outColor = Fetch4(_InputTexture, uv, 0.0, _RTHandleScale.xy).CTYPE_SWIZZLE;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        // TAA
        Pass
        {
            Stencil
            {
                ReadMask [_StencilMask]       // ExcludeFromTAA
                Ref [_StencilRef]          // ExcludeFromTAA
                Comp NotEqual
                Pass Keep
            }

            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragTAA
            ENDHLSL
        }

        // Excluded from TAA
        // Note: This is a straightup passthrough now, but it would be interesting instead to try to reduce history influence instead.
        Pass
        {
            Stencil
            {
                ReadMask [_StencilMask]    
                Ref     [_StencilRef]
                Comp Equal
                Pass Keep
            }

            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragExcludedTAA
            ENDHLSL
        }
    }
    Fallback Off
}
