#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ShaderVariablesProbeVolumes.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Unpack variables
#define _PoolDim _PoolDim_CellInMeters.xyz
#define _CellInMeters _PoolDim_CellInMeters.w
#define _MinCellPosition _MinCellPos_Noise.xyz
#define _PVSamplingNoise _MinCellPos_Noise.w
#define _CellIndicesDim _IndicesDim_IndexChunkSize.xyz
#define _IndexChunkSize _IndicesDim_IndexChunkSize.w
#define _NormalBias _Biases_CellInMinBrick_MinBrickSize.x
#define _ViewBias _Biases_CellInMinBrick_MinBrickSize.y
#define _MinBrickSize _Biases_CellInMinBrick_MinBrickSize.w
#define _Weight _Weight_MinLoadedCell.x
#define _MinLoadedCell _Weight_MinLoadedCell.yzw
#define _MaxLoadedCell _MaxLoadedCell_FrameIndex.xyz
#define _NoiseFrameIndex _MaxLoadedCell_FrameIndex.w
#define _MinReflProbeNormalizationFactor _NormalizationClamp_Padding12.x
#define _MaxReflProbeNormalizationFactor _NormalizationClamp_Padding12.y

#ifndef DECODE_SH
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
#endif

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_point_clamp_sampler);
#endif

// TODO: Remove define when we are sure about what to do with this.
#define MANUAL_FILTERING 0


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

    Texture3D Validity;
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

    void Encode()
    {
        if (status == APV_SAMPLE_STATUS_DECODED)
        {
            L1_R = EncodeSH(L0.r, L1_R);
            L1_G = EncodeSH(L0.g, L1_G);
            L1_B = EncodeSH(L0.b, L1_B);
#ifdef PROBE_VOLUMES_L2
            EncodeSH_L2(L0, L2_R, L2_G, L2_B, L2_C);
#endif

            status = APV_SAMPLE_STATUS_ENCODED;
        }
    }
};

// Resources required for APV
StructuredBuffer<int> _APVResIndex;
StructuredBuffer<uint3> _APVResCellIndices;

TEXTURE3D(_APVResL0_L1Rx);

TEXTURE3D(_APVResL1G_L1Ry);
TEXTURE3D(_APVResL1B_L1Rz);

TEXTURE3D(_APVResL2_0);
TEXTURE3D(_APVResL2_1);
TEXTURE3D(_APVResL2_2);
TEXTURE3D(_APVResL2_3);
TEXTURE3D(_APVResValidity);

// -------------------------------------------------------------
// Various weighting functions for occlusion or helper functions.
// -------------------------------------------------------------
float3 AddNoiseToSamplingPosition(float3 posWS, float2 positionSS)
{
    float3 outPos = posWS;
    if (_PVSamplingNoise > 0)
    {
        float noise1D_0 = (InterleavedGradientNoise(positionSS, _NoiseFrameIndex) * 2.0f - 1.0f) * _PVSamplingNoise;
        outPos += noise1D_0;
    }
    return outPos;
}

uint3 GetSampleOffset(uint i)
{
    return uint3(i, i >> 1, i >> 2) & 1;
}

// The validity mask is sampled once and contains a binary info on whether a probe neighbour (relevant for trilinear) is to be used
// or not. The entry in the mask uses the same mapping that GetSampleOffset above uses.
float GetValidityWeight(int offset, uint validityMask)
{
    int mask = 1 << offset;
    return (validityMask & mask) > 0 ? 1 : 0;
}

float ProbeDistance(uint subdiv)
{
    return pow(3, subdiv) * _MinBrickSize / 3.0f;
}

float3 GetSnappedProbePosition(float3 posWS, uint subdiv)
{
    float distBetweenProbes = ProbeDistance(subdiv);
    float3 dividedPos = posWS / distBetweenProbes;
    return (dividedPos  - frac(dividedPos)) * distBetweenProbes;
}

float GetNormalWeight(int3 offset, float3 posWS, float3 sample0Pos, float3 normalWS, int subdiv)
{
    // TODO: This can be optimized.
    float3 samplePos = sample0Pos + offset * ProbeDistance(subdiv);
    float3 vecToProbe = normalize((samplePos)-posWS);
    float weight = saturate(dot(vecToProbe, normalWS) - _LeakReductionParams.z);
    return weight;

}


// -------------------------------------------------------------
// Indexing functions
// -------------------------------------------------------------

bool LoadCellIndexMetaData(int cellFlatIdx, out int chunkIndex, out int stepSize, out int3 minRelativeIdx, out int3 maxRelativeIdx)
{
    bool cellIsLoaded = false;
    uint3 metaData = _APVResCellIndices[cellFlatIdx];

    if (metaData.x != 0xFFFFFFFF)
    {
        chunkIndex = metaData.x & 0x1FFFFFFF;
        stepSize = pow(3, (metaData.x >> 29) & 0x7);

        minRelativeIdx.x = metaData.y & 0x3FF;
        minRelativeIdx.y = (metaData.y >> 10) & 0x3FF;
        minRelativeIdx.z = (metaData.y >> 20) & 0x3FF;

        maxRelativeIdx.x = metaData.z & 0x3FF;
        maxRelativeIdx.y = (metaData.z >> 10) & 0x3FF;
        maxRelativeIdx.z = (metaData.z >> 20) & 0x3FF;
        cellIsLoaded = true;
    }
    else
    {
        chunkIndex = -1;
        stepSize = -1;
        minRelativeIdx = -1;
        maxRelativeIdx = -1;
    }

    return cellIsLoaded;
}

uint GetIndexData(APVResources apvRes, float3 posWS)
{
    int3 cellPos = floor(posWS / _CellInMeters);
    float3 topLeftCellWS = cellPos * _CellInMeters;

    bool isALoadedCell = all(cellPos <= _MaxLoadedCell) && all(cellPos >= _MinLoadedCell);

    // Make sure we start from 0
    cellPos -= (int3)_MinCellPosition;

    int flatIdx = cellPos.z * (_CellIndicesDim.x * _CellIndicesDim.y) + cellPos.y * _CellIndicesDim.x + cellPos.x;

    int stepSize = 0;
    int3 minRelativeIdx, maxRelativeIdx;
    int chunkIdx = -1;
    bool isValidBrick = true;
    int locationInPhysicalBuffer = 0;

    if (isALoadedCell && LoadCellIndexMetaData(flatIdx, chunkIdx, stepSize, minRelativeIdx, maxRelativeIdx))
    {
        float3 residualPosWS = posWS - topLeftCellWS;
        int3 localBrickIndex = floor(residualPosWS / (_MinBrickSize * stepSize));

        // Out of bounds.
        if (any(localBrickIndex < minRelativeIdx) || any(localBrickIndex >= maxRelativeIdx))
        {
            isValidBrick = false;
        }

        int3 sizeOfValid = maxRelativeIdx - minRelativeIdx;
        // Relative to valid region
        int3 localRelativeIndexLoc = (localBrickIndex - minRelativeIdx);
        int flattenedLocationInCell = localRelativeIndexLoc.z * (sizeOfValid.x * sizeOfValid.y) + localRelativeIndexLoc.x * sizeOfValid.y + localRelativeIndexLoc.y;

        locationInPhysicalBuffer = chunkIdx * _IndexChunkSize + flattenedLocationInCell;

    }
    else
    {
        isValidBrick = false;
    }

    return isValidBrick ? apvRes.index[locationInPhysicalBuffer] : 0xffffffff;
}

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

    apvRes.L2_0 = _APVResL2_0;
    apvRes.L2_1 = _APVResL2_1;
    apvRes.L2_2 = _APVResL2_2;
    apvRes.L2_3 = _APVResL2_3;

    apvRes.Validity = _APVResValidity;

    return apvRes;
}



bool TryToGetPoolUVWAndSubdiv(APVResources apvRes, float3 posWS, float3 normalWS, float3 viewDirWS, out float3 uvw, out uint subdiv, out float3 biasedPosWS)
{
    uvw = 0;
    // Note: we could instead early return when we know we'll have invalid UVs, but some bade code gen on Vulkan generates shader warnings if we do.
    bool hasValidUVW = true;

    float4 posWSForSample = float4(posWS + normalWS * _NormalBias
        + viewDirWS * _ViewBias, 1.0);
    biasedPosWS = posWSForSample.xyz;

    uint3 poolDim = (uint3)_PoolDim;

    // resolve the index
    float3 posRS = posWSForSample.xyz / _MinBrickSize;
    uint packed_pool_idx = GetIndexData(apvRes, posWSForSample.xyz);

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
    float3 unusedPos;
    return TryToGetPoolUVWAndSubdiv(apvRes, posWS, normalWS, viewDir, uvw, unusedSubdiv, unusedPos);
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

APVSample LoadAndDecodeAPV(APVResources apvRes, int3 loc)
{
    APVSample apvSample;

    float4 L0_L1Rx = LOAD_TEXTURE3D(apvRes.L0_L1Rx, loc).rgba;
    float4 L1G_L1Ry = LOAD_TEXTURE3D(apvRes.L1G_L1Ry, loc).rgba;
    float4 L1B_L1Rz = LOAD_TEXTURE3D(apvRes.L1B_L1Rz, loc).rgba;

    apvSample.L0 = L0_L1Rx.xyz;
    apvSample.L1_R = float3(L0_L1Rx.w, L1G_L1Ry.w, L1B_L1Rz.w);
    apvSample.L1_G = L1G_L1Ry.xyz;
    apvSample.L1_B = L1B_L1Rz.xyz;

#ifdef PROBE_VOLUMES_L2
    apvSample.L2_R = LOAD_TEXTURE3D(apvRes.L2_0, loc).rgba;
    apvSample.L2_G = LOAD_TEXTURE3D(apvRes.L2_1, loc).rgba;
    apvSample.L2_B = LOAD_TEXTURE3D(apvRes.L2_2, loc).rgba;
    apvSample.L2_C = LOAD_TEXTURE3D(apvRes.L2_3, loc).rgb;
#endif

    apvSample.status = APV_SAMPLE_STATUS_ENCODED;
    apvSample.Decode();

    return apvSample;
}

void WeightSample(inout APVSample apvSample, float weight)
{
    apvSample.L0 *= weight;
    apvSample.L1_R *= weight;
    apvSample.L1_G *= weight;
    apvSample.L1_B *= weight;

#ifdef PROBE_VOLUMES_L2
    apvSample.L2_R *= weight;
    apvSample.L2_G *= weight;
    apvSample.L2_B *= weight;
    apvSample.L2_C *= weight;
#endif
}

void AccumulateSamples(inout APVSample dst, APVSample other, float weight)
{
    WeightSample(other, weight);
    dst.L0   += other.L0;
    dst.L1_R += other.L1_R;
    dst.L1_G += other.L1_G;
    dst.L1_B += other.L1_B;

#ifdef PROBE_VOLUMES_L2
    dst.L2_R += other.L2_R;
    dst.L2_G += other.L2_G;
    dst.L2_B += other.L2_B;
    dst.L2_C += other.L2_C;
#endif
}

APVSample ManuallyFilteredSample(APVResources apvRes, float3 posWS, float3 normalWS, int subdiv, float3 biasedPosWS, float3 uvw)
{
    float3 texCoordFloat = uvw * _PoolDim - .5f;
    int3 texCoordInt = texCoordFloat;
    float3 texFrac = frac(texCoordFloat);
    float3 oneMinTexFrac = 1.0f - texFrac;

    bool sampled = false;
    float totalW = 0.0f;

    APVSample baseSample;

    float3 positionCentralProbe = GetSnappedProbePosition(biasedPosWS, subdiv);

    ZERO_INITIALIZE(APVSample, baseSample);

    uint validityMask = LOAD_TEXTURE3D(apvRes.Validity, texCoordInt).x * 255;
    for (int i = 0; i < 8; ++i)
    {
        uint3 offset = GetSampleOffset(i);
        float trilinearW =
            ((offset.x == 1) ? texFrac.x : oneMinTexFrac.x) *
            ((offset.y == 1) ? texFrac.y : oneMinTexFrac.y) *
            ((offset.z == 1) ? texFrac.z : oneMinTexFrac.z);

        float validityWeight = GetValidityWeight(i, validityMask);

        if (validityWeight > 0)
        {
            APVSample apvSample = LoadAndDecodeAPV(apvRes, texCoordInt + offset);
            float geoW = GetNormalWeight(offset, posWS, positionCentralProbe, normalWS, subdiv);

            float finalW = geoW * trilinearW;
            AccumulateSamples(baseSample, apvSample, finalW);
            totalW += finalW;
        }
    }

    WeightSample(baseSample, rcp(totalW));

    return baseSample;
}

void WarpUVWLeakReduction(APVResources apvRes, float3 posWS, float3 normalWS, int subdiv, float3 biasedPosWS, inout float3 uvw)
{
    float3 texCoordFloat = uvw * _PoolDim - .5f;
    int3 texCoordInt = texCoordFloat;
    float3 texFrac = frac(texCoordFloat);
    float3 oneMinTexFrac = 1.0f - texFrac;
    uint validityMask = LOAD_TEXTURE3D(apvRes.Validity, texCoordInt).x * 255;

    float3 newFrac = 0.0f;
    float weights[8] = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    float totalW = 0.0f;
    int i = 0;
    float3 positionCentralProbe = GetSnappedProbePosition(biasedPosWS, subdiv);

    for (i = 0; i < 8; ++i)
    {
        uint3 offset = GetSampleOffset(i);
        float trilinearW =
            ((offset.x == 1) ? texFrac.x : oneMinTexFrac.x) *
            ((offset.y == 1) ? texFrac.y : oneMinTexFrac.y) *
            ((offset.z == 1) ? texFrac.z : oneMinTexFrac.z);

        float validityWeight = GetValidityWeight(i, validityMask);

        float geoW = GetNormalWeight(offset, posWS, positionCentralProbe, normalWS, subdiv);

        weights[i] = max(0.0001f, saturate(trilinearW * geoW * validityWeight));
        totalW += weights[i];
    }

    for (i = 0; i < 8; ++i)
    {
        uint3 offset = GetSampleOffset(i);
        newFrac += (float3)offset * weights[i] * rcp(totalW);
    }

    uvw = ((texCoordFloat - texFrac + newFrac + 0.5) * rcp(_PoolDim));

}

APVSample SampleAPV(APVResources apvRes, float3 posWS, float3 biasNormalWS, float3 viewDir)
{
    APVSample outSample;

    float3 pool_uvw;
    uint subdiv;
    float3 biasedPosWS;
    if (TryToGetPoolUVWAndSubdiv(apvRes, posWS, biasNormalWS, viewDir, pool_uvw, subdiv, biasedPosWS))
    {
#if MANUAL_FILTERING == 1
        if (_LeakReductionParams.x != 0)
            outSample = ManuallyFilteredSample(apvRes, posWS, biasNormalWS, subdiv, biasedPosWS, pool_uvw);
        else
            outSample = SampleAPV(apvRes, pool_uvw);
#else
        if (_LeakReductionParams.x != 0)
        {
            WarpUVWLeakReduction(apvRes, posWS, biasNormalWS, subdiv, biasedPosWS, pool_uvw);
        }
        outSample = SampleAPV(apvRes, pool_uvw);
#endif
    }
    else
    {
        ZERO_INITIALIZE(APVSample, outSample);
        outSample.status = APV_SAMPLE_STATUS_INVALID;
    }

    return outSample;
}


APVSample SampleAPV(float3 posWS, float3 biasNormalWS, float3 viewDir)
{
    APVResources apvRes = FillAPVResources();
    return SampleAPV(apvRes, posWS, biasNormalWS, viewDir);
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

        if (_Weight < 1.f)
        {
            bakeDiffuseLighting = lerp(EvaluateAmbientProbe(normalWS), bakeDiffuseLighting, _Weight);
            backBakeDiffuseLighting = lerp(EvaluateAmbientProbe(backNormalWS), backBakeDiffuseLighting, _Weight);
        }
    }
    else
    {
        // no valid brick, fallback to ambient probe
        bakeDiffuseLighting = EvaluateAmbientProbe(normalWS);
        backBakeDiffuseLighting = EvaluateAmbientProbe(backNormalWS);
    }
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 reflDir, in float3 viewDir,
    in float2 positionSS, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting, out float3 lightingInReflDir)
{
    APVResources apvRes = FillAPVResources();

    posWS = AddNoiseToSamplingPosition(posWS, positionSS);

    APVSample apvSample = SampleAPV(posWS, normalWS, viewDir);

    if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
    {
#if MANUAL_FILTERING == 0
        apvSample.Decode();
#endif

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

        if (_Weight < 1.f)
        {
            bakeDiffuseLighting = lerp(EvaluateAmbientProbe(normalWS), bakeDiffuseLighting, _Weight);
            backBakeDiffuseLighting = lerp(EvaluateAmbientProbe(backNormalWS), backBakeDiffuseLighting, _Weight);
        }
    }
    else
    {
        bakeDiffuseLighting = EvaluateAmbientProbe(normalWS);
        backBakeDiffuseLighting = EvaluateAmbientProbe(backNormalWS);
        lightingInReflDir = -1;
    }
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 viewDir,
    in float2 positionSS, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    posWS = AddNoiseToSamplingPosition(posWS, positionSS);

    APVSample apvSample = SampleAPV(posWS, normalWS, viewDir);
    EvaluateAdaptiveProbeVolume(apvSample, normalWS, backNormalWS, bakeDiffuseLighting, backBakeDiffuseLighting);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float2 positionSS, out float3 bakeDiffuseLighting)
{
    APVResources apvRes = FillAPVResources();

    posWS = AddNoiseToSamplingPosition(posWS, positionSS);

    float3 uvw;
    if (TryToGetPoolUVW(apvRes, posWS, 0, 0, uvw))
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

float EvaluateReflectionProbeSH(float3 sampleDir, float4 reflProbeSHL0L1, float4 reflProbeSHL2_1, float reflProbeSHL2_2)
{
    float outFactor = 0;
    float L0 = reflProbeSHL0L1.x;
    float L1 = dot(reflProbeSHL0L1.yzw, sampleDir);

    outFactor = L0 + L1;

#ifdef PROBE_VOLUMES_L2

    // IMPORTANT: The encoding is unravelled C# side before being sent

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
    float refProbeNormalization = EvaluateReflectionProbeSH(sampleDir, reflProbeSHL0L1, reflProbeSHL2_1, reflProbeSHL2_2);

    float localNormalization = Luminance(lightingInReflDir);
    return lerp(1.f, clamp(SafeDiv(localNormalization, refProbeNormalization), _MinReflProbeNormalizationFactor, _MaxReflProbeNormalizationFactor), _Weight);

}

#endif // __PROBEVOLUME_HLSL__
