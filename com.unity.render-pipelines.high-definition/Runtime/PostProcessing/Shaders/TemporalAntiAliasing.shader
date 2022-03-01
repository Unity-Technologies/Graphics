Shader "Hidden/HDRP/TemporalAA"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2
        [HideInInspector] _StencilMask("_StencilMask", Int) = 2
    }

    HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile_local_fragment _ ORTHOGRAPHIC
        #pragma multi_compile_local_fragment _ ENABLE_ALPHA
        #pragma multi_compile_local_fragment _ FORCE_BILINEAR_HISTORY
        #pragma multi_compile_local_fragment _ ENABLE_MV_REJECTION
        #pragma multi_compile_local_fragment _ ANTI_RINGING
        #pragma multi_compile_local_fragment _ DIRECT_STENCIL_SAMPLE
        #pragma multi_compile_local_fragment LOW_QUALITY MEDIUM_QUALITY HIGH_QUALITY TAA_UPSCALE POST_DOF

        #pragma editor_sync_compilation
       // #pragma enable_d3d11_debug_symbols

        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"


        // ---------------------------------------------------
        // Tier definitions
        // ---------------------------------------------------
        //  TODO: YCoCg gives better result in terms of ghosting reduction, but it also seems to let through
        //  some additional aliasing that is undesirable in some occasions. Would like to investigate better.
#ifdef LOW_QUALITY
    #define YCOCG 0
    #define HISTORY_SAMPLING_METHOD BILINEAR
    #define WIDE_NEIGHBOURHOOD 0
    #define NEIGHBOUROOD_CORNER_METHOD MINMAX
    #define CENTRAL_FILTERING NO_FILTERING
    #define HISTORY_CLIP SIMPLE_CLAMP
    #define ANTI_FLICKER 0
    #define VELOCITY_REJECTION (defined(ENABLE_MV_REJECTION) && 0)
    #define PERCEPTUAL_SPACE 0
    #define PERCEPTUAL_SPACE_ONLY_END 1 && (PERCEPTUAL_SPACE == 0)

#elif defined(MEDIUM_QUALITY)
    #define YCOCG 1
    #define HISTORY_SAMPLING_METHOD BICUBIC_5TAP
    #define WIDE_NEIGHBOURHOOD 0
    #define NEIGHBOUROOD_CORNER_METHOD VARIANCE
    #define CENTRAL_FILTERING NO_FILTERING
    #define HISTORY_CLIP DIRECT_CLIP
    #define ANTI_FLICKER 1
    #define ANTI_FLICKER_MV_DEPENDENT 0
    #define VELOCITY_REJECTION (defined(ENABLE_MV_REJECTION) && 0)
    #define PERCEPTUAL_SPACE 1
    #define PERCEPTUAL_SPACE_ONLY_END 0 && (PERCEPTUAL_SPACE == 0)


#elif defined(HIGH_QUALITY) // TODO: We can do better in term of quality here (e.g. subpixel changes etc) and can be optimized a bit more
    #define YCOCG 1
    #define HISTORY_SAMPLING_METHOD BICUBIC_5TAP
    #define WIDE_NEIGHBOURHOOD 1
    #define NEIGHBOUROOD_CORNER_METHOD VARIANCE
    #define CENTRAL_FILTERING BLACKMAN_HARRIS
    #define HISTORY_CLIP DIRECT_CLIP
    #define ANTI_FLICKER 1
    #define ANTI_FLICKER_MV_DEPENDENT 1
    #define VELOCITY_REJECTION defined(ENABLE_MV_REJECTION)
    #define PERCEPTUAL_SPACE 1
    #define PERCEPTUAL_SPACE_ONLY_END 0 && (PERCEPTUAL_SPACE == 0)

#elif defined(TAA_UPSCALE)
    #define YCOCG 1
    #define HISTORY_SAMPLING_METHOD BICUBIC_5TAP
    #define WIDE_NEIGHBOURHOOD 1
    #define NEIGHBOUROOD_CORNER_METHOD VARIANCE
    #define CENTRAL_FILTERING UPSCALE
    #define HISTORY_CLIP DIRECT_CLIP
    #define ANTI_FLICKER 1
    #define ANTI_FLICKER_MV_DEPENDENT 1
    #define VELOCITY_REJECTION defined(ENABLE_MV_REJECTION)
    #define PERCEPTUAL_SPACE 1
    #define PERCEPTUAL_SPACE_ONLY_END 0 && (PERCEPTUAL_SPACE == 0)

#elif defined(POST_DOF)
    #define YCOCG 1
    #define HISTORY_SAMPLING_METHOD BILINEAR
    #define WIDE_NEIGHBOURHOOD 0
    #define NEIGHBOUROOD_CORNER_METHOD VARIANCE
    #define CENTRAL_FILTERING NO_FILTERING
    #define HISTORY_CLIP DIRECT_CLIP
    #define ANTI_FLICKER 1
    #define ANTI_FLICKER_MV_DEPENDENT 1
    #define VELOCITY_REJECTION defined(ENABLE_MV_REJECTION)
    #define PERCEPTUAL_SPACE 1
    #define PERCEPTUAL_SPACE_ONLY_END 0 && (PERCEPTUAL_SPACE == 0)

#endif


        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/TemporalAntialiasing.hlsl"

        TEXTURE2D_X(_DepthTexture);
        TEXTURE2D_X(_InputTexture);
        TEXTURE2D_X(_InputHistoryTexture);
        #ifdef SHADER_API_PSSL
        RW_TEXTURE2D_X(CTYPE, _OutputHistoryTexture) : register(u0);
        #else
        RW_TEXTURE2D_X(CTYPE, _OutputHistoryTexture) : register(u1);
        #endif

        #if DIRECT_STENCIL_SAMPLE
        TEXTURE2D_X_UINT2(_StencilTexture);
        #endif

        float4 _TaaPostParameters;
        float4 _TaaPostParameters1;
        float4 _TaaHistorySize;
        float4 _TaaFilterWeights;
        float4 _TaaFilterWeights1;

        #define _HistorySharpening _TaaPostParameters.x
        #define _AntiFlickerIntensity _TaaPostParameters.y
        #define _SpeedRejectionIntensity _TaaPostParameters.z
        #define _ContrastForMaxAntiFlicker _TaaPostParameters.w

        #define _BaseBlendFactor _TaaPostParameters1.x
        #define _CentralWeight _TaaPostParameters1.y
        #define _ExcludeTAABit (uint)_TaaPostParameters1.z
        #define _NormalizedSpeedRejectionFactor _TaaPostParameters1.w

        // TAAU specific
        float4 _TaauParameters;
        #define _TAAUFilterRcpSigma2 _TaauParameters.x
        #define _TAAUScale _TaauParameters.y
        #define _TAAUBoxConfidenceThresh _TaauParameters.z
        #define _TAAURenderScale _TaauParameters.w
        #define _InputSize _ScreenSize


        float4 _TaaScales;
        // NOTE: We need to define custom scales instead of using the default ones for several reasons.
        // 1- This shader is shared by TAA and Temporal Upscaling, having a single scale defined in C# instead helps readability.
        // 2- Especially with history, when doing temporal upscaling we have an unusal situation in which the history size doesn't match the input size.
        //    This in turns lead to some rounding issue (final viewport is not rounded, while the render target size is) that cause artifacts.
        //    To fix said artifacts we recompute manually the scales as we need them.
        #define _RTHandleScaleForTAAHistory _TaaScales.xy
        #define _RTHandleScaleForTAA _TaaScales.zw

#if VELOCITY_REJECTION
        TEXTURE2D_X(_InputVelocityMagnitudeHistory);
        #ifdef SHADER_API_PSSL
        RW_TEXTURE2D_X(float, _OutputVelocityMagnitudeHistory) : register(u1);
        #else
        RW_TEXTURE2D_X(float, _OutputVelocityMagnitudeHistory) : register(u2);
        #endif
#endif

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

    // ------------------------------------------------------------------

        // This complexity will not be needed when moving to CS.
        void SwizzleFilterWeights(int2 posSS, inout float4 filterParams1, inout float4 filterParams2)
        {
            // Data arrives as if filterParams weights for { (0, 1), (1, 0), (-1, 0), (0,-1) }, filterParams2 for { (-1, 1), (1, -1), (1, 1), (-1, -1) }
            bool2 needSwizzle = (posSS & 1) == 0;

            filterParams1.yz = needSwizzle.x ? filterParams1.zy : filterParams1.yz;
            filterParams1.xw = needSwizzle.y ? filterParams1.wx : filterParams1.xw;
#if WIDE_NEIGHBOURHOOD
            filterParams2 = all(needSwizzle) ? filterParams2.yxwz :
                (needSwizzle.x && !needSwizzle.y) ? filterParams2.zwxy :
                (!needSwizzle.x && needSwizzle.y) ? filterParams2.wzyx : filterParams2;
#endif
        }



        float mitchell_netravali(float x) {
            float B = 1.0 / 3.0;
            float C = 1.0 / 3.0;

            float ax = abs(x);
            if (ax < 1) {
                return ((12 - 9 * B - 6 * C) * ax * ax * ax + (-18 + 12 * B + 6 * C) * ax * ax + (6 - 2 * B)) / 6;
            }
            else if ((ax >= 1) && (ax < 2)) {
                return ((-B - 6 * C) * ax * ax * ax + (6 * B + 30 * C) * ax * ax + (-12 * B - 48 * C) * ax + (8 * B + 24 * C)) / 6;
            }
            else {
                return 0;
            }
        }

        float3 FilterColorTest(int2 output_px, float2 sample_offset_pixels)
        {
            sample_offset_pixels = _TaaJitterStrength.zw;

            const float2 input_tex_size = _InputSize.xy;
            const float2 input_resolution_scale = _TAAURenderScale;
            const int2 base_src_px = int2((output_px + 0.5) * input_resolution_scale);
            // In pixel units of the destination (upsampled)
            const float2 dst_sample_loc = output_px + 0.5;
            const float2 base_src_sample_loc =
                (base_src_px + 0.5 + sample_offset_pixels * float2(1, -1)) / input_resolution_scale;

            float3 res = 0.0;
            float dev_wt_sum = 0.0;
            float wt_sum = 0.0;

            const float kernel_distance_mult = 1;
            int k = 1;

            for (int y = -k; y <= k; ++y)
            {
                for (int x = -k; x <= k; ++x)
                {
                    int2 src_px = base_src_px + int2(x, y);
                    float2 src_sample_loc = base_src_sample_loc + float2(x, y) / input_resolution_scale;
                    float3 col = _InputTexture[COORD_TEXTURE2D_X(src_px.xy)];
                    float2 sample_center_offset = (src_sample_loc - dst_sample_loc) * kernel_distance_mult;
                    float dist2 = dot(sample_center_offset, sample_center_offset);
                    float dist = sqrt(dist2);
                    //float wt = mitchell_netravali(2.5 * dist * input_resolution_scale.x);
                    float wt = exp2(-15 * dist2 * input_resolution_scale.x);
                    res += col * wt;
                    wt_sum += wt;
                }
            }

            return res / wt_sum;
        }

        float3 FilterColorTest(int2 output_px, float2 sample_offset_pixels, NeighbourhoodSamples samples)
        {
            sample_offset_pixels = _TaaJitterStrength.zw;

            const float2 input_tex_size = _InputSize.xy;
            const float2 input_resolution_scale = rcp(_TAAUScale);
            const int2 base_src_px = int2((output_px + 0.5) * input_resolution_scale);
            // In pixel units of the destination (upsampled)
            const float2 dst_sample_loc = output_px + 0.5;
            const float2 base_src_sample_loc =
                (base_src_px + 0.5 + sample_offset_pixels * float2(1, -1)) / input_resolution_scale;

            float3 res = 0.0;
            float dev_wt_sum = 0.0;
            float wt_sum = 0.0;

            const float kernel_distance_mult = 1.0;

#if WIDE_NEIGHBOURHOOD
            //int k = 1;
            res = samples.central;
            float2 sample_center_offset = (base_src_sample_loc - dst_sample_loc);
            float2 dist2 = dot(sample_center_offset, sample_center_offset);

            wt_sum = mitchell_netravali(2.5 * sqrt(dist2) * input_resolution_scale.x);
            res *= wt_sum;

            for (int i = 0; i < 8; ++i)
            {

                int x = samples.offsets[i].x;
                int y = samples.offsets[i].y;
                float3 col = samples.neighbours[i];

                int2 src_px = base_src_px + int2(x, y);
                float2 src_sample_loc = base_src_sample_loc + float2(x, y) / input_resolution_scale;
                sample_center_offset = (src_sample_loc - dst_sample_loc);
                dist2 = dot(sample_center_offset, sample_center_offset);
                float dist = sqrt(dist2);
                float wt = mitchell_netravali(2.5 * dist * input_resolution_scale.x);
                //float wt = exp2(-10 * dist2 * input_resolution_scale.x);
                res += col * wt;
                wt_sum += wt;

            }


#else
            return 1;
#endif
            return res / wt_sum;
        }

        void FragTAA(Varyings input, out CTYPE outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float sharpenStrength = _TaaFrameInfo.x;
            float2 jitter = _TaaJitterStrength.zw;

            float2 uv = input.texcoord + jitter;

            #ifdef TAA_UPSCALE
            float2 outputPixInInput = input.texcoord * _InputSize.xy + _TaaJitterStrength.xy;

            uv = _InputSize.zw * (0.5f + floor(outputPixInInput));
            #endif

            // --------------- Get closest motion vector ---------------
            float2 motionVector;
            float blendFactor = 0;
            int2 samplePos = input.positionCS.xy;

#if ORTHOGRAPHIC
            float2 closestOffset = 0;
#else

#ifdef TAA_UPSCALE
            samplePos = outputPixInInput;
#endif
            float closestDepth, centralDepth;
            // TODO_FCC: !!!!! IMPORTANT !!!! This doesn't work great with the speed rejection, why?
            float2 closestOffset =  GetClosestFragmentOffset(_DepthTexture, samplePos, closestDepth, centralDepth);
#endif
            bool excludeTAABit = false;
#if DIRECT_STENCIL_SAMPLE
            uint stencil = GetStencilValue(LOAD_TEXTURE2D_X(_StencilTexture, samplePos));
            excludeTAABit = (stencil == _ExcludeTAABit);
#endif

            float lengthMV = 0;

            DecodeMotionVector(SAMPLE_TEXTURE2D_X_LOD(_CameraMotionVectorsTexture, s_point_clamp_sampler, ClampAndScaleUVForPoint(uv + closestOffset * _InputSize.zw), 0), motionVector);
            // --------------------------------------------------------

            // --------------- Get resampled history ---------------
            float2 prevUV = input.texcoord - motionVector;

            CTYPE history = GetFilteredHistory(_InputHistoryTexture, prevUV, _HistorySharpening, _TaaHistorySize, _RTHandleScaleForTAAHistory);
            bool offScreen = any(abs(prevUV * 2 - 1) >= (1.0f - (1.0 * _TaaHistorySize.zw)));
            history.xyz *= PerceptualWeight(history);
            // -----------------------------------------------------

            // --------------- Gather neigbourhood data ---------------
            CTYPE color = Fetch4(_InputTexture, uv, 0.0, _RTHandleScaleForTAA).CTYPE_SWIZZLE;
            if (!excludeTAABit)
            {
                color = clamp(color, 0, CLAMP_MAX);
                color = ConvertToWorkingSpace(color);

                NeighbourhoodSamples samples;
                GatherNeighbourhood(_InputTexture, uv, floor(input.positionCS.xy), color, _RTHandleScaleForTAA, samples);
                // --------------------------------------------------------

                // --------------- Filter central sample ---------------
                float4 filterParams = _TaaFilterWeights;
                float4 filterParams1 = _TaaFilterWeights1;
                float centralWeight = _CentralWeight;
#ifdef TAA_UPSCALE
                filterParams.x = _TAAUFilterRcpSigma2;
                filterParams.y = _TAAUScale;
                filterParams.zw = outputPixInInput - (floor(outputPixInInput) + 0.5f);

#elif CENTRAL_FILTERING  == BLACKMAN_HARRIS
                // We need to swizzle weights as we use quad communication to access neighbours, so the order of neighbours is not always the same (this needs to go away when moving back to compute)
                SwizzleFilterWeights(floor(input.positionCS.xy), filterParams, filterParams1);
#endif
                CTYPE filteredColor = FilterCentralColor(samples, filterParams, filterParams1, centralWeight);
                filteredColor = FilterColorTest(input.positionCS.xy, _TaaJitterStrength.xy, samples);
                // ------------------------------------------------------

                if (offScreen)
                    history = filteredColor;

                // --------------- Get neighbourhood information and clamp history ---------------
                float colorLuma = GetLuma(filteredColor);
                float historyLuma = GetLuma(history);

                float motionVectorLength = 0.0f;
                float motionVectorLenInPixels = 0.0f;

#if ANTI_FLICKER_MV_DEPENDENT || VELOCITY_REJECTION
                motionVectorLength = length(motionVector);
                motionVectorLenInPixels = motionVectorLength * length(_InputSize.xy);

                float objOnlyMVLen = length(motionVector - GrabCameraMotionVec(uv + closestOffset * _InputSize.zw, closestDepth));
#endif

                float stdDev = GetNeighbourhoodCorners(samples, historyLuma, colorLuma, float2(_AntiFlickerIntensity, _ContrastForMaxAntiFlicker), motionVectorLenInPixels, _TAAURenderScale, _NormalizedSpeedRejectionFactor);

                history = GetClippedHistory(filteredColor, history, samples.minNeighbour, samples.maxNeighbour);
                filteredColor = SharpenColor(samples, filteredColor, sharpenStrength);
                // ------------------------------------------------------------------------------

                // --------------- Compute blend factor for history ---------------
                blendFactor = GetBlendFactor(colorLuma, historyLuma, GetLuma(samples.minNeighbour), GetLuma(samples.maxNeighbour), _BaseBlendFactor);
                // --------------------------------------------------------

                // ------------------- Alpha handling ---------------------------
#if defined(ENABLE_ALPHA)
                // Compute the antialiased alpha value
                filteredColor.w = lerp(history.w, filteredColor.w, blendFactor);
                // TAA should not overwrite pixels with zero alpha. This allows camera stacking with mixed TAA settings (bottom camera with TAA OFF and top camera with TAA ON).
                CTYPE unjitteredColor = Fetch4(_InputTexture, input.texcoord - color.w * jitter, 0.0, _RTHandleScale.xy).CTYPE_SWIZZLE;
                unjitteredColor = ConvertToWorkingSpace(unjitteredColor);
                unjitteredColor.xyz *= PerceptualWeight(unjitteredColor);
                filteredColor.xyz = lerp(unjitteredColor.xyz, filteredColor.xyz, filteredColor.w);
                blendFactor = color.w > 0 ? blendFactor : 1;
#endif
                // ---------------------------------------------------------------

                // --------------- Blend to final value and output ---------------

#if VELOCITY_REJECTION
                // The 10 multiplier serves a double purpose, it is an empirical scale value used to perform the rejection and it also helps with storing the value itself.
                lengthMV = objOnlyMVLen * 10;
                blendFactor = ModifyBlendWithMotionVectorRejection(_InputVelocityMagnitudeHistory, lengthMV, prevUV, blendFactor, _SpeedRejectionIntensity, _RTHandleScaleForTAAHistory);
#endif

#ifdef TAA_UPSCALE
                blendFactor *= GetUpsampleConfidence(filterParams.zw, _TAAUBoxConfidenceThresh, _TAAUFilterRcpSigma2, _TAAUScale);
#endif
                blendFactor = max(blendFactor, 0.03);

                CTYPE finalColor;
#if PERCEPTUAL_SPACE_ONLY_END
                finalColor.xyz = lerp(ReinhardToneMap(history).xyz, ReinhardToneMap(filteredColor).xyz, blendFactor);
                finalColor.xyz = InverseReinhardToneMap(finalColor).xyz;
#else
                finalColor.xyz = lerp(history.xyz, filteredColor.xyz, blendFactor);
                finalColor.xyz *= PerceptualInvWeight(finalColor);
#endif

                color.xyz = ConvertToOutputSpace(finalColor.xyz);
                color.xyz = clamp(color.xyz, 0, CLAMP_MAX);
#if defined(ENABLE_ALPHA)
                // Set output alpha to the antialiased alpha.
                color.w = filteredColor.w;
#endif
            }


            bool rejectionEvent = blendFactor > 0.25;
            //  rejectionEvent = false;
            if (rejectionEvent)
            {
                color.xyz = AlternateAA(_InputTexture, color, samplePos, uv);
            }
            else
            {
                color = color.CTYPE_SWIZZLE + (rejectionEvent ? float4(10, 0, 0, 1).CTYPE_SWIZZLE : 0);
            }

            _OutputHistoryTexture[COORD_TEXTURE2D_X(input.positionCS.xy)] = color.CTYPE_SWIZZLE;
            outColor = color.CTYPE_SWIZZLE;
#if VELOCITY_REJECTION && !defined(POST_DOF)
            _OutputVelocityMagnitudeHistory[COORD_TEXTURE2D_X(input.positionCS.xy)] = lengthMV;
#endif
            // -------------------------------------------------------------
        }

        void FragExcludedTAA(Varyings input, out CTYPE outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 jitter = _TaaJitterStrength.zw;
            float2 uv = input.texcoord - jitter;

            outColor = Fetch4(_InputTexture, uv, 0.0, _RTHandleScale.xy).CTYPE_SWIZZLE;
        }

        void FragCopyHistory(Varyings input, out CTYPE outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 jitter = _TaaJitterStrength.zw;
            float2 uv = input.texcoord;

#ifdef TAA_UPSCALE
            float2 outputPixInInput = input.texcoord * _InputSize.xy - _TaaJitterStrength.xy;

            uv = _InputSize.zw * (0.5f + floor(outputPixInInput));
#endif
            CTYPE color = Fetch4(_InputTexture, uv, 0.0, _RTHandleScaleForTAA).CTYPE_SWIZZLE;

            outColor = color;
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

        Pass // TAAU
        {
            // We cannot stencil with TAAU, we will need to manually sample the texture.

            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragTAA
            ENDHLSL
        }

        Pass // Copy history
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCopyHistory
            ENDHLSL
        }

    }
    Fallback Off
}
