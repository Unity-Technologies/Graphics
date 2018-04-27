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


#endif
