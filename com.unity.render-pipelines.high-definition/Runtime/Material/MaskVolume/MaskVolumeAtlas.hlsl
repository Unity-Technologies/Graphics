#ifndef __MASKVOLUMEATLAS_HLSL__
#define __MASKVOLUMEATLAS_HLSL__

float MaskVolumeSampleValidity(float3 maskVolumeAtlasUVW)
{
#if SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    return SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 3), 0).x;
#elif SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    return SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 6), 0).w;
#else
    return 0.0;
#endif
}

float MaskVolumeLoadValidity(int3 maskVolumeAtlasTexelCoord)
{
#if SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    return LOAD_TEXTURE3D_LOD(_MaskVolumeAtlasSH, int3(maskVolumeAtlasTexelCoord.x, maskVolumeAtlasTexelCoord.y, maskVolumeAtlasTexelCoord.z + _MaskVolumeAtlasResolutionAndSliceCount.z * 3), 0).x;
#elif SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    return LOAD_TEXTURE3D_LOD(_MaskVolumeAtlasSH, int3(maskVolumeAtlasTexelCoord.x, maskVolumeAtlasTexelCoord.y, maskVolumeAtlasTexelCoord.z + _MaskVolumeAtlasResolutionAndSliceCount.z * 6), 0).w;
#else
    return 0.0;
#endif
}

struct MaskVolumeSphericalHarmonicsL0
{
    float4 data[1];
};

struct MaskVolumeSphericalHarmonicsL1
{
    float4 data[3];
};

struct MaskVolumeSphericalHarmonicsL2
{
    float4 data[7];
};

// See MaskVolumeAtlasBlit.compute for atlas coefficient layout information.
void MaskVolumeSampleAccumulateSphericalHarmonicsL0(float3 maskVolumeAtlasUVW, float weight, inout MaskVolumeSphericalHarmonicsL0 coefficients)
{
    coefficients.data[0].xyz += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0).xyz * weight;
}

void MaskVolumeSampleAccumulateSphericalHarmonicsL1(float3 maskVolumeAtlasUVW, float weight, inout MaskVolumeSphericalHarmonicsL1 coefficients)
{
#if SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1 || SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    coefficients.data[0] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0) * weight;
    coefficients.data[1] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 1), 0) * weight;
    coefficients.data[2] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 2), 0) * weight;
#endif
}

void MaskVolumeSampleAccumulateSphericalHarmonicsL2(float3 maskVolumeAtlasUVW, float weight, inout MaskVolumeSphericalHarmonicsL2 coefficients)
{
#if SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    // Requesting SH2, but atlas only contains SH1.
    // Only accumulate SH1 coefficients.
    coefficients.data[0] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0) * weight;
    coefficients.data[1] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 1), 0) * weight;
    coefficients.data[2] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 2), 0) * weight;

#elif SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE == MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    coefficients.data[0] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0) * weight;
    coefficients.data[1] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 1), 0) * weight;
    coefficients.data[2] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 2), 0) * weight;

    coefficients.data[3] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 3), 0) * weight;
    coefficients.data[4] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 4), 0) * weight;
    coefficients.data[5] += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 5), 0) * weight;

    coefficients.data[6].xyz += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 6), 0).xyz * weight;
#endif
}

// Utility functions for converting from atlas coefficients layout, into the layout that our EntityLighting.hlsl evaluation functions expect.
void MaskVolumeSwizzleAndNormalizeSphericalHarmonicsL0(inout MaskVolumeSphericalHarmonicsL0 coefficients)
{
    // Nothing to do here. DC terms are already normalized and stored in RGB order.
}

void MaskVolumeSwizzleAndNormalizeSphericalHarmonicsL1(inout MaskVolumeSphericalHarmonicsL1 coefficients)
{
/*#ifdef DEBUG_DISPLAY
    if (_DebugMaskVolumeMode == MASKVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS || _DebugMaskVolumeMode == MASKVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        // coefficients are storing debug info. Do not swizzle or normalize.
        return;
    }
#endif*/

    // SHEvalLinearL0L1() expects coefficients in real4 shAr, real4 shAg, real4 shAb vectors whos channels are laid out {x, y, z, DC}
    float4 shAr = float4(coefficients.data[0].w, coefficients.data[1].x, coefficients.data[1].y, coefficients.data[0].x);
    float4 shAg = float4(coefficients.data[1].z, coefficients.data[1].w, coefficients.data[2].x, coefficients.data[0].y);
    float4 shAb = float4(coefficients.data[2].y, coefficients.data[2].z, coefficients.data[2].w, coefficients.data[0].z);

    coefficients.data[0] = shAr;
    coefficients.data[1] = shAg;
    coefficients.data[2] = shAb;
}

void MaskVolumeSwizzleAndNormalizeSphericalHarmonicsL2(inout MaskVolumeSphericalHarmonicsL2 coefficients)
{
/*#ifdef DEBUG_DISPLAY
    if (_DebugMaskVolumeMode == MASKVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS || _DebugMaskVolumeMode == MASKVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        // coefficients are storing debug info. Do not swizzle or normalize.
        return;
    }
#endif*/

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