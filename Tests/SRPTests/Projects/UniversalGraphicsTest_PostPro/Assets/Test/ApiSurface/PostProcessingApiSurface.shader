// Fixture for PostProcessingApiSurfaceTests: each pass calls one documented arity of a
// PostProcessing/Common.hlsl function. The passes are never rendered, so the values are arbitrary.
Shader "Hidden/Test/PostProcessingApiSurface"
{
    HLSLINCLUDE
        #pragma target 3.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D(_TestLut);
        TEXTURE2D(_TestUserLut);

        static const half3 kTestColor = half3(0.2, 0.4, 0.6);
        static const float3 kTestLutParams = float3(0.03125, 0.96875, 31.0);
        static const float4 kTestAgxParams = float4(1.193, 0.18, 0.5889541, 0.97826);
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "ApplyTonemapNoAgxParams"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(ApplyTonemap(kTestColor), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ApplyTonemapWithAgxParams"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(ApplyTonemap(kTestColor, kTestAgxParams), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ApplyColorGradingNoAgxParams"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = ApplyColorGrading(kTestColor, 1.0,
                    TEXTURE2D_ARGS(_TestLut, sampler_LinearClamp), kTestLutParams,
                    TEXTURE2D_ARGS(_TestUserLut, sampler_LinearClamp), kTestLutParams,
                    0.5, 1.0, 1.0);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ApplyColorGradingWithAgxParams"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = ApplyColorGrading(kTestColor, 1.0,
                    TEXTURE2D_ARGS(_TestLut, sampler_LinearClamp), kTestLutParams,
                    TEXTURE2D_ARGS(_TestUserLut, sampler_LinearClamp), kTestLutParams,
                    0.5, 1.0, 1.0, kTestAgxParams);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
