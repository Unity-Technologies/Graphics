Shader "Hidden/Universal Render Pipeline/TemporalAA"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles

        #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT

        #pragma vertex Vert
        #pragma fragment TaaFrag
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
                // User RGB color space for better perf. on low-end devices.
                #define TAA_YCOCG 0
                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/TemporalAA.hlsl"

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 0, 0, 0, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Accumulate - Quality Low"

            HLSLPROGRAM
                // User RGB color space for better perf.
                #define TAA_YCOCG 0
                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/TemporalAA.hlsl"

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 0, 1, 1, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Accumulate - Quality Medium"

            HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/TemporalAA.hlsl"

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 2, 2, 1, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Accumulate - Quality High"

            HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/TemporalAA.hlsl"

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 2, 2, 2, 0);
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Accumulate - Quality Very High"

            HLSLPROGRAM
                #pragma multi_compile_fragment _ TAA_LOW_PRECISION_SOURCE

                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/TemporalAA.hlsl"

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    #ifdef TAA_LOW_PRECISION_SOURCE
                        // Use clamp instead of clip with low precision color sources to avoid flicker.
                        return DoTemporalAA(input, 2, 2, 2, 1);
                    #else
                        return DoTemporalAA(input, 3, 2, 2, 1);
                    #endif
                }

            ENDHLSL
        }

        Pass
        {
            Name "TemporalAA - Copy History"

            HLSLPROGRAM

                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/TemporalAA.hlsl"

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoCopy(input);
                }

            ENDHLSL
        }
    }
}
