#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ShaderVariablesProbeVolumes.cs.hlsl"

#ifndef DECODE_SH
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
#endif

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_point_clamp_sampler);
#endif

struct APVResources
{
    StructuredBuffer<int> index;

    Texture3D L0_L1Rx;

    Texture3D L1G_L1Ry;
    Texture3D L1B_L1Rz;
    Texture3D L2_0;
    Texture3D L2_1;
    Texture3D L2_2;
    Texture3D L2_3;
};

// Resources required for APV
StructuredBuffer<int> _APVResIndex;

TEXTURE3D(_APVResL0_L1Rx);

TEXTURE3D(_APVResL1G_L1Ry);
TEXTURE3D(_APVResL1B_L1Rz);

TEXTURE3D(_APVResL2_0);
TEXTURE3D(_APVResL2_1);
TEXTURE3D(_APVResL2_2);
TEXTURE3D(_APVResL2_3);

// We split the evaluation in several steps to make variants with different bands easier.
float3 EvaluateAPVL0(APVResources apvRes, float3 uvw, out float L1Rx)
{
    float4 L0_L1Rx = SAMPLE_TEXTURE3D_LOD(apvRes.L0_L1Rx, s_linear_clamp_sampler, uvw, 0).rgba;
    L1Rx = L0_L1Rx.w;

    return L0_L1Rx.xyz;
}

void EvaluateAPVL1(APVResources apvRes, float3 L0, float L1Rx, float3 N, float3 backN, float3 uvw, out float3 diffuseLighting, out float3 backDiffuseLighting)
{
    float4 L1G_L1Ry = SAMPLE_TEXTURE3D_LOD(apvRes.L1G_L1Ry, s_linear_clamp_sampler, uvw, 0).rgba;
    float4 L1B_L1Rz = SAMPLE_TEXTURE3D_LOD(apvRes.L1B_L1Rz, s_linear_clamp_sampler, uvw, 0).rgba;

    float3 l1_R = float3(L1Rx, L1G_L1Ry.w, L1B_L1Rz.w);
    float3 l1_G = L1G_L1Ry.xyz;
    float3 l1_B = L1B_L1Rz.xyz;

    // decode the L1 coefficients
    l1_R = DecodeSH(L0.r, l1_R);
    l1_G = DecodeSH(L0.g, l1_G);
    l1_B = DecodeSH(L0.b, l1_B);

    diffuseLighting = SHEvalLinearL1(N, l1_R, l1_G, l1_B);
    backDiffuseLighting = SHEvalLinearL1(backN, l1_R, l1_G, l1_B);
}

// Temporary duplication
float3 EvaluateAPVL0Point(APVResources apvRes, float3 uvw, out float L1Rx)
{
    float4 L0_L1Rx = SAMPLE_TEXTURE3D_LOD(apvRes.L0_L1Rx, s_point_clamp_sampler, uvw, 0).rgba;
    L1Rx = L0_L1Rx.w;

    return L0_L1Rx.xyz;
}

void EvaluateAPVL1Point(APVResources apvRes, float3 L0, float L1Rx, float3 N, float3 backN, float3 uvw, out float3 diffuseLighting, out float3 backDiffuseLighting)
{
    float4 L1G_L1Ry = SAMPLE_TEXTURE3D_LOD(apvRes.L1G_L1Ry, s_point_clamp_sampler, uvw, 0).rgba;
    float4 L1B_L1Rz = SAMPLE_TEXTURE3D_LOD(apvRes.L1B_L1Rz, s_point_clamp_sampler, uvw, 0).rgba;

    float3 l1_R = float3(L1Rx, L1G_L1Ry.w, L1B_L1Rz.w);
    float3 l1_G = L1G_L1Ry.xyz;
    float3 l1_B = L1B_L1Rz.xyz;

    // decode the L1 coefficients
    l1_R = DecodeSH(L0.r, l1_R);
    l1_G = DecodeSH(L0.g, l1_G);
    l1_B = DecodeSH(L0.b, l1_B);

    diffuseLighting = SHEvalLinearL1(N, l1_R, l1_G, l1_B);
    backDiffuseLighting = SHEvalLinearL1(backN, l1_R, l1_G, l1_B);
}

#ifdef PROBE_VOLUMES_L2
void EvaluateAPVL1L2(APVResources apvRes, float3 L0, float L1Rx, float3 N, float3 backN, float3 uvw, out float3 diffuseLighting, out float3 backDiffuseLighting)
{
    EvaluateAPVL1(apvRes, L0, L1Rx, N, backN, uvw, diffuseLighting, backDiffuseLighting);

    float4 l2_R = SAMPLE_TEXTURE3D_LOD(apvRes.L2_0, s_linear_clamp_sampler, uvw, 0).rgba;
    float4 l2_G = SAMPLE_TEXTURE3D_LOD(apvRes.L2_1, s_linear_clamp_sampler, uvw, 0).rgba;
    float4 l2_B = SAMPLE_TEXTURE3D_LOD(apvRes.L2_2, s_linear_clamp_sampler, uvw, 0).rgba;
    float4 l2_C = SAMPLE_TEXTURE3D_LOD(apvRes.L2_3, s_linear_clamp_sampler, uvw, 0).rgba;

    DecodeSH_L2(L0, l2_R, l2_G, l2_B, l2_C);

    diffuseLighting += SHEvalLinearL2(N, l2_R, l2_G, l2_B, l2_C);
    backDiffuseLighting += SHEvalLinearL2(backN, l2_R, l2_G, l2_B, l2_C);
}

void EvaluateAPVL1L2Point(APVResources apvRes, float3 L0, float L1Rx, float3 N, float3 backN, float3 uvw, out float3 diffuseLighting, out float3 backDiffuseLighting)
{
    EvaluateAPVL1Point(apvRes, L0, L1Rx, N, backN, uvw, diffuseLighting, backDiffuseLighting);

    float4 l2_R = SAMPLE_TEXTURE3D_LOD(apvRes.L2_0, s_point_clamp_sampler, uvw, 0).rgba;
    float4 l2_G = SAMPLE_TEXTURE3D_LOD(apvRes.L2_1, s_point_clamp_sampler, uvw, 0).rgba;
    float4 l2_B = SAMPLE_TEXTURE3D_LOD(apvRes.L2_2, s_point_clamp_sampler, uvw, 0).rgba;
    float4 l2_C = SAMPLE_TEXTURE3D_LOD(apvRes.L2_3, s_point_clamp_sampler, uvw, 0).rgba;

    DecodeSH_L2(L0, l2_R, l2_G, l2_B, l2_C);

    diffuseLighting += SHEvalLinearL2(N, l2_R, l2_G, l2_B, l2_C);
    backDiffuseLighting += SHEvalLinearL2(backN, l2_R, l2_G, l2_B, l2_C);
}
#endif

bool TryToGetPoolUVWAndSubdiv(APVResources apvRes, float3 posWS, float3 normalWS, float3 viewDirWS, out float3 uvw, out uint subdiv)
{
    uvw = 0;
    // Note: we could instead early return when we know we'll have invalid UVs, but some bade code gen on Vulkan generates shader warnings if we do.
    bool hasValidUVW = true;

    // transform into APV space
    float3 posRS = mul(_WStoRS, float4(posWS + normalWS * _NormalBias
                                             + viewDirWS * _ViewBias, 1.0)).xyz;

    uint3 indexDim = (uint3)_IndexDim;
    uint3 poolDim = (uint3)_PoolDim;
    int3 centerIS = indexDim / 2;

    // check bounds
    if (any(abs(posRS) > float3(centerIS)))
    {
        hasValidUVW = false;
    }

    // convert to index
    int3 index = centerIS + floor(posRS);
    index = index % indexDim;

    // resolve the index
    int  flattened_index = index.z * (indexDim.x * indexDim.y) + index.x * indexDim.y + index.y;
    uint packed_pool_idx = apvRes.index[flattened_index];

    // no valid brick loaded for this index, fallback to ambient probe
    if (packed_pool_idx == 0xffffffff)
    {
        hasValidUVW = false;
    }

    // unpack pool idx
    // size is encoded in the upper 4 bits
    subdiv = (packed_pool_idx >> 28) & 15;
    float  cellSize = pow(3.0, subdiv);
    uint   flattened_pool_idx = packed_pool_idx & ((1 << 28) - 1);
    uint3  pool_idx;
    pool_idx.z = flattened_pool_idx / (poolDim.x * poolDim.y);
    flattened_pool_idx -= pool_idx.z * (poolDim.x * poolDim.y);
    pool_idx.y = flattened_pool_idx / poolDim.x;
    pool_idx.x = flattened_pool_idx - (pool_idx.y * poolDim.x);
    uvw = ((float3) pool_idx + 0.5) / _PoolDim;

    // calculate uv offset and scale
    float3 offset = frac(posRS / (float)cellSize);  // [0;1] in brick space
    //offset    = clamp( offset, 0.25, 0.75 );      // [0.25;0.75] in brick space (is this actually necessary?)
    offset *= 3.0 / _PoolDim;                       // convert brick footprint to texels footprint in pool texel space
    uvw += offset;                                  // add the final offset

    return hasValidUVW;
}

bool TryToGetPoolUVW(APVResources apvRes, float3 posWS, float3 normalWS, float3 viewDir, out float3 uvw)
{
    uint unusedSubdiv;
    return TryToGetPoolUVWAndSubdiv(apvRes, posWS, normalWS, viewDir, uvw, unusedSubdiv);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 viewDir, in APVResources apvRes,
    out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    float3 pool_uvw;
    if (TryToGetPoolUVW(apvRes, posWS, normalWS, viewDir, pool_uvw))
    {
        float L1Rx;
        float3 L0 = EvaluateAPVL0(apvRes, pool_uvw, L1Rx);

#ifdef PROBE_VOLUMES_L1
        EvaluateAPVL1(apvRes, L0, L1Rx, normalWS, backNormalWS, pool_uvw, bakeDiffuseLighting, backBakeDiffuseLighting);
#elif PROBE_VOLUMES_L2
        EvaluateAPVL1L2(apvRes, L0, L1Rx, normalWS, backNormalWS, pool_uvw, bakeDiffuseLighting, backBakeDiffuseLighting);
#endif

        bakeDiffuseLighting += L0;
        backBakeDiffuseLighting += L0;
    }
    else
    {
        // no valid brick, fallback to ambient probe
        bakeDiffuseLighting = EvaluateAmbientProbe(normalWS);
        backBakeDiffuseLighting = EvaluateAmbientProbe(backNormalWS);
    }
}

float3 EvaluateAdaptiveProbeVolumeL0(in float3 posWS, in float3 normalWS, in float3 viewDir, in APVResources apvRes)
{
    float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    float3 pool_uvw;
    if (TryToGetPoolUVW(apvRes, posWS, normalWS, viewDir, pool_uvw))
    {
        float unused;
        float3 L0 = EvaluateAPVL0(apvRes, pool_uvw, unused);
        bakeDiffuseLighting = L0;
    }
    else
    {
        // no valid brick, fallback to ambient probe
        bakeDiffuseLighting = EvaluateAmbientProbe(normalWS);
    }

    return bakeDiffuseLighting;
}

APVResources FillAPVResources()
{
    APVResources apvRes;
    apvRes.index = _APVResIndex;

    apvRes.L0_L1Rx = _APVResL0_L1Rx;

    apvRes.L1G_L1Ry = _APVResL1G_L1Ry;
    apvRes.L1B_L1Rz = _APVResL1B_L1Rz;

    apvRes.L2_0 = _APVResL2_0;
    apvRes.L2_1 = _APVResL2_1;
    apvRes.L2_2 = _APVResL2_2;
    apvRes.L2_3 = _APVResL2_3;

    return apvRes;
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 viewDir, in float2 positionSS,
    out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    APVResources apvRes = FillAPVResources();

    // Bit of an hack to apply noise at sampling location to hide seams. Ideally we should run this only when there is a seam, but detecting that would be costly.
    if (_PVSamplingNoise > 0)
    {
        float3x3 orthoBasis = GetLocalFrame(normalWS);

        float noise1D_0 = (InterleavedGradientNoise(positionSS, 0) * 2.0f - 1.0f) * _PVSamplingNoise;
        float noise1D_1 = (InterleavedGradientNoise(positionSS, 1) * 2.0f - 1.0f) * _PVSamplingNoise;
        posWS += orthoBasis[0] * noise1D_1 + noise1D_0 * orthoBasis[1];
    }

    EvaluateAdaptiveProbeVolume(posWS, normalWS, backNormalWS, viewDir, apvRes,
        bakeDiffuseLighting, backBakeDiffuseLighting);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float2 positionSS, out float3 bakeDiffuseLighting)
{
    if (_PVSamplingNoise > 0)
    {
        float noise1D_0 = (InterleavedGradientNoise(positionSS, 0) * 2.0f - 1.0f) * _PVSamplingNoise;
        posWS += noise1D_0;
    }

    APVResources apvRes = FillAPVResources();
    bakeDiffuseLighting = EvaluateAdaptiveProbeVolumeL0(posWS, float3(0.0f, 0.0f, 0.0f), float3(0.0f, 0.0f, 0.0f), apvRes);
}

#endif // __PROBEVOLUME_HLSL__
