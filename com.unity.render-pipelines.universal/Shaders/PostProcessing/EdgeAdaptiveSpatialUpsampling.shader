Shader "Hidden/Universal Render Pipeline/Edge Adaptive Spatial Upsampling"
{
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D_X(_SourceTex);
        float4 _SourceSize;

        #define FSR_INPUT_TEXTURE _SourceTex
        #define FSR_INPUT_SAMPLER sampler_LinearClamp

        // Only enable 16-bit instructions when the underlying hardware supports them
        #if HAS_HALF
            #define FSR_ENABLE_16BIT 1
        #endif

        #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/FSRCommon.hlsl"

        half4 FragEASU(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            uint2 integerUv = uv * _ScreenParams.xy;

            half3 color = ApplyEASU(integerUv);

            // Convert back to linear color space before this data is sent into RCAS
            color = Gamma20ToLinear(color);

            return half4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "EASU"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragEASU
                #pragma target 4.5
            ENDHLSL
        }
    }
}
