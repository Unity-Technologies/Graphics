#ifndef __PROBEVOLUMEATLAS_HLSL__
#define __PROBEVOLUMEATLAS_HLSL__

float ProbeVolumeSampleValidity(float3 probeVolumeAtlasUVW)
{
#if SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    return SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 3), 0).x;
#elif SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    return SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 6), 0).w;
#else
    return 0.0;
#endif
}

float ProbeVolumeLoadValidity(int3 probeVolumeAtlasTexelCoord)
{
#if SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    return LOAD_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, int3(probeVolumeAtlasTexelCoord.x, probeVolumeAtlasTexelCoord.y, probeVolumeAtlasTexelCoord.z + _ProbeVolumeAtlasResolutionAndSliceCount.z * 3), 0).x;
#elif SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    return LOAD_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, int3(probeVolumeAtlasTexelCoord.x, probeVolumeAtlasTexelCoord.y, probeVolumeAtlasTexelCoord.z + _ProbeVolumeAtlasResolutionAndSliceCount.z * 6), 0).w;
#else
    return 0.0;
#endif
}

struct ProbeVolumeSphericalHarmonicsL0
{
    float4 data[1];
};

struct ProbeVolumeSphericalHarmonicsL1
{
    float4 data[3];
};

struct ProbeVolumeSphericalHarmonicsL2
{
    float4 data[7];
};

// See ProbeVolumeAtlasBlit.compute for atlas coefficient layout information.
void ProbeVolumeSampleAccumulateSphericalHarmonicsL0(float3 probeVolumeAtlasUVW, float weight, inout ProbeVolumeSphericalHarmonicsL0 coefficients)
{
    coefficients.data[0].xyz += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0).xyz * weight;
}

void ProbeVolumeSampleAccumulateSphericalHarmonicsL1(float3 probeVolumeAtlasUVW, float weight, inout ProbeVolumeSphericalHarmonicsL1 coefficients)
{
#if SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1 || SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    coefficients.data[0] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0) * weight;
    coefficients.data[1] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 1), 0) * weight;
    coefficients.data[2] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 2), 0) * weight;
#endif
}

void ProbeVolumeSampleAccumulateSphericalHarmonicsL2(float3 probeVolumeAtlasUVW, float weight, inout ProbeVolumeSphericalHarmonicsL2 coefficients)
{
#if SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    // Requesting SH2, but atlas only contains SH1.
    // Only accumulate SH1 coefficients.
    coefficients.data[0] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0) * weight;
    coefficients.data[1] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 1), 0) * weight;
    coefficients.data[2] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 2), 0) * weight;

#elif SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    coefficients.data[0] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0) * weight;
    coefficients.data[1] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 1), 0) * weight;
    coefficients.data[2] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 2), 0) * weight;

    coefficients.data[3] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 3), 0) * weight;
    coefficients.data[4] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 4), 0) * weight;
    coefficients.data[5] += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 5), 0) * weight;

    coefficients.data[6].xyz += SAMPLE_TEXTURE3D_LOD(_ProbeVolumeAtlasSH, s_linear_clamp_sampler, float3(probeVolumeAtlasUVW.x, probeVolumeAtlasUVW.y, probeVolumeAtlasUVW.z + _ProbeVolumeAtlasResolutionAndSliceCountInverse.w * 6), 0).xyz * weight;
#endif
}

// Utility functions for converting from atlas coefficients layout, into the layout that our EntityLighting.hlsl evaluation functions expect.
void ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL0(inout ProbeVolumeSphericalHarmonicsL0 coefficients)
{
    // Nothing to do here. DC terms are already normalized and stored in RGB order.
}

void ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL1(inout ProbeVolumeSphericalHarmonicsL1 coefficients)
{
#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS || _DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        // coefficients are storing debug info. Do not swizzle or normalize.
        return;
    }
#endif

    // SHEvalLinearL0L1() expects coefficients in real4 shAr, real4 shAg, real4 shAb vectors whos channels are laid out {x, y, z, DC}
    float4 shAr = float4(coefficients.data[0].w, coefficients.data[1].x, coefficients.data[1].y, coefficients.data[0].x);
    float4 shAg = float4(coefficients.data[1].z, coefficients.data[1].w, coefficients.data[2].x, coefficients.data[0].y);
    float4 shAb = float4(coefficients.data[2].y, coefficients.data[2].z, coefficients.data[2].w, coefficients.data[0].z);

    coefficients.data[0] = shAr;
    coefficients.data[1] = shAg;
    coefficients.data[2] = shAb;
}

void ProbeVolumeSwizzleAndNormalizeSphericalHarmonicsL2(inout ProbeVolumeSphericalHarmonicsL2 coefficients)
{
#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS || _DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        // coefficients are storing debug info. Do not swizzle or normalize.
        return;
    }
#endif

    // SampleSH9() expects coefficients in shAr, shAg, shAb, shBr, shBg, shBb, shCr vectors.
    float4 shAr = float4(coefficients.data[0].w, coefficients.data[1].x, coefficients.data[1].y, coefficients.data[0].x);
    float4 shAg = float4(coefficients.data[1].z, coefficients.data[1].w, coefficients.data[2].x, coefficients.data[0].y);
    float4 shAb = float4(coefficients.data[2].y, coefficients.data[2].z, coefficients.data[2].w, coefficients.data[0].z);

    coefficients.data[0] = shAr;
    coefficients.data[1] = shAg;
    coefficients.data[2] = shAb;

    // coefficients[3] through coefficients[6] are already laid out in shBr, shBg, shBb, shCr order.
    // Now just need to perform final SH2 normalization:
    // Again, normalization from: https://www.ppsloan.org/publications/StupidSH36.pdf
    // Appendix A10 Shader/CPU code for Irradiance Environment Maps
    
    // Normalize DC term:
    coefficients.data[0].w -= coefficients.data[3].z;
    coefficients.data[1].w -= coefficients.data[4].z;
    coefficients.data[2].w -= coefficients.data[5].z;

    // Normalize Quadratic term:
    coefficients.data[3].z *= 3.0;
    coefficients.data[4].z *= 3.0;
    coefficients.data[5].z *= 3.0;
}

#endif