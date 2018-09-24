//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef VOLUMETRICLIGHTING_CS_HLSL
#define VOLUMETRICLIGHTING_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DensityVolumeData
// PackingRules = Exact
struct DensityVolumeData
{
    float3 scattering;
    float extinction;
    float3 textureTiling;
    int textureIndex;
    float3 textureScroll;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.DensityVolumeData
//
float3 GetScattering(DensityVolumeData value)
{
    return value.scattering;
}
float GetExtinction(DensityVolumeData value)
{
    return value.extinction;
}
float3 GetTextureTiling(DensityVolumeData value)
{
    return value.textureTiling;
}
int GetTextureIndex(DensityVolumeData value)
{
    return value.textureIndex;
}
float3 GetTextureScroll(DensityVolumeData value)
{
    return value.textureScroll;
}


#endif
