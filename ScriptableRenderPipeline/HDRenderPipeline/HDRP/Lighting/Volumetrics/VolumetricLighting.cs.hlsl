//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef VOLUMETRICLIGHTING_CS_HLSL
#define VOLUMETRICLIGHTING_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.VolumeProperties
// PackingRules = Exact
struct VolumeProperties
{
    float3 scattering;
    float extinction;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.VolumeProperties
//
float3 GetScattering(VolumeProperties value)
{
	return value.scattering;
}
float GetExtinction(VolumeProperties value)
{
	return value.extinction;
}


#endif
