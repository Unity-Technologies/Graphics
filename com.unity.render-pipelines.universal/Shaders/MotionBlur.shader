Shader "Hidden/kMotion/MotionBlur"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
    }

    HLSLINCLUDE

    // -------------------------------------
    // Includes
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

    // -------------------------------------
    // Inputs
    TEXTURE2D_X(_MainTex);
    TEXTURE2D(_MotionVectorTexture);       SAMPLER(sampler_MotionVectorTexture);

    float _Intensity;
    float4 _MainTex_TexelSize;

    // -------------------------------------
    // Structs
    struct VaryingsMB
    {
        float4 positionCS    : SV_POSITION;
        float4 uv            : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };
    
    struct Attributes
    {
        float4 positionOS   : POSITION;
        float2 uv           : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    
    // -------------------------------------
    // Vertex
    VaryingsMB VertMB(Attributes input)
    {
        VaryingsMB output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

        float4 projPos = output.positionCS * 0.5;
        projPos.xy = projPos.xy + projPos.w;

        output.uv.xy = input.uv;
        output.uv.zw = projPos.xy;

        return output;
    }

    // -------------------------------------
    // Fragment
    float3 GatherSample(float sampleNumber, float2 velocity, float invSampleCount, float2 centerUV, float randomVal, float velocitySign)
    {
        float  offsetLength = (sampleNumber + 0.5) + (velocitySign * (randomVal - 0.5));
        float2 sampleUV = centerUV + (offsetLength * invSampleCount) * velocity * velocitySign;
        return SAMPLE_TEXTURE2D_X(_MainTex, sampler_PointClamp, sampleUV).xyz;
    }

    half4 DoMotionBlur(VaryingsMB input, int iterations)
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 uv = UnityStereoTransformScreenSpaceTex(input.uv.xy);
        float2 velocity = SAMPLE_TEXTURE2D(_MotionVectorTexture, sampler_MotionVectorTexture, uv).rg * _Intensity;
        float randomVal = InterleavedGradientNoise(uv * _MainTex_TexelSize.zw, 0);
        float invSampleCount = rcp(iterations * 2.0);

        half3 color = 0.0;

        UNITY_UNROLL
        for (int i = 0; i < iterations; i++)
        {
            color += GatherSample(i, velocity, invSampleCount, uv, randomVal, -1.0);
            color += GatherSample(i, velocity, invSampleCount, uv, randomVal,  1.0);
        }

        return half4(color * invSampleCount, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Camera Motion Blur - Low Quality"

            HLSLPROGRAM

            #pragma vertex VertMB
            #pragma fragment Frag

            half4 Frag(VaryingsMB input) : SV_Target
            {
                return DoMotionBlur(input, 2);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Camera Motion Blur - Medium Quality"

            HLSLPROGRAM

            #pragma vertex VertMB
            #pragma fragment Frag

            half4 Frag(VaryingsMB input) : SV_Target
            {
                return DoMotionBlur(input, 3);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Camera Motion Blur - High Quality"

            HLSLPROGRAM

            #pragma vertex VertMB
            #pragma fragment Frag

            half4 Frag(VaryingsMB input) : SV_Target
            {
                return DoMotionBlur(input, 4);
            }

            ENDHLSL
        }
    }
}
