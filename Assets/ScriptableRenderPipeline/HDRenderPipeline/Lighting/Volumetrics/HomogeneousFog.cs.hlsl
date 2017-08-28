//
// This file was automatically generated from Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/Volumetrics/HomogeneousFog.cs.  Please don't edit by hand.
//

#ifndef HOMOGENEOUSFOG_CS_HLSL
#define HOMOGENEOUSFOG_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.VolumeProperties
// PackingRules = Exact
struct VolumeProperties
{
    float3 scattering;
    float asymmetry;
    float3 extinction;
    float unused;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.VolumeProperties
//
float3 GetScattering(VolumeProperties value)
{
	return value.scattering;
}
float GetAsymmetry(VolumeProperties value)
{
	return value.asymmetry;
}
float3 GetExtinction(VolumeProperties value)
{
	return value.extinction;
}
float GetUnused(VolumeProperties value)
{
	return value.unused;
}


#endif
