Shader "Hidden/HDRP/GenerateEyeNormals"
{
    HLSLINCLUDE

#pragma target 4.5
#pragma multi_compile_local BILINEAR NEAREST_DEPTH
#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"

        int _MapRes;
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

        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;

            const int iterations = 100;

            uint2 scramblingValue = 0;// ScramblingValue(input.positionCS.x, input.positionCS.y);


            float finalNoise = 0.0f;
            float sum = 0;
            float someParameterControllingNoiseWidth = 1.0f;
            for (int i = 0; i < iterations; ++i)
            {
                float3 randVals;
                randVals.x = GetRaytracingNoiseSample(i, 0, scramblingValue.x);
                randVals.y = GetRaytracingNoiseSample(i, 1, scramblingValue.x);
                randVals.z = GetRaytracingNoiseSample(i, 2, scramblingValue.x);

                float2 currUVs = input.positionCS.xy / _MapRes;

                randVals.xy *= _MapRes * 2 * PI;

                float px = input.positionCS.x - randVals.x;
                px *= px;
                float py = input.positionCS.y - randVals.y;
                py *= py;
                sum += sin(sqrt(px + py) * 1.0f / (2.08f + someParameterControllingNoiseWidth * randVals.z));
            }

            sum /= iterations;

            float heightScale = 1.0f / 16.0f;
            float3 N = normalize(float3(ddx(sum), ddy(sum), heightScale));
            N *= 0.5;
            N += 0.5;
            return  float4(N, 1.0f);

        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off ZTest Off Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
