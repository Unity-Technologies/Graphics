#ifndef UNITY_DISCRETE_SAMPLING
#define UNITY_DISCRETE_SAMPLING

//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"

float4 ImportanceSingleSampleSkyTexture(float3 normal, int sliceIndex, uint sampleIdx, uint samplesCount)
{
    float2 xi = Hammersley2dSeq(sampleIdx, samplesCount);

    float v = SAMPLE_TEXTURE2D_ARRAY(_SkyTextureMarginalRows, s_trilinear_clamp_sampler, float2(0.0f, xi.x), sliceIndex).x;
    float u = SAMPLE_TEXTURE2D_ARRAY(_SkyTextureMarginalCols, s_trilinear_clamp_sampler, float2(xi.y,  v  ), sliceIndex).x;

    float3 L = normalize(UnpackNormalOctQuadEncode(2*float2(u, v) - 1));

    float  NdotL = max(dot(normal, L), 0.0f);

    if (NdotL > 0.0f)
    {
        return NdotL*SAMPLE_TEXTURECUBE_ARRAY(_SkyTexture, s_trilinear_clamp_sampler, w, sliceIndex);
    }
    else
        return float4(0, 0, 0, 1);
}

float4 ImportanceSampleSkyTexture(float3 normal, int sliceIndex, uint samplesCount)
{
    float4 sum = 0.0f;

    for (uint i = 0; i < samplesCount; ++i)
    {
        sum += ImportanceSingleSampleSkyTexture(normal, sliceIndex, i, samplesCount);
    }

    return sum;
}

#endif // UNITY_DISCRETE_SAMPLING
