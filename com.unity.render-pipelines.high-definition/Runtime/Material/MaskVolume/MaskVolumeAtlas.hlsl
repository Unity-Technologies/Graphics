#ifndef __MASKVOLUMEATLAS_HLSL__
#define __MASKVOLUMEATLAS_HLSL__

struct MaskVolumeData
{
    float4 data[1];
};

// See MaskVolumeAtlasBlit.compute for atlas coefficient layout information.
void MaskVolumeSampleAccumulate(float3 maskVolumeAtlasUVW, float weight, inout MaskVolumeData coefficients)
{
    coefficients.data[0].xyz += SAMPLE_TEXTURE3D_LOD(_MaskVolumeAtlasSH, s_linear_clamp_sampler, float3(maskVolumeAtlasUVW.x, maskVolumeAtlasUVW.y, maskVolumeAtlasUVW.z + _MaskVolumeAtlasResolutionAndSliceCountInverse.w * 0), 0).xyz * weight;
}

void MaskVolumeLoadAccumulate(int3 maskVolumeAtlasTexelCoord, float weight, inout MaskVolumeData coefficients)
{
    coefficients.data[0].xyz += LOAD_TEXTURE3D_LOD(_MaskVolumeAtlasSH, int3(maskVolumeAtlasTexelCoord.x, maskVolumeAtlasTexelCoord.y, maskVolumeAtlasTexelCoord.z + _MaskVolumeAtlasResolutionAndSliceCount.z * 0), 0).xyz * weight;
}

#endif