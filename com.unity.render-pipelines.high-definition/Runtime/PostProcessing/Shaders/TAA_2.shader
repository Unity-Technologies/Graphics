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
        #pragma enable_d3d11_debug_symbols

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/TemporalAntialiasing2.hlsl"

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

            // --------------- Gather neigbourhood data --------------- 
            float3 color = Fetch(_InputTexture, uv, 0.0, _RTHandleScale.xy);
            color = clamp(color, 0, CLAMP_MAX);
            color = ConvertToWorkingSpace(color);

            NeighbourhoodSamples samples;
            GatherNeighbourhood(uv, input.positionCS.xy, color, samples);
            // --------------------------------------------------------

            // --------------- Filter central sample ---------------
            float3 filteredColor = FilterCentralColor(samples);
            // ------------------------------------------------------


            // --------------- Get closest motion vector --------------- 
    #if defined(ORTHOGRAPHIC)
            float2 closest = input.positionCS.xy;
    #else
            float2 closest = GetClosestFragment(input.positionCS.xy);
    #endif
            float2 motionVector;
            DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, closest), motionVector);
            // --------------------------------------------------------

            // --------------- Get resampled history --------------- 
            float3 history = GetFilteredHistory(input.texcoord - motionVector);
            // -----------------------------------------------------

            // --------------- Get neighbourhood information and clamp history --------------- 
            GetNeighbourhoodCorners(samples);

            float colorLuma = GetLuma(filteredColor);
            float historyLuma = GetLuma(history);

            history = GetClippedHistory(filteredColor, history.xyz, samples.minNeighbour.xyz, samples.maxNeighbour.xyz);

            filteredColor.y = clamp(filteredColor.y, 0, CLAMP_MAX);
            filteredColor = SharpenColor(samples, filteredColor, sharpenStrength);
            // ------------------------------------------------------------------------------

            // --------------- Compute blend factor for history ---------------

            // Feedback weight from unbiased luminance diff (Timothy Lottes)
            float feedback = GetBlendFactor(colorLuma, historyLuma, GetLuma(samples.minNeighbour), GetLuma(samples.maxNeighbour));
            // --------------------------------------------------------

            // --------------- Blend to final value and output ---------------
            float3 finalColor = lerp(history.xyz, filteredColor.xyz, feedback);
            color.xyz = ConvertToOutputSpace(finalColor);

            _OutputHistoryTexture[COORD_TEXTURE2D_X(input.positionCS.xy)] = color;
            outColor = color;

            // -------------------------------------------------------------


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
