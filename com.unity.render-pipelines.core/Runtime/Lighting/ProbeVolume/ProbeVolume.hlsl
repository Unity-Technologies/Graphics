#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#ifndef DECODE_SH
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
#endif

#define APV_USE_BASE_OFFSET

// -------------------------------------------------------------
// Structure definitions
// -------------------------------------------------------------

// APV specific code
struct APVConstants
{
    float3x4    WStoRS;
    float       normalBias; // amount of biasing along the normal
    int3        centerRS;   // index center location in refspace
    int3        centerIS;   // index center location in index space
    uint3       indexDim;   // resolution of the index
    uint3       poolDim;    // resolution of the brick pool
};

static const int kAPVConstantsSize = 12 + 1 + 3 + 3 + 3 + 3;

struct APVResources
{
    StructuredBuffer<int> index;

    Texture3D L0_L1Rx;

    Texture3D L1G_L1Ry;
    Texture3D L1B_L1Rz;

#ifdef PROBE_VOLUMES_L2
    Texture3D L2_0;
    Texture3D L2_1;
    Texture3D L2_2;
    Texture3D L2_3;
#endif
};

struct APVSample
{
    float3 L0;
    float3 L1_R;
    float3 L1_G;
    float3 L1_B;
#ifdef PROBE_VOLUMES_L2
    float4 L2_R;
    float4 L2_G;
    float4 L2_B;
    float3 L2_C;
#endif

#define APV_SAMPLE_STATUS_INVALID -1
#define APV_SAMPLE_STATUS_ENCODED 0
#define APV_SAMPLE_STATUS_DECODED 1

    int status;

    // Note: at the moment this is called at the moment the struct is built, but it is kept as a separate step
    // as ideally should be called as far as possible from sample to allow for latency hiding.
    void Decode()
    {
        if (status == APV_SAMPLE_STATUS_ENCODED)
        {
            L1_R = DecodeSH(L0.r, L1_R);
            L1_G = DecodeSH(L0.g, L1_G);
            L1_B = DecodeSH(L0.b, L1_B);
#ifdef PROBE_VOLUMES_L2
            float4 outL2_C = float4(L2_C, 0.0f);
            DecodeSH_L2(L0, L2_R, L2_G, L2_B, outL2_C);
            L2_C = outL2_C.xyz;
#endif

            status = APV_SAMPLE_STATUS_DECODED;
        }
    }
};

// Resources required for APV
StructuredBuffer<int> _APVResIndex;

TEXTURE3D(_APVResL0_L1Rx);

TEXTURE3D(_APVResL1G_L1Ry);
TEXTURE3D(_APVResL1B_L1Rz);

#ifdef PROBE_VOLUMES_L2
TEXTURE3D(_APVResL2_0);
TEXTURE3D(_APVResL2_1);
TEXTURE3D(_APVResL2_2);
TEXTURE3D(_APVResL2_3);
#endif

// -------------------------------------------------------------
// Loading functions
// -------------------------------------------------------------

APVResources FillAPVResources()
{
    APVResources apvRes;
    apvRes.index = _APVResIndex;

    apvRes.L0_L1Rx = _APVResL0_L1Rx;

    apvRes.L1G_L1Ry = _APVResL1G_L1Ry;
    apvRes.L1B_L1Rz = _APVResL1B_L1Rz;

#if PROBE_VOLUMES_L2
    apvRes.L2_0 = _APVResL2_0;
    apvRes.L2_1 = _APVResL2_1;
    apvRes.L2_2 = _APVResL2_2;
    apvRes.L2_3 = _APVResL2_3;
#endif

    return apvRes;
}

APVConstants LoadAPVConstants(StructuredBuffer<int> index)
{
    APVConstants apvc;
    apvc.WStoRS[0][0] = asfloat(index[0]);
    apvc.WStoRS[1][0] = asfloat(index[1]);
    apvc.WStoRS[2][0] = asfloat(index[2]);
    apvc.WStoRS[0][1] = asfloat(index[3]);
    apvc.WStoRS[1][1] = asfloat(index[4]);
    apvc.WStoRS[2][1] = asfloat(index[5]);
    apvc.WStoRS[0][2] = asfloat(index[6]);
    apvc.WStoRS[1][2] = asfloat(index[7]);
    apvc.WStoRS[2][2] = asfloat(index[8]);
    apvc.WStoRS[0][3] = asfloat(index[9]);
    apvc.WStoRS[1][3] = asfloat(index[10]);
    apvc.WStoRS[2][3] = asfloat(index[11]);
    apvc.normalBias = asfloat(index[12]);
    apvc.centerRS.x = index[13];
    apvc.centerRS.y = index[14];
    apvc.centerRS.z = index[15];
    apvc.centerIS.x = index[16];
    apvc.centerIS.y = index[17];
    apvc.centerIS.z = index[18];
    apvc.indexDim.x = index[19];
    apvc.indexDim.y = index[20];
    apvc.indexDim.z = index[21];
    apvc.poolDim.x = index[22];
    apvc.poolDim.y = index[23];
    apvc.poolDim.z = index[24];
    return apvc;
}

bool TryToGetPoolUVW(APVResources apvRes, float3 posWS, float3 normalWS, out float3 uvw)
{
    uvw = 0;
    // Note: we could instead early return when we know we'll have invalid UVs, but some bade code gen on Vulkan generates shader warnings if we do.
    bool hasValidUVW = true;

    APVConstants apvConst = LoadAPVConstants(apvRes.index);
    // transform into APV space
    float3 posRS = mul(apvConst.WStoRS, float4(posWS + normalWS * apvConst.normalBias, 1.0));
    posRS -= apvConst.centerRS;

    // check bounds
#ifdef APV_USE_BASE_OFFSET
    if (any(abs(posRS.xz) > float2(apvConst.indexDim.xz / 2)))
#else
    if (any(abs(posRS) > float3(apvConst.indexDim / 2)))
#endif
    {
        hasValidUVW = false;
    }

    // convert to index
    int3 index = apvConst.centerIS + floor(posRS);
    index = index % apvConst.indexDim;

#ifdef APV_USE_BASE_OFFSET
    // get the y-offset
    int  yoffset = apvRes.index[kAPVConstantsSize + index.z * apvConst.indexDim.x + index.x];
    if (yoffset == -1 || posRS.y < yoffset || posRS.y >= float(apvConst.indexDim.y))
    {
        hasValidUVW = false;
    }

    index.y = posRS.y - yoffset;
#endif

    // resolve the index
    int  base_offset = kAPVConstantsSize + apvConst.indexDim.x * apvConst.indexDim.z;
    int  flattened_index = index.z * (apvConst.indexDim.x * apvConst.indexDim.y) + index.x * apvConst.indexDim.y + index.y;
    uint packed_pool_idx = apvRes.index[base_offset + flattened_index];

    // no valid brick loaded for this index, fallback to ambient probe
    if (packed_pool_idx == 0xffffffff)
    {
        hasValidUVW = false;
    }

    // unpack pool idx
    // size is encoded in the upper 4 bits
    uint   subdiv = (packed_pool_idx >> 28) & 15;
    float  cellSize = pow(3.0, subdiv);
    uint   flattened_pool_idx = packed_pool_idx & ((1 << 28) - 1);
    uint3  pool_idx;
    pool_idx.z = flattened_pool_idx / (apvConst.poolDim.x * apvConst.poolDim.y);
    flattened_pool_idx -= pool_idx.z * (apvConst.poolDim.x * apvConst.poolDim.y);
    pool_idx.y = flattened_pool_idx / apvConst.poolDim.x;
    pool_idx.x = flattened_pool_idx - (pool_idx.y * apvConst.poolDim.x);
    uvw = ((float3) pool_idx + 0.5) / (float3) apvConst.poolDim;

    // calculate uv offset and scale
    float3 offset = frac(posRS / (float)cellSize);  // [0;1] in brick space
    //offset    = clamp( offset, 0.25, 0.75 );      // [0.25;0.75] in brick space (is this actually necessary?)
    offset *= 3.0 / (float3) apvConst.poolDim;      // convert brick footprint to texels footprint in pool texel space
    uvw += offset;                                  // add the final offset

    return hasValidUVW;
}

APVSample SampleAPV(APVResources apvRes, float3 uvw)
{
    APVSample apvSample;
    float4 L0_L1Rx = SAMPLE_TEXTURE3D_LOD(apvRes.L0_L1Rx, s_linear_clamp_sampler, uvw, 0).rgba;
    float4 L1G_L1Ry = SAMPLE_TEXTURE3D_LOD(apvRes.L1G_L1Ry, s_linear_clamp_sampler, uvw, 0).rgba;
    float4 L1B_L1Rz = SAMPLE_TEXTURE3D_LOD(apvRes.L1B_L1Rz, s_linear_clamp_sampler, uvw, 0).rgba;

    apvSample.L0 = L0_L1Rx.xyz;
    apvSample.L1_R = float3(L0_L1Rx.w, L1G_L1Ry.w, L1B_L1Rz.w);
    apvSample.L1_G = L1G_L1Ry.xyz;
    apvSample.L1_B = L1B_L1Rz.xyz;

#ifdef PROBE_VOLUMES_L2
    apvSample.L2_R = SAMPLE_TEXTURE3D_LOD(apvRes.L2_0, s_linear_clamp_sampler, uvw, 0).rgba;
    apvSample.L2_G = SAMPLE_TEXTURE3D_LOD(apvRes.L2_1, s_linear_clamp_sampler, uvw, 0).rgba;
    apvSample.L2_B = SAMPLE_TEXTURE3D_LOD(apvRes.L2_2, s_linear_clamp_sampler, uvw, 0).rgba;
    apvSample.L2_C = SAMPLE_TEXTURE3D_LOD(apvRes.L2_3, s_linear_clamp_sampler, uvw, 0).rgb;
#endif

    apvSample.status = APV_SAMPLE_STATUS_ENCODED;

    return apvSample;
}

APVSample SampleAPV(APVResources apvRes, float3 posWS, float3 biasNormalWS)
{
    APVSample outSample;

    float3 pool_uvw;
    if (TryToGetPoolUVW(apvRes, posWS, biasNormalWS, pool_uvw))
    {
        outSample = SampleAPV(apvRes, pool_uvw);
    }
    else
    {
        ZERO_INITIALIZE(APVSample, outSample);
        outSample.status = APV_SAMPLE_STATUS_INVALID;
    }

    return outSample;
}

APVSample SampleAPV(float3 posWS, float3 biasNormalWS)
{
    APVResources apvRes = FillAPVResources();
    return SampleAPV(apvRes, posWS, biasNormalWS);
}

// -------------------------------------------------------------
// Internal Evaluation functions (avoid usage in caller code outside this file)
// -------------------------------------------------------------

float3 EvaluateAPVL0(APVSample apvSample)
{
    return apvSample.L0;
}

void EvaluateAPVL1(APVSample apvSample, float3 N, out float3 diffuseLighting)
{
    diffuseLighting = SHEvalLinearL1(N, apvSample.L1_R, apvSample.L1_G, apvSample.L1_B);
}

#ifdef PROBE_VOLUMES_L2
void EvaluateAPVL1L2(APVSample apvSample, float3 N, out float3 diffuseLighting)
{
    EvaluateAPVL1(apvSample, N, diffuseLighting);
    diffuseLighting += SHEvalLinearL2(N, apvSample.L2_R, apvSample.L2_G, apvSample.L2_B, float4(apvSample.L2_C, 0.0f));
}
#endif



// -------------------------------------------------------------
// "Public" Evaluation functions, the one that callers outside this file should use
// -------------------------------------------------------------

void EvaluateAdaptiveProbeVolume(APVSample apvSample, float3 normalWS, float3 backNormalWS, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
    {
        apvSample.Decode();

#ifdef PROBE_VOLUMES_L1
        EvaluateAPVL1(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1(apvSample, backNormalWS, backBakeDiffuseLighting);
#elif PROBE_VOLUMES_L2
        EvaluateAPVL1L2(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1L2(apvSample, backNormalWS, backBakeDiffuseLighting);
#endif

        bakeDiffuseLighting += apvSample.L0;
        backBakeDiffuseLighting += apvSample.L0;
    }
    else
    {
        // no valid brick, fallback to ambient probe
        bakeDiffuseLighting = EvaluateAmbientProbe(normalWS);
        backBakeDiffuseLighting = EvaluateAmbientProbe(backNormalWS);
    }
}

// Note: This is probably an internal only usage.
void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in APVResources apvRes,
    out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    APVSample apvSample = SampleAPV(apvRes, posWS, normalWS);
    EvaluateAdaptiveProbeVolume(apvSample, normalWS, backNormalWS, bakeDiffuseLighting, backBakeDiffuseLighting);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS,
    out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    APVResources apvRes = FillAPVResources();

    EvaluateAdaptiveProbeVolume(posWS, normalWS, backNormalWS, apvRes,
        bakeDiffuseLighting, backBakeDiffuseLighting);
}


void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 reflDir,
    out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting, out float3 lightingInReflDir)
{
    APVResources apvRes = FillAPVResources();

    APVSample apvSample = SampleAPV(apvRes, posWS, normalWS);
    if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
    {
        apvSample.Decode();

#ifdef PROBE_VOLUMES_L1
        EvaluateAPVL1(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1(apvSample, backNormalWS, backBakeDiffuseLighting);
        EvaluateAPVL1(apvSample, reflDir, lightingInReflDir);
#elif PROBE_VOLUMES_L2
        EvaluateAPVL1L2(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1L2(apvSample, backNormalWS, backBakeDiffuseLighting);
        EvaluateAPVL1L2(apvSample, reflDir, lightingInReflDir);
#endif

        bakeDiffuseLighting += apvSample.L0;
        backBakeDiffuseLighting += apvSample.L0;
        lightingInReflDir += apvSample.L0;
    }
    else
    {
        bakeDiffuseLighting = EvaluateAmbientProbe(normalWS);
        backBakeDiffuseLighting = EvaluateAmbientProbe(backNormalWS);
        lightingInReflDir = -1;
    }
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, out float3 bakeDiffuseLighting)
{
    APVResources apvRes = FillAPVResources();

    float3 uvw;
    if (TryToGetPoolUVW(apvRes, posWS, 0, uvw))
    {
        bakeDiffuseLighting = SAMPLE_TEXTURE3D_LOD(apvRes.L0_L1Rx, s_linear_clamp_sampler, uvw, 0).rgb;
    }
    else
    {
        bakeDiffuseLighting = EvaluateAmbientProbe(0);
    }
}


// -------------------------------------------------------------
// Reflection Probe Normalization functions
// -------------------------------------------------------------


// Same idea as in Rendering of COD:IW [Drobot 2017]
float GetReflProbeNormalizationFactor(float3 sampleDir, float4 reflProbeSHL0L1, float4 reflProbeSHL2_1, float reflProbeSHL2_2)
{
    float outFactor = 0;
    float L0 = reflProbeSHL0L1.x;
    float L1 = dot(reflProbeSHL0L1.yzw, sampleDir);

    outFactor = L0 + L1;

#ifdef PROBE_VOLUMES_L2
    float4 vB = sampleDir.xyzz * sampleDir.yzzx;
    // First 4 coeff.
    float L2 = dot(reflProbeSHL2_1, vB);
    float vC = sampleDir.x * sampleDir.x - sampleDir.y * sampleDir.y;
    L2 += reflProbeSHL2_2 * vC;

    outFactor += L2;
#endif

    return outFactor;
}

float GetReflectionProbeNormalizationFactor(float3 lightingInReflDir, float3 sampleDir, float4 reflProbeSHL0L1, float4 reflProbeSHL2_1, float reflProbeSHL2_2)
{
    float refProbeNormalization = GetReflProbeNormalizationFactor(sampleDir, reflProbeSHL0L1, reflProbeSHL2_1, reflProbeSHL2_2);
    float localNormalization = Luminance(lightingInReflDir);

    return localNormalization / refProbeNormalization;
}
#endif // __PROBEVOLUME_HLSL__
