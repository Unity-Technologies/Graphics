Shader "Hidden/Universal Render Pipeline/TemporalAA"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

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

                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/TemporalAA.hlsl"

                half4 TaaFrag(Varyings input) : SV_Target
                {
                    return DoTemporalAA(input, 3, 2, 2, 1);
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
