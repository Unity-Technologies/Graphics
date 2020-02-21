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
        float3 QuadReadAcrossDiagonal_3(float3 val, int2 positionSS)
        {
            return float3(QuadReadAcrossDiagonal(val.x, positionSS), QuadReadAcrossDiagonal(val.y, positionSS), QuadReadAcrossDiagonal(val.z, positionSS));
        }


        float2 GetQuadOffset_other(int2 screenPos)
        {
            return float2(float(screenPos.x & 1) * 2.0 - 1.0, float(screenPos.y & 1) * 2.0 - 1.0);
        }

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

        float GetLuma(float3 color)
        {
            // We work in YCoCg hence the luminance is in the first channel.
            return color.x;
        }

        // ---------------------------------------------------
        // History sampling 
        // ---------------------------------------------------

        // TODO: Check to avoid sampling outside the screen!

        #define BILINEAR 0
        #define BICUBIC_5TAP 1
        #define HISTORY_SAMPLING_METHOD BICUBIC_5TAP

        float3 HistoryBilinear(float2 UV)
        {
            float4 rgb = Fetch4(_InputHistoryTexture, UV, 0.0, _RTHandleScaleHistory.xy);
            return rgb.xyz;
        }

        // From Filmic SMAA presentation[Jimenez 2016]
        float3 HistoryBicubic5Tap(float2 UV)
        {
            float4 texSize = _ScreenSize * float4(_RTHandleScaleHistory.xy, rcp(_RTHandleScaleHistory.xy));
            const float sharpening = -0.4;  

            float2 samplePos = UV * texSize.xy;
            float2 tc1 = floor(samplePos - 0.5) + 0.5;
            float2 f = samplePos - tc1;
            float2 f2 = f * f;
            float2 f3 = f * f2;

            const float c = 0.5 + sharpening;

            float2 w0 =         -c * f3 +  2.0 * c        * f2 - c * f;
            float2 w1 =  (2.0 - c) * f3 - (3.0 - c)        * f2 + 1.0;
            float2 w2 = -(2.0 - c) * f3 +  (3.0 - 2.0 * c) * f2 + c * f;
            float2 w3 =          c * f3 -                c * f2;

            float2 w12 = w1 + w2;
            float2 tc0  = texSize.zw  * (tc1 - 1.0);
            float2 tc3  = texSize.zw  * (tc1 + 2.0);
            float2 tc12 = texSize.zw  * (tc1 + w2 / w12);

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
            history = HistoryBilinear(UV);
#elif HISTORY_SAMPLING_METHOD == BICUBIC_5TAP
            history = HistoryBicubic5Tap(UV);
#endif

            return RGBToYCoCg(history);
        }

        // ---------------------------------------------------
        // Neighbourhood handling.
        // ---------------------------------------------------

        #define PLUS 0    // Faster! Can allow for read across twice (paying cost of 2 samples only)
        #define CROSS 1   // Can only do one fast read diagonal 
        #define SMALL_NEIGHBOURHOOD_SHAPE PLUS

        #define SMALL_NEIGHBOURHOOD_SIZE 4 
        // If 0, the neighbourhood is smaller, if 1 the neighbourhood is 9 samples (full 3x3)
        #define WIDE_NEIGHBOURHOOD 0

        #define NEIGHBOUR_COUNT ((WIDE_NEIGHBOURHOOD == 0) ? 4 : 8)

        struct NeighbourhoodSamples
        {
            float3 neighbours[8];
            float3 central;
            float3 minNeighbour;
            float3 maxNeighbour;
            float3 avgNeighbour;
        };

        void GatherNeighbourhood(float2 UV, float2 positionSS, float3 centralColor, out NeighbourhoodSamples samples)
        {
            samples = (NeighbourhoodSamples)0; // TODO_FCC VERIFY

            samples.central = centralColor;

            float2 quadOffset = GetQuadOffset_other(positionSS);

#if WIDE_NEIGHBOURHOOD

            // Plus shape
            samples.neighbours[0] = ReadAsYCoCg(_InputTexture, UV, float2(0.0f, quadOffset.y), _RTHandleScale.xy);
            samples.neighbours[1] = ReadAsYCoCg(_InputTexture, UV, float2(quadOffset.x, 0.0f), _RTHandleScale.xy);
            samples.neighbours[2] = QuadReadAcrossX_3(centralColor, positionSS);
            samples.neighbours[3] = QuadReadAcrossY_3(centralColor, positionSS);

            // Cross shape
            int2 fastOffset = int2(quadOffset.x > 0 ? -1 : 1, quadOffset.y > 0 ? 1 : -1);
            int2 offset1 = (quadOffset.x == quadOffset.y) ? int2(1, 1) : int2(-1, 1);
            int2 offset2 = (quadOffset.x == 0 && quadOffset.y == 1) ? int2(1, 1) : int2(-1, -1);
            int2 offset3 = (quadOffset.x == 0 && quadOffset.y == 0) ? int2(-1, 1) : int2(1, -1);

            samples.neighbours[4] = QuadReadAcrossDiagonal_3(centralColor, positionSS);
            samples.neighbours[5] = ReadAsYCoCg(_InputTexture, UV, offset1, _RTHandleScale.xy);
            samples.neighbours[6] = ReadAsYCoCg(_InputTexture, UV, offset2, _RTHandleScale.xy);
            samples.neighbours[7] = ReadAsYCoCg(_InputTexture, UV, offset3, _RTHandleScale.xy);

#else // !WIDE_NEIGHBOURHOOD

#if SMALL_NEIGHBOURHOOD_SHAPE == PLUS


            samples.neighbours[0] = ReadAsYCoCg(_InputTexture, UV, float2(0.0f, quadOffset.y), _RTHandleScale.xy);
            samples.neighbours[1] = ReadAsYCoCg(_InputTexture, UV, float2(quadOffset.x, 0.0f), _RTHandleScale.xy);
            samples.neighbours[2] = QuadReadAcrossX_3(centralColor, positionSS);
            samples.neighbours[3] = QuadReadAcrossY_3(centralColor, positionSS);

#else // SMALL_NEIGHBOURHOOD_SHAPE == CROSS

            int2 fastOffset = int2(quadOffset.x > 0 ? -1 : 1, quadOffset.y > 0 ? 1 : -1);
            int2 offset1 = (quadOffset.x == quadOffset.y) ? int2(1, 1) : int2(-1, 1);
            int2 offset2 = (quadOffset.x == 0 && quadOffset.y == 1) ? int2(1, 1) : int2(-1, -1);
            int2 offset3 = (quadOffset.x == 0 && quadOffset.y == 0) ? int2(-1, 1) : int2(1, -1);

            samples.neighbours[0] = QuadReadAcrossDiagonal_3(centralColor, positionSS);
            samples.neighbours[1] = ReadAsYCoCg(_InputTexture, UV, offset1, _RTHandleScale.xy);
            samples.neighbours[2] = ReadAsYCoCg(_InputTexture, UV, offset2, _RTHandleScale.xy);
            samples.neighbours[3] = ReadAsYCoCg(_InputTexture, UV, offset3, _RTHandleScale.xy);

#endif // SMALL_NEIGHBOURHOOD_SHAPE == 5

#endif // !WIDE_NEIGHBOURHOOD
        }

        void MinMaxNeighbourhood(inout NeighbourhoodSamples samples, out float3 minNeighbour, out float3 maxNeighbour)
        {
            // We always have at least the first 4 neighbours.
            samples.minNeighbour = MinColor(samples.neighbours[0], samples.neighbours[1], samples.neighbours[2]);
            samples.minNeighbour = MinColor(samples.minNeighbour, samples.central, samples.neighbours[3]);

            samples.maxNeighbour = MaxColor(samples.neighbours[0], samples.neighbours[1], samples.neighbours[2]);
            samples.maxNeighbour = MaxColor(samples.maxNeighbour, samples.central, samples.neighbours[3]);

#if WIDE_NEIGHBOURHOOD
            samples.minNeighbour = MinColor(samples.minNeighbour, samples.neighbours[4], samples.neighbours[5]);
            samples.minNeighbour = MinColor(samples.minNeighbour, samples.neighbours[6], samples.neighbours[7]);

            samples.maxNeighbour = MaxColor(samples.maxNeighbour, samples.neighbours[4], samples.neighbours[5]);
            samples.maxNeighbour = MaxColor(samples.maxNeighbour, samples.neighbours[6], samples.neighbours[7]);
#endif
        }

        void VarianceNeighbourhood(inout NeighbourhoodSamples samples, out float stdDevOut)
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
            stDevMultiplier = 1.5;
            stdDevOut = stdDev;
            samples.minNeighbour = moment1 - stDevMultiplier * stdDev;
            samples.maxNeighbour = moment1 + stDevMultiplier * stdDev;
        }

#define MINMAX 0
#define VARIANCE 1
#define NEIGHBOUROOD_CORNER_METHOD VARIANCE

        void GetNeighbourhoodCorners(inout NeighbourhoodSamples samples, out float stdDevOut)
        {
            stdDevOut = 0;
#if NEIGHBOUROOD_CORNER_METHOD == MINMAX
            MinMaxNeighbourhood(samples);
#else
            VarianceNeighbourhood(samples, stdDevOut);
#endif
        }

        // ---------------------------------------------------
        // Filter main color
        // ---------------------------------------------------
        float3 FilterCentralColor(NeighbourhoodSamples samples)
        {
            float3 avg = samples.central;
            for (int i = 0; i < NEIGHBOUR_COUNT; ++i)
            {
                avg += samples.neighbours[i];
            }
             return avg / (1+NEIGHBOUR_COUNT);
            return  samples.central;// 
        }


        // ---------------------------------------------------
        // Blend factor calculation
        // ---------------------------------------------------

#define OLD_FEEDBACK 0
#define LUMA_AABB_HISTORY_CONTRAST 1
#define JIMENEZ 2
#define BLEND_FACTOR_METHOD LUMA_AABB_HISTORY_CONTRAST

        float OldLuminanceDiff(float colorLuma, float historyLuma)
        {
            float diff = abs(colorLuma - historyLuma) / Max3(0.2, colorLuma, historyLuma);
            float weight = 1.0 - diff;
            float feedback = lerp(FEEDBACK_MIN, FEEDBACK_MAX, weight * weight);
            return 1.0f - feedback;
        }

        float FBLuminanceDiff(float historyLuma, float minNeighbourLuma, float maxNeighbourLuma)
        {
            // We have 8 frames
            float baseContribution = 0.125f;
            float lumaContrast = max(maxNeighbourLuma - minNeighbourLuma, 0) / historyLuma;

            // TODO_FCC : Antiflicker here
            return saturate(baseContribution / (1.0f + lumaContrast));
        }

        float3 SpatialContrast(NeighbourhoodSamples samples)
        {

        }

        float JimenezWeigth(float historyLuma, float minNeighbourLuma, float maxNeighbourLuma)
        {
            // We have 8 frames
            float baseContribution = 0.125f;
            float lumaContrast = max(maxNeighbourLuma - minNeighbourLuma, 0) / historyLuma;

            // TODO_FCC : Antiflicker here
            return saturate(baseContribution / (1.0f + lumaContrast));
        }

        float GetBlendFactor(float colorLuma, float historyLuma, float minNeighbourLuma, float maxNeighbourLuma)
        {
#if BLEND_FACTOR_METHOD == OLD_FEEDBACK
            return OldLuminanceDiff(colorLuma, historyLuma);
#elif BLEND_FACTOR_METHOD == LUMA_AABB_HISTORY_CONTRAST
            return FBLuminanceDiff(historyLuma, minNeighbourLuma, maxNeighbourLuma);
#endif
            return 0.95;
        }

    // ------------------------------------------------------------------


        void FragTAA(Varyings input, out CTYPE outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float sharpenStrength = _TaaFrameInfo.x;
            float2 jitter = _TaaJitterStrength.zw;

            float2 uv = input.texcoord - jitter;

            // --------------- Gather neigbourhood data --------------- 
            float3 color = ReadAsYCoCg(_InputTexture, uv, 0.0, _RTHandleScale.xy);
            NeighbourhoodSamples samples;
            GatherNeighbourhood(uv, input.positionCS.xy, color, samples);
            // --------------------------------------------------------

            // --------------- Filter central sample ---------------
            color = FilterCentralColor(samples);
            // ------------------------------------------------------


            // --------------- Get closest motion vector --------------- 
    #if defined(ORTHOGRAPHIC)
            float2 closest = input.positionCS.xy;
    #else
            // Front most neighbourhood velocity ([Karis 2014])
            float2 closest = GetClosestFragment2(input.positionCS.xy);
    #endif
            float2 motionVector;
            DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, closest), motionVector);
            // --------------------------------------------------------

            // --------------- Get resampled history --------------- 
            float3 history = GetFilteredHistory(input.texcoord - motionVector);
            // -----------------------------------------------------

            // --------------- Get neighbourhood information and clamp history --------------- 
            float stdDevOut;
            GetNeighbourhoodCorners(samples, stdDevOut);

            float colorLuma = GetLuma(color);
            float historyLuma = GetLuma(history);

    #if CLIP_AABB
            history = ClipToAABB2(history.xyz, samples.minNeighbour.xyz, samples.maxNeighbour.xyz);
    #else
            history = clamp(history, samples.minNeighbour, samples.maxNeighbour);
    #endif
            // ------------------------------------------------------------------------------

            // --------------- Compute blend factor for history ---------------

            // Feedback weight from unbiased luminance diff (Timothy Lottes)
            float feedback = GetBlendFactor(colorLuma, historyLuma, GetLuma(samples.minNeighbour), GetLuma(samples.maxNeighbour));
            // --------------------------------------------------------

            // --------------- Blend to final value and output --------------- 
            color.xyz = YCoCgToRGB(lerp(history.xyz, color.xyz, feedback));

            _OutputHistoryTexture[COORD_TEXTURE2D_X(input.positionCS.xy)] = color;
            outColor = color;
            // -------------------------------------------------------------
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
