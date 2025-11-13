#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#if defined(SHADER_API_MOBILE) || defined(SHADER_API_SWITCH)
//#define USE_APV_TEXTURE_HALF
#endif // SHADER_API_MOBILE || SHADER_API_SWITCH

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ShaderVariablesProbeVolumes.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Unpack variables
#define _APVWorldOffset _Offset_LayerCount.xyz
#define _APVIndirectionEntryDim _MinLoadedCellInEntries_IndirectionEntryDim.w
#define _APVRcpIndirectionEntryDim _MaxLoadedCellInEntries_RcpIndirectionEntryDim.w
#define _APVMinBrickSize _PoolDim_MinBrickSize.w
#define _APVPoolDim _PoolDim_MinBrickSize.xyz
#define _APVRcpPoolDim _RcpPoolDim_XY.xyz
#define _APVRcpPoolDimXY _RcpPoolDim_XY.w
#define _APVMinEntryPosition _MinEntryPos_Noise.xyz
#define _APVSamplingNoise _MinEntryPos_Noise.w
#define _APVEntryCount _EntryCount_X_XY_LeakReduction.xy
#define _APVLeakReductionMode _EntryCount_X_XY_LeakReduction.z
#define _APVNormalBias _Biases_NormalizationClamp.x
#define _APVViewBias _Biases_NormalizationClamp.y
#define _APVMinLoadedCellInEntries _MinLoadedCellInEntries_IndirectionEntryDim.xyz
#define _APVMaxLoadedCellInEntries _MaxLoadedCellInEntries_RcpIndirectionEntryDim.xyz
#define _APVLayerCount (uint)(_Offset_LayerCount.w)
#define _APVMinReflProbeNormalizationFactor _Biases_NormalizationClamp.z
#define _APVMaxReflProbeNormalizationFactor _Biases_NormalizationClamp.w
#define _APVFrameIndex _FrameIndex_Weights.x
#define _APVWeight _FrameIndex_Weights.y
#define _APVSkyOcclusionWeight _FrameIndex_Weights.z
#define _APVSkyDirectionWeight _FrameIndex_Weights.w

#ifndef DECODE_SH
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
#endif

#ifndef __AMBIENTPROBE_HLSL__
float3 EvaluateAmbientProbe(float3 normalWS)
{
    return float3(0, 0, 0);
}
#endif

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
SAMPLER(s_linear_clamp_sampler);
SAMPLER(s_point_clamp_sampler);
#endif

#ifdef USE_APV_TEXTURE_HALF
#define TEXTURE3D_APV TEXTURE3D_HALF
#else
#define TEXTURE3D_APV TEXTURE3D_FLOAT
#endif

struct APVResources
{
    StructuredBuffer<int> index;
    StructuredBuffer<float3> SkyPrecomputedDirections;

    TEXTURE3D_APV(L0_L1Rx);
    TEXTURE3D_APV(L1G_L1Ry);
    TEXTURE3D_APV(L1B_L1Rz);
    TEXTURE3D_APV(L2_0);
    TEXTURE3D_APV(L2_1);
    TEXTURE3D_APV(L2_2);
    TEXTURE3D_APV(L2_3);
    TEXTURE3D_FLOAT(Validity); // Validity stores indices and requires full float precision to be decoded properly.

    TEXTURE3D_APV(ProbeOcclusion);

    TEXTURE3D_APV(SkyOcclusionL0L1);
    TEXTURE3D_FLOAT(SkyShadingDirectionIndices);
};

struct APVResourcesRW
{
    RWTexture3D<half4> L0_L1Rx;
    RWTexture3D<unorm float4> L1G_L1Ry;
    RWTexture3D<unorm float4> L1B_L1Rz;
    RWTexture3D<unorm float4> L2_0;
    RWTexture3D<unorm float4> L2_1;
    RWTexture3D<unorm float4> L2_2;
    RWTexture3D<unorm float4> L2_3;
    RWTexture3D<unorm float4> ProbeOcclusion;
};

#ifndef USE_APV_PROBE_OCCLUSION
// If we are rendering a probe lit renderer, and we have APV enabled, and we are using subtractive or shadowmask mode, we sample occlusion from APV.
#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)) && (defined(LIGHTMAP_SHADOW_MIXING) || defined(SHADOWS_SHADOWMASK))
#define USE_APV_PROBE_OCCLUSION 1
#endif
#endif

#define LOAD_APV_RES_L1(res, target) \
    res.L0_L1Rx  = CALL_MERGE_NAME(target, _L0_L1Rx); \
    res.L1G_L1Ry = CALL_MERGE_NAME(target, _L1G_L1Ry); \
    res.L1B_L1Rz = CALL_MERGE_NAME(target, _L1B_L1Rz);
#define LOAD_APV_RES_L2(res, target) \
    res.L2_0 = CALL_MERGE_NAME(target, _L2_0); \
    res.L2_1 = CALL_MERGE_NAME(target, _L2_1); \
    res.L2_2 = CALL_MERGE_NAME(target, _L2_2); \
    res.L2_3 = CALL_MERGE_NAME(target, _L2_3);
#define LOAD_APV_RES_OCCLUSION(res, target) \
    res.ProbeOcclusion = CALL_MERGE_NAME(target, _ProbeOcclusion);

#ifndef PROBE_VOLUMES_L2
    #ifndef USE_APV_PROBE_OCCLUSION
    # define LOAD_APV_RES(res, target) LOAD_APV_RES_L1(res, target)
    #else
    # define LOAD_APV_RES(res, target) LOAD_APV_RES_L1(res, target) \
        LOAD_APV_RES_OCCLUSION(res, target)
    #endif
#else
    #ifndef USE_APV_PROBE_OCCLUSION
    # define LOAD_APV_RES(res, target) \
        LOAD_APV_RES_L1(res, target) \
        LOAD_APV_RES_L2(res, target)
    #else
    # define LOAD_APV_RES(res, target) \
        LOAD_APV_RES_L1(res, target) \
        LOAD_APV_RES_L2(res, target) \
        LOAD_APV_RES_OCCLUSION(res, target)
    #endif
#endif

struct APVSample
{
    half3 L0;
    half3 L1_R;
    half3 L1_G;
    half3 L1_B;
#ifdef PROBE_VOLUMES_L2
    half4 L2_R;
    half4 L2_G;
    half4 L2_B;
    half3 L2_C;
#endif // PROBE_VOLUMES_L2

    float4 skyOcclusionL0L1;
    float3 skyShadingDirection;

#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion;
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
            DecodeSH_L2(L0, L2_R, L2_G, L2_B, L2_C);
#endif // PROBE_VOLUMES_L2

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
#endif // PROBE_VOLUMES_L2

            status = APV_SAMPLE_STATUS_ENCODED;
        }
    }
};

// Resources required for APV
StructuredBuffer<int> _APVResIndex;
StructuredBuffer<uint3> _APVResCellIndices;
StructuredBuffer<float3> _SkyPrecomputedDirections;
StructuredBuffer<uint> _AntiLeakData;

TEXTURE3D_APV(_APVResL0_L1Rx);

TEXTURE3D_APV(_APVResL1G_L1Ry);
TEXTURE3D_APV(_APVResL1B_L1Rz);
TEXTURE3D_APV(_APVResL2_0);
TEXTURE3D_APV(_APVResL2_1);
TEXTURE3D_APV(_APVResL2_2);
TEXTURE3D_APV(_APVResL2_3);

TEXTURE3D_APV(_APVProbeOcclusion);

TEXTURE3D_APV(_APVResValidity);

TEXTURE3D_APV(_SkyOcclusionTexL0L1);
TEXTURE3D(_SkyShadingDirectionIndicesTex);


// -------------------------------------------------------------
// Various weighting functions for occlusion or helper functions.
// -------------------------------------------------------------
float3 AddNoiseToSamplingPosition(float3 posWS, float2 positionSS, float3 direction)
{
#ifdef UNITY_SPACE_TRANSFORMS_INCLUDED
    float3 right = mul((float3x3)GetViewToWorldMatrix(), float3(1.0, 0.0, 0.0));
    float3 top = mul((float3x3)GetViewToWorldMatrix(), float3(0.0, 1.0, 0.0));
    float noise01 = InterleavedGradientNoise(positionSS, _APVFrameIndex);
    float noise02 = frac(noise01 * 100.0);
    float noise03 = frac(noise01 * 1000.0);
    direction += top * (noise02 - 0.5) + right * (noise03 - 0.5);
    return _APVSamplingNoise > 0 ? posWS + noise01 * _APVSamplingNoise * direction : posWS;
#else
    return posWS;
#endif
}

uint3 GetSampleOffset(uint i)
{
    return uint3(i, i >> 1, i >> 2) & 1;
}

// The validity mask is sampled once and contains a binary info on whether a probe neighbour (relevant for trilinear) is to be used
// or not. The entry in the mask uses the same mapping that GetSampleOffset above uses.
half GetValidityWeight(uint offset, uint validityMask)
{
    uint mask = 1U << offset;
    return (validityMask & mask) > 0 ? 1 : 0;
}

float ProbeDistance(uint subdiv)
{
    return pow(3, subdiv) * _APVMinBrickSize / 3.0f;
}

half ProbeDistanceHalf(uint subdiv)
{
    return pow(half(3), half(subdiv)) * half(_APVMinBrickSize) / 3.0;
}

float3 GetSnappedProbePosition(float3 posWS, uint subdiv)
{
    float3 distBetweenProbes = ProbeDistance(subdiv);
    float3 dividedPos = posWS / distBetweenProbes;
    return (dividedPos - frac(dividedPos)) * distBetweenProbes;
}

// -------------------------------------------------------------
// Indexing functions
// -------------------------------------------------------------

bool LoadCellIndexMetaData(uint cellFlatIdx, out uint chunkIndex, out int stepSize, out int3 minRelativeIdx, out uint3 sizeOfValid)
{
    uint3 metaData = _APVResCellIndices[cellFlatIdx];

    // See ProbeIndexOfIndices.cs for packing
    chunkIndex = metaData.x & 0x1FFFFFFF;
    stepSize = round(pow(3, (metaData.x >> 29) & 0x7));

    minRelativeIdx.x = metaData.y & 0x3FF;
    minRelativeIdx.y = (metaData.y >> 10) & 0x3FF;
    minRelativeIdx.z = (metaData.y >> 20) & 0x3FF;

    sizeOfValid.x = metaData.z & 0x3FF;
    sizeOfValid.y = (metaData.z >> 10) & 0x3FF;
    sizeOfValid.z = (metaData.z >> 20) & 0x3FF;

    return metaData.x != 0xFFFFFFFF;
}

uint GetIndexData(APVResources apvRes, float3 posWS)
{
    float3 entryPos = floor(posWS * _APVRcpIndirectionEntryDim);
    float3 topLeftEntryWS = entryPos * _APVIndirectionEntryDim;

    bool isALoadedCell = all(entryPos >= _APVMinLoadedCellInEntries) && all(entryPos <= _APVMaxLoadedCellInEntries);

    // Make sure we start from 0
    uint3 entryPosInt = (uint3)(entryPos - _APVMinEntryPosition);
    uint flatIdx = dot(entryPosInt, uint3(1, _APVEntryCount.x, _APVEntryCount.y));

    // Dynamic branch must be enforced to avoid out-of-bounds memory access in LoadCellIndexMetaData
    uint result = 0xffffffff;
    UNITY_BRANCH if (isALoadedCell)
    {
        int stepSize;
        int3 minRelativeIdx;
        uint3 sizeOfValid;
        uint chunkIdx;
        if (LoadCellIndexMetaData(flatIdx, chunkIdx, stepSize, minRelativeIdx, sizeOfValid))
        {
            float3 residualPosWS = posWS - topLeftEntryWS;
            uint3 localBrickIndex = floor(residualPosWS / (_APVMinBrickSize * stepSize));
            localBrickIndex = min(localBrickIndex, (uint3)(3 * 3 * 3 - 1)); // due to floating point issue, we may query an invalid brick
            localBrickIndex -= minRelativeIdx; // Relative to valid region

            UNITY_BRANCH
            if (all(localBrickIndex < sizeOfValid))
            {
                uint flattenedLocationInCell = dot(localBrickIndex, uint3(sizeOfValid.y, 1, sizeOfValid.x * sizeOfValid.y));
                uint locationInPhysicalBuffer = chunkIdx * (uint)PROBE_INDEX_CHUNK_SIZE + flattenedLocationInCell;
                result = apvRes.index[locationInPhysicalBuffer];
            }
        }
    }

    return result;
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
    apvRes.SkyOcclusionL0L1 = _SkyOcclusionTexL0L1;
    apvRes.SkyShadingDirectionIndices = _SkyShadingDirectionIndicesTex;
    apvRes.SkyPrecomputedDirections = _SkyPrecomputedDirections;

    apvRes.ProbeOcclusion = _APVProbeOcclusion;

    return apvRes;
}


bool TryToGetPoolUVWAndSubdiv(APVResources apvRes, float3 posWSForSample, out float3 uvw, out uint subdiv)
{
    // resolve the index
    uint packed_pool_idx = GetIndexData(apvRes, posWSForSample.xyz);

    // unpack pool idx
    // size is encoded in the upper 4 bits
    subdiv = (packed_pool_idx >> 28) & 15;

    float   flattened_pool_idx = packed_pool_idx & ((1 << 28) - 1);
    float3 pool_idx;
    pool_idx.z = floor(flattened_pool_idx * _APVRcpPoolDimXY);
    flattened_pool_idx -= (pool_idx.z * (_APVPoolDim.x * _APVPoolDim.y));
    pool_idx.y = floor(flattened_pool_idx * _APVRcpPoolDim.x);
    pool_idx.x = floor(flattened_pool_idx - (pool_idx.y * _APVPoolDim.x));

    // calculate uv offset and scale
    float brickSizeWS = pow(3.0, subdiv) * _APVMinBrickSize;
    float3 offset = frac(posWSForSample.xyz / brickSizeWS);  // [0;1] in brick space
    //offset    = clamp( offset, 0.25, 0.75 );      // [0.25;0.75] in brick space (is this actually necessary?)

    uvw = (pool_idx + 0.5 + (3.0 * offset)) * _APVRcpPoolDim; // add offset with brick footprint converted to text footprint in pool texel space

    // no valid brick loaded for this index, fallback to ambient probe
    // Note: we could instead early return when we know we'll have invalid UVs, but some bade code gen on Vulkan generates shader warnings if we do.
    return packed_pool_idx != 0xffffffffu;
}

bool TryToGetPoolUVWAndSubdiv(APVResources apvRes, float3 posWS, float3 normalWS, float3 viewDirWS, out float3 uvw, out uint subdiv, out float3 biasedPosWS)
{
    biasedPosWS = (posWS + normalWS * _APVNormalBias) + viewDirWS * _APVViewBias;
    return TryToGetPoolUVWAndSubdiv(apvRes, biasedPosWS, uvw, subdiv);
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
    half4 L0_L1Rx = half4(SAMPLE_TEXTURE3D_LOD(apvRes.L0_L1Rx, s_linear_clamp_sampler, uvw, 0).rgba);
    half4 L1G_L1Ry = half4(SAMPLE_TEXTURE3D_LOD(apvRes.L1G_L1Ry, s_linear_clamp_sampler, uvw, 0).rgba);
    half4 L1B_L1Rz = half4(SAMPLE_TEXTURE3D_LOD(apvRes.L1B_L1Rz, s_linear_clamp_sampler, uvw, 0).rgba);

    apvSample.L0 = L0_L1Rx.xyz;
    apvSample.L1_R = half3(L0_L1Rx.w, L1G_L1Ry.w, L1B_L1Rz.w);
    apvSample.L1_G = L1G_L1Ry.xyz;
    apvSample.L1_B = L1B_L1Rz.xyz;

#ifdef PROBE_VOLUMES_L2
    apvSample.L2_R = half4(SAMPLE_TEXTURE3D_LOD(apvRes.L2_0, s_linear_clamp_sampler, uvw, 0).rgba);
    apvSample.L2_G = half4(SAMPLE_TEXTURE3D_LOD(apvRes.L2_1, s_linear_clamp_sampler, uvw, 0).rgba);
    apvSample.L2_B = half4(SAMPLE_TEXTURE3D_LOD(apvRes.L2_2, s_linear_clamp_sampler, uvw, 0).rgba);
    apvSample.L2_C = half3(SAMPLE_TEXTURE3D_LOD(apvRes.L2_3, s_linear_clamp_sampler, uvw, 0).rgb);
#endif // PROBE_VOLUMES_L2

    if (_APVSkyOcclusionWeight > 0)
        apvSample.skyOcclusionL0L1 = SAMPLE_TEXTURE3D_LOD(apvRes.SkyOcclusionL0L1, s_linear_clamp_sampler, uvw, 0).rgba;
    else
        apvSample.skyOcclusionL0L1 = float4(0, 0, 0, 0);

    if (_APVSkyDirectionWeight > 0)
    {
        // No interpolation for sky shading indices
        int3 texCoord = uvw * _APVPoolDim - 0.5f;
        uint index = LOAD_TEXTURE3D(apvRes.SkyShadingDirectionIndices, texCoord).x * 255.0;

        if (index == 255)
            apvSample.skyShadingDirection = float3(0, 0, 0);
        else
            apvSample.skyShadingDirection = apvRes.SkyPrecomputedDirections[index].rgb;
    }
    else
        apvSample.skyShadingDirection = float3(0, 0, 0);

#ifdef USE_APV_PROBE_OCCLUSION
    apvSample.probeOcclusion = SAMPLE_TEXTURE3D_LOD(apvRes.ProbeOcclusion, s_linear_clamp_sampler, uvw, 0).rgba;
#endif

    apvSample.status = APV_SAMPLE_STATUS_ENCODED;

    return apvSample;
}

APVSample LoadAndDecodeAPV(APVResources apvRes, int3 loc)
{
    APVSample apvSample;

    half4 L0_L1Rx =  half4(LOAD_TEXTURE3D(apvRes.L0_L1Rx, loc).rgba);
    half4 L1G_L1Ry = half4(LOAD_TEXTURE3D(apvRes.L1G_L1Ry, loc).rgba);
    half4 L1B_L1Rz = half4(LOAD_TEXTURE3D(apvRes.L1B_L1Rz, loc).rgba);

    apvSample.L0 = L0_L1Rx.xyz;
    apvSample.L1_R = half3(L0_L1Rx.w, L1G_L1Ry.w, L1B_L1Rz.w);
    apvSample.L1_G = L1G_L1Ry.xyz;
    apvSample.L1_B = L1B_L1Rz.xyz;

#ifdef PROBE_VOLUMES_L2
    apvSample.L2_R = half4(LOAD_TEXTURE3D(apvRes.L2_0, loc).rgba);
    apvSample.L2_G = half4(LOAD_TEXTURE3D(apvRes.L2_1, loc).rgba);
    apvSample.L2_B = half4(LOAD_TEXTURE3D(apvRes.L2_2, loc).rgba);
    apvSample.L2_C = half3(LOAD_TEXTURE3D(apvRes.L2_3, loc).rgb);
#endif // PROBE_VOLUMES_L2

#ifdef USE_APV_PROBE_OCCLUSION
    apvSample.probeOcclusion = LOAD_TEXTURE3D(apvRes.ProbeOcclusion, loc).rgba;
#endif

    apvSample.status = APV_SAMPLE_STATUS_ENCODED;
    apvSample.Decode();

    return apvSample;
}

void WeightSample(inout APVSample apvSample, half weight)
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
#endif // PROBE_VOLUMES_L2

#ifdef USE_APV_PROBE_OCCLUSION
    apvSample.probeOcclusion *= weight;
#endif

    apvSample.skyOcclusionL0L1 *= weight;
}

void AccumulateSamples(inout APVSample dst, APVSample other, half weight)
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
#endif // PROBE_VOLUMES_L2

#ifdef USE_APV_PROBE_OCCLUSION
    dst.probeOcclusion += other.probeOcclusion;
#endif

    dst.skyOcclusionL0L1 += other.skyOcclusionL0L1;
}

uint LoadValidityMask(APVResources apvRes, uint renderingLayer, int3 coord)
{
    float rawValidity = LOAD_TEXTURE3D(apvRes.Validity, coord).x;

    uint validityMask;
    if (_APVLayerCount == 1)
    {
        validityMask = rawValidity * 255.0;
    }
    else
    {
        // If the object is on none of the masks, enable all layers to still sample validity correctly
        uint globalLayer = _ProbeVolumeLayerMask[0] | _ProbeVolumeLayerMask[1] | _ProbeVolumeLayerMask[2] | _ProbeVolumeLayerMask[3];
        if ((renderingLayer & globalLayer) == 0) renderingLayer = 0xFFFFFFFF;

        validityMask = 0;
        if ((renderingLayer & _ProbeVolumeLayerMask[0]) != 0)
            validityMask = asuint(rawValidity);
        if ((renderingLayer & _ProbeVolumeLayerMask[1]) != 0)
            validityMask |= asuint(rawValidity) >> 8;
        if ((renderingLayer & _ProbeVolumeLayerMask[2]) != 0)
            validityMask |= asuint(rawValidity) >> 16;
        if ((renderingLayer & _ProbeVolumeLayerMask[3]) != 0)
            validityMask |= asuint(rawValidity) >> 24;
        validityMask = validityMask & 0xFF;
    }

    return validityMask;
}

float UnpackSamplingWeight(uint mask, float3 texFrac)
{
    int3 dir = 0;
    dir.x = (int)(mask >> 1) & 3;
    dir.y = (int)(mask >> 4) & 3;
    dir.z = (int)(mask >> 7) & 3;

    float3 weights;
    weights.x = saturate(mask & 1)  + (dir.x - 1) * texFrac.x;
    weights.y = saturate(mask & 8)  + (dir.y - 1) * texFrac.y;
    weights.z = saturate(mask & 64) + (dir.z - 1) * texFrac.z;

    return weights.x * weights.y * weights.z;
}

void UnpackSamplingOffset(uint mask, float3 texFrac, out float3 uvwOffset)
{
    uvwOffset.x = saturate(mask & 4)   + (saturate(mask & 2) * texFrac.x - texFrac.x);
    uvwOffset.y = saturate(mask & 32)  + (saturate(mask & 16) * texFrac.y - texFrac.y);
    uvwOffset.z = saturate(mask & 256) + (saturate(mask & 128) * texFrac.z - texFrac.z);
}

APVSample QualityLeakReduction(APVResources apvRes, uint renderingLayer, inout float3 uvw)
{
    float3 texCoord = uvw * _APVPoolDim - .5f;
    float3 texFrac = frac(texCoord);

    half3 offset;

    uint validityMask = LoadValidityMask(apvRes, renderingLayer, texCoord);
    uint antileak = _AntiLeakData[validityMask];

    UnpackSamplingOffset(antileak >> 0, texFrac, offset);
    APVSample apvSample = SampleAPV(apvRes, uvw + offset * _APVRcpPoolDim);

    // Optional additional samples for quality leak reduction
    if (validityMask != 0xFF)
    {
        float3 weights;
        weights.x = UnpackSamplingWeight(antileak >> 0, texFrac);
        weights.y = UnpackSamplingWeight(antileak >> 9, texFrac);
        weights.z = UnpackSamplingWeight(antileak >> 18, texFrac);

        weights *= rcp(max(0.0001, weights.x+weights.y+weights.z));
        WeightSample(apvSample, weights.x);

        UNITY_BRANCH if (weights.y != 0)
        {
            UnpackSamplingOffset(antileak >> 9, texFrac, offset);
            APVSample partialSample = SampleAPV(apvRes, uvw + offset * _APVRcpPoolDim);
            AccumulateSamples(apvSample, partialSample, weights.y);
        }

        UNITY_BRANCH if (weights.z != 0)
        {
            UnpackSamplingOffset(antileak >> 18, texFrac, offset);
            APVSample partialSample = SampleAPV(apvRes, uvw + offset * _APVRcpPoolDim);
            AccumulateSamples(apvSample, partialSample, weights.z);
        }
    }

    return apvSample;
}

void WarpUVWLeakReduction(APVResources apvRes, uint renderingLayer, inout float3 uvw)
{
    float3 texCoord = uvw * _APVPoolDim - 0.5f;
    half3 texFrac = half3(frac(texCoord));

    uint validityMask = LoadValidityMask(apvRes, renderingLayer, texCoord);
    if (validityMask != 0xFF)
    {
        half weights[8];
        half totalW = 0.0;
        half3 offset = 0;
        uint i;

        UNITY_UNROLL
        for (i = 0; i < 8; ++i)
        {
            uint3 probeOffset = GetSampleOffset(i);
            half validityWeight =
                ((probeOffset.x == 1) ? texFrac.x : 1.0f - texFrac.x) *
                ((probeOffset.y == 1) ? texFrac.y : 1.0f - texFrac.y) *
                ((probeOffset.z == 1) ? texFrac.z : 1.0f - texFrac.z);

            validityWeight *= GetValidityWeight(i, validityMask);
            half weight = saturate(validityWeight);
            weights[i] = weight;
            totalW += weight;
        }

        offset = -texFrac;
        half rcpTotalW = rcp(max(0.0001, totalW));

        UNITY_UNROLL
        for (i = 0; i < 8; ++i)
        {
            uint3 probeOffset = GetSampleOffset(i);
            offset += (half3)probeOffset * (weights[i] * rcpTotalW);
        }

        uvw += (float3)offset * _APVRcpPoolDim;
    }
}

APVSample SampleAPV(APVResources apvRes, float3 posWS, float3 biasNormalWS, uint renderingLayer, float3 viewDir)
{
    APVSample outSample;

    posWS -= _APVWorldOffset;

    uint subdiv;
    float3 pool_uvw;
    float3 biasedPosWS;
    if (TryToGetPoolUVWAndSubdiv(apvRes, posWS, biasNormalWS, viewDir, pool_uvw, subdiv, biasedPosWS))
    {
        UNITY_BRANCH if (_APVLeakReductionMode == APVLEAKREDUCTIONMODE_QUALITY)
        {
            outSample = QualityLeakReduction(apvRes, renderingLayer, pool_uvw);
        }
        else
        {
            if (_APVLeakReductionMode == APVLEAKREDUCTIONMODE_PERFORMANCE)
                WarpUVWLeakReduction(apvRes, renderingLayer, pool_uvw);
            outSample = SampleAPV(apvRes, pool_uvw);
        }
    }
    else
    {
        ZERO_INITIALIZE(APVSample, outSample);

        #ifdef USE_APV_PROBE_OCCLUSION
        // The "neutral" value for probe occlusion is 1.0. We write it here to prevent objects that are outside
        // the bounds of the probe volume from being entirely shadowed.
        outSample.probeOcclusion = 1.0f;
        #endif

        outSample.status = APV_SAMPLE_STATUS_INVALID;
    }

    return outSample;
}


APVSample SampleAPV(float3 posWS, float3 biasNormalWS, uint renderingLayer, float3 viewDir)
{
    APVResources apvRes = FillAPVResources();
    return SampleAPV(apvRes, posWS, biasNormalWS, renderingLayer, viewDir);
}

// -------------------------------------------------------------
// Dynamic Sky Handling
// -------------------------------------------------------------

// Expects Layout DC, x, y, z
// See on baking side in DynamicGISkyOcclusion.hlsl
float EvalSHSkyOcclusion(float3 dir, APVSample apvSample)
{
    // L0 L1
    float4 temp = float4(kSHBasis0, kSHBasis1 * dir.x, kSHBasis1 * dir.y, kSHBasis1 * dir.z);
    return _APVSkyOcclusionWeight * dot(temp, apvSample.skyOcclusionL0L1);
}

float3 EvaluateOccludedSky(APVSample apvSample, float3 N)
{
    float occValue = EvalSHSkyOcclusion(N, apvSample);
    float3 shadingNormal = N;

    if (_APVSkyDirectionWeight > 0)
    {
        shadingNormal = apvSample.skyShadingDirection;
        float normSquared = dot(shadingNormal, shadingNormal);
        if (normSquared < 0.2f)
            shadingNormal = N;
        else
        {
            shadingNormal = shadingNormal * rsqrt(normSquared);
        }
    }
    return occValue * EvaluateAmbientProbe(shadingNormal);
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
void EvaluateAdaptiveProbeVolume(APVSample apvSample, float3 normalWS, out float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0.0f, 0.0f, 0.0f);
    if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
    {
        apvSample.Decode();

#if defined(PROBE_VOLUMES_L1)
        EvaluateAPVL1(apvSample, normalWS, bakeDiffuseLighting);
#elif defined(PROBE_VOLUMES_L2)
        EvaluateAPVL1L2(apvSample, normalWS, bakeDiffuseLighting);
#endif

        bakeDiffuseLighting += apvSample.L0;
        if (_APVSkyOcclusionWeight > 0)
            bakeDiffuseLighting += EvaluateOccludedSky(apvSample, normalWS);

        //if (_APVWeight < 1.f)
        {
            bakeDiffuseLighting = bakeDiffuseLighting * _APVWeight;
        }
    }
    else
    {
        // no valid brick, fallback to ambient probe
        bakeDiffuseLighting = EvaluateAmbientProbe(normalWS);
    }
}

void EvaluateAdaptiveProbeVolume(APVSample apvSample, float3 normalWS, float3 backNormalWS, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
    {
        apvSample.Decode();

#ifdef PROBE_VOLUMES_L1
        EvaluateAPVL1(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1(apvSample, backNormalWS, backBakeDiffuseLighting);
#elif defined(PROBE_VOLUMES_L2)
        EvaluateAPVL1L2(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1L2(apvSample, backNormalWS, backBakeDiffuseLighting);
#endif

        bakeDiffuseLighting += apvSample.L0;
        backBakeDiffuseLighting += apvSample.L0;
        if (_APVSkyOcclusionWeight > 0)
        {
            bakeDiffuseLighting += EvaluateOccludedSky(apvSample, normalWS);
            backBakeDiffuseLighting += EvaluateOccludedSky(apvSample, backNormalWS);
        }

        //if (_APVWeight < 1.f)
        {
            bakeDiffuseLighting = bakeDiffuseLighting * _APVWeight;
            backBakeDiffuseLighting = backBakeDiffuseLighting * _APVWeight;
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
    in float2 positionSS, in uint renderingLayer, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting, out float3 lightingInReflDir)
{
    APVResources apvRes = FillAPVResources();

    posWS = AddNoiseToSamplingPosition(posWS, positionSS, viewDir);

    APVSample apvSample = SampleAPV(posWS, normalWS, renderingLayer, viewDir);

    if (apvSample.status != APV_SAMPLE_STATUS_INVALID)
    {
        apvSample.Decode();

#ifdef PROBE_VOLUMES_L1
        EvaluateAPVL1(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1(apvSample, backNormalWS, backBakeDiffuseLighting);
        EvaluateAPVL1(apvSample, reflDir, lightingInReflDir);
#elif defined(PROBE_VOLUMES_L2)
        EvaluateAPVL1L2(apvSample, normalWS, bakeDiffuseLighting);
        EvaluateAPVL1L2(apvSample, backNormalWS, backBakeDiffuseLighting);
        EvaluateAPVL1L2(apvSample, reflDir, lightingInReflDir);
#endif

        bakeDiffuseLighting += apvSample.L0;
        backBakeDiffuseLighting += apvSample.L0;
        lightingInReflDir += apvSample.L0;
        if (_APVSkyOcclusionWeight > 0)
        {
            bakeDiffuseLighting += EvaluateOccludedSky(apvSample, normalWS);
            backBakeDiffuseLighting += EvaluateOccludedSky(apvSample, backNormalWS);
            lightingInReflDir += EvaluateOccludedSky(apvSample, reflDir);
        }

        //if (_APVWeight < 1.f)
        {
            bakeDiffuseLighting = bakeDiffuseLighting * _APVWeight;
            backBakeDiffuseLighting = backBakeDiffuseLighting * _APVWeight;
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
    in float2 positionSS, in uint renderingLayer, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting, out float4 probeOcclusion)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    posWS = AddNoiseToSamplingPosition(posWS, positionSS, viewDir);

    APVSample apvSample = SampleAPV(posWS, normalWS, renderingLayer, viewDir);
#ifdef USE_APV_PROBE_OCCLUSION
    probeOcclusion = apvSample.probeOcclusion;
#else
    probeOcclusion = 1;
#endif

    EvaluateAdaptiveProbeVolume(apvSample, normalWS, backNormalWS, bakeDiffuseLighting, backBakeDiffuseLighting);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 viewDir,
    in float2 positionSS, in uint renderingLayer, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{
    float4 unusedProbeOcclusion = 0;
    EvaluateAdaptiveProbeVolume(posWS, normalWS, backNormalWS, viewDir, positionSS, renderingLayer, bakeDiffuseLighting, backBakeDiffuseLighting, unusedProbeOcclusion);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 viewDir, in float2 positionSS, in uint renderingLayer,
    out float3 bakeDiffuseLighting, out float4 probeOcclusion)
{
    bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    posWS = AddNoiseToSamplingPosition(posWS, positionSS, viewDir);

    APVSample apvSample = SampleAPV(posWS, normalWS, renderingLayer, viewDir);
#ifdef USE_APV_PROBE_OCCLUSION
    probeOcclusion = apvSample.probeOcclusion;
#else
    probeOcclusion = 1;
#endif

    EvaluateAdaptiveProbeVolume(apvSample, normalWS, bakeDiffuseLighting);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 viewDir, in float2 positionSS, in uint renderingLayer,
    out float3 bakeDiffuseLighting)
{
    float4 unusedProbeOcclusion = 0;
    EvaluateAdaptiveProbeVolume(posWS, normalWS, viewDir, positionSS, renderingLayer, bakeDiffuseLighting, unusedProbeOcclusion);
}

void EvaluateAdaptiveProbeVolume(in float3 posWS, in float2 positionSS, out float3 bakeDiffuseLighting)
{
    APVResources apvRes = FillAPVResources();
    posWS = AddNoiseToSamplingPosition(posWS, positionSS, 1);
    posWS -= _APVWorldOffset;

    float3 ambientProbe = EvaluateAmbientProbe(0);

    float3 uvw;
    if (TryToGetPoolUVW(apvRes, posWS, 0, 0, uvw))
    {
        bakeDiffuseLighting = SAMPLE_TEXTURE3D_LOD(apvRes.L0_L1Rx, s_linear_clamp_sampler, uvw, 0).rgb;
        if (_APVSkyOcclusionWeight > 0)
        {
            float skyOcclusionL0 = kSHBasis0 * SAMPLE_TEXTURE3D_LOD(apvRes.SkyOcclusionL0L1, s_linear_clamp_sampler, uvw, 0).x;
            bakeDiffuseLighting += ambientProbe * skyOcclusionL0;
        }
    }
    else
    {
        bakeDiffuseLighting = ambientProbe;
    }
}

// public APIs for backward compatibility
// to be removed after Unity 6
void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 reflDir, in float3 viewDir, in float2 positionSS, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting, out float3 lightingInReflDir)
{ EvaluateAdaptiveProbeVolume(posWS, normalWS, backNormalWS, reflDir, viewDir, positionSS, 0xFFFFFFFF, bakeDiffuseLighting, backBakeDiffuseLighting, lightingInReflDir); }
void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 backNormalWS, in float3 viewDir, in float2 positionSS, out float3 bakeDiffuseLighting, out float3 backBakeDiffuseLighting)
{ EvaluateAdaptiveProbeVolume(posWS, normalWS, backNormalWS, viewDir, positionSS, 0xFFFFFFFF, bakeDiffuseLighting, backBakeDiffuseLighting); }
void EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in float3 viewDir, in float2 positionSS, out float3 bakeDiffuseLighting)
{ EvaluateAdaptiveProbeVolume(posWS, normalWS, viewDir, positionSS, 0xFFFFFFFF, bakeDiffuseLighting); }

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
#endif // PROBE_VOLUMES_L2

    return outFactor;
}

float GetReflectionProbeNormalizationFactor(float3 lightingInReflDir, float3 sampleDir, float4 reflProbeSHL0L1, float4 reflProbeSHL2_1, float reflProbeSHL2_2)
{
    float refProbeNormalization = EvaluateReflectionProbeSH(sampleDir, reflProbeSHL0L1, reflProbeSHL2_1, reflProbeSHL2_2);

    float localNormalization = Luminance(real3(lightingInReflDir));
    return lerp(1.f, clamp(SafeDiv(localNormalization, refProbeNormalization), _APVMinReflProbeNormalizationFactor, _APVMaxReflProbeNormalizationFactor), _APVWeight);

}

#endif // __PROBEVOLUME_HLSL__
