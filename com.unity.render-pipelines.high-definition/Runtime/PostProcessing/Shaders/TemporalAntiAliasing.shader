Shader "Hidden/HDRP/TemporalAA"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2
        [HideInInspector] _StencilMask("_StencilMask", Int) = 2
    }

    HLSLINCLUDE

        #pragma target 4.5
        #pragma multi_compile_local _ ORTHOGRAPHIC
        #pragma multi_compile_local _ ENABLE_ALPHA
        #pragma multi_compile_local _ FORCE_BILINEAR_HISTORY
        #pragma multi_compile_local _ ENABLE_MV_REJECTION
        #pragma multi_compile_local _ ANTI_RINGING
        #pragma multi_compile_local LOW_QUALITY MEDIUM_QUALITY HIGH_QUALITY POST_DOF

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

#elif defined(POST_DOF)
    #define YCOCG 1     
    #define HISTORY_SAMPLING_METHOD BILINEAR
    #define WIDE_NEIGHBOURHOOD 0
    #define NEIGHBOUROOD_CORNER_METHOD VARIANCE
    #define CENTRAL_FILTERING NO_FILTERINGs
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

        #define _HistorySharpening _TaaPostParameters.x
        #define _AntiFlickerIntensity _TaaPostParameters.y
        #define _SpeedRejectionIntensity _TaaPostParameters.z
        #define _ContrastForMaxAntiFlicker _TaaPostParameters.w

#if VELOCITY_REJECTION
        TEXTURE2D_X(_InputVelocityMagnitudeHistory);
        #ifdef SHADER_API_PSSL
        RW_TEXTURE2D_X(float, _OutputVelocityMagnitudeHistory) : register(u1);
        #else
        RW_TEXTURE2D_X(float, _OutputVelocityMagnitudeHistory) : register(u2);
        #endif
#endif

        float4 _TaaPostParameters;
        float4 _TaaHistorySize;
        float4 _TaaFilterWeights;

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

        void FragTAA(Varyings input, out CTYPE outColor : SV_Target0)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float sharpenStrength = _TaaFrameInfo.x;
            float2 jitter = _TaaJitterStrength.zw;

            float2 uv = input.texcoord - jitter;

            // --------------- Get closest motion vector ---------------
            float2 motionVector;

#if ORTHOGRAPHIC
            float2 closest = input.positionCS.xy;
#else
            float2 closest = GetClosestFragment(_DepthTexture, int2(input.positionCS.xy));
#endif
            DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, closest), motionVector);
            // --------------------------------------------------------

            // --------------- Get resampled history ---------------
            float2 prevUV = input.texcoord - motionVector;

            CTYPE history = GetFilteredHistory(_InputHistoryTexture, prevUV, _HistorySharpening, _TaaHistorySize);
            bool offScreen = any(abs(prevUV * 2 - 1) >= (1.0f - (1.0 * _TaaHistorySize.zw)));
            history.xyz *= PerceptualWeight(history);
            // -----------------------------------------------------

            // --------------- Gather neigbourhood data --------------- 
            CTYPE color = Fetch4(_InputTexture, uv, 0.0, _RTHandleScale.xy).CTYPE_SWIZZLE;
            color = clamp(color, 0, CLAMP_MAX);
            color = ConvertToWorkingSpace(color);

            NeighbourhoodSamples samples;
            GatherNeighbourhood(_InputTexture, uv, input.positionCS.xy, color, samples);
            // --------------------------------------------------------

            // --------------- Filter central sample ---------------
            CTYPE filteredColor = FilterCentralColor(samples, _TaaFilterWeights);
            // ------------------------------------------------------

            if (offScreen)
                history = filteredColor;

            // --------------- Get neighbourhood information and clamp history --------------- 
            float colorLuma = GetLuma(filteredColor);
            float historyLuma = GetLuma(history);

#if ANTI_FLICKER_MV_DEPENDENT || VELOCITY_REJECTION
            float motionVectorLength = length(motionVector);
#else
            float motionVectorLength = 0.0f;
#endif
            GetNeighbourhoodCorners(samples, historyLuma, colorLuma, float2(_AntiFlickerIntensity, _ContrastForMaxAntiFlicker), motionVectorLength);

            history = GetClippedHistory(filteredColor, history, samples.minNeighbour, samples.maxNeighbour);
            filteredColor = SharpenColor(samples, filteredColor, sharpenStrength);
            // ------------------------------------------------------------------------------

            // --------------- Compute blend factor for history ---------------
            float blendFactor = GetBlendFactor(colorLuma, historyLuma, GetLuma(samples.minNeighbour), GetLuma(samples.maxNeighbour));
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
            float lengthMV = motionVectorLength * 10;
            blendFactor = ModifyBlendWithMotionVectorRejection(_InputVelocityMagnitudeHistory, lengthMV, prevUV, blendFactor, _SpeedRejectionIntensity);
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

            _OutputHistoryTexture[COORD_TEXTURE2D_X(input.positionCS.xy)] = color.CTYPE_SWIZZLE;
            outColor = color.CTYPE_SWIZZLE;

#if VELOCITY_REJECTION
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
