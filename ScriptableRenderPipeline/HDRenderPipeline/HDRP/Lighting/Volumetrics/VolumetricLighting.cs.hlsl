//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef VOLUMETRICLIGHTING_CS_HLSL
#define VOLUMETRICLIGHTING_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DensityVolumeProperties
// PackingRules = Exact
struct DensityVolumeProperties
{
    float3 scattering;
    float extinction;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.DensityVolumeProperties
//
float3 GetScattering(DensityVolumeProperties value)
{
	return value.scattering;
}
float GetExtinction(DensityVolumeProperties value)
{
	return value.extinction;
}


#endif
