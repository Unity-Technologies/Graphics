Shader "Hidden/HDRP/GenerateEyeNormals"
{
    HLSLINCLUDE

#pragma target 4.5
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

        TEXTURE2D(_EyeTexture);
        TEXTURE2D(_EyeMask);

        real Luminance(real3 linearRgb)
        {
            return dot(linearRgb, real3(0.2126729, 0.7151522, 0.0721750));
        }

#define VEINS 0 
        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = input.texcoord;

            const int iterations = 100;

            uint2 scramblingValue = 0;// ScramblingValue(input.positionCS.x, input.positionCS.y);


            float finalNoise = 0.0f;
            float sum = 0;
            float someParameterControllingNoiseWidth = 6.0f;
            for (int i = 0; i < iterations; ++i)
            {
                float3 randVals;
                randVals.x = GetRaytracingNoiseSample(i, 0, scramblingValue.x);
                randVals.y = GetRaytracingNoiseSample(i, 1, scramblingValue.x);
                randVals.z = GetRaytracingNoiseSample(i, 2, scramblingValue.x);

                float2 currUVs = input.positionCS.xy / _MapRes;

                randVals.xy *= _MapRes * 2 * PI;

                float px = (float)input.positionCS.x - randVals.x;
                px *= px;
                float py = (float)input.positionCS.y - randVals.y;
                py *= py;
                sum += sin(sqrt(px + py) * 1.0f / (2.08f + someParameterControllingNoiseWidth * randVals.z));
            }

            sum /= iterations;

            float heightScale = 1.0f / 2.0f;
            float3 N = normalize(float3(ddx(sum), ddy(sum), heightScale));

            // TODO! THIS NEEDS TO BE BLURRED.
#if VEINS
            float3 albedo = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy);

            float deltaUV = 1.0f / _MapRes;
            float A = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(-deltaUV, deltaUV)).b;
            float B = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(-deltaUV, 0)).b;
            float C = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(-deltaUV, -deltaUV)).b;
            float D = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(deltaUV, -deltaUV)).b;
            float E = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(deltaUV, 0)).b;
            float F = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(deltaUV, deltaUV)).b;

            float G = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(0, -deltaUV)).b;
            float H = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(0, deltaUV)).b;

            float sobolX = -A - 2 * B - C + D + 2 * E + F;
            float sobolY = -A - 2 * G - F + D +  2 * H + F;

            float S = sqrt(sobolX * sobolX + sobolY * sobolY);


            float3 veinN = normalize(float3(sobolX, sobolY, 0.5 * heightScale));

            float3x3 nBasis = float3x3(
                float3(N.z, N.y, -N.x), // +90 degree rotation around y axis
                float3(N.x, N.z, -N.y), // -90 degree rotation around x axis
                float3(N.x, N.y, N.z));

            float3 r = normalize(veinN.x*nBasis[0] + veinN.y*nBasis[1] + veinN.z*nBasis[2]);
            N = r * 0.5 + 0.5;
#endif

            N *= 0.5;
            N += 0.5;

            return  float4(normalize(N), 1.0f);
        }


        //    float4 Frag2(Varyings input) : SV_Target
        //{
        //    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        //    float2 uv = input.texcoord;

        //    float deltaUV = 1.0f / 512.0f;
        //    float A = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(-deltaUV, deltaUV)).b;
        //    float B = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(-deltaUV, 0)).b;
        //    float C = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(-deltaUV, -deltaUV)).b;
        //    float D = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(deltaUV, -deltaUV)).b;
        //    float E = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(deltaUV, 0)).b;
        //    float F = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(deltaUV, deltaUV)).b;

        //    float G = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(0, -deltaUV)).b;
        //    float H = _EyeTexture.Sample(s_linear_clamp_sampler, input.texcoord.xy + float2(0, deltaUV)).b;

        //    float sobolX = -A - 2 * B - C + D + 2 * E + F;
        //    float sobolY;

        //    float3 veinNormal = float3(ddx(veins), ddy(veins), heightScale);

        //    //veinNormal = normalize(veinNormal);
        //    //veinNormal *= 0.5;
        //    //veinNormal += 0.5;

        //    float vv = veinNormal.x * veinNormal.x + veinNormal.y * veinNormal.y;

        //    return  float4(sobolX, sqrt(vv), veins, 1.0f);
        //}

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
