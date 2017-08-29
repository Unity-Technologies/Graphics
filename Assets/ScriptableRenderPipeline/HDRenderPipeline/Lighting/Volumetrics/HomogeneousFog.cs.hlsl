//
// This file was automatically generated from Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/Volumetrics/HomogeneousFog.cs.  Please don't edit by hand.
//

#ifndef HOMOGENEOUSFOG_CS_HLSL
#define HOMOGENEOUSFOG_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.VolumeProperties
// PackingRules = Exact
struct VolumeProperties
{
    float3 extinction;
    float asymmetry;
    float3 scattering;
    float align16;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.VolumeProperties
//
float3 GetExtinction(VolumeProperties value)
{
	return value.extinction;
}
float GetAsymmetry(VolumeProperties value)
{
	return value.asymmetry;
}
float3 GetScattering(VolumeProperties value)
{
	return value.scattering;
}
float GetAlign16(VolumeProperties value)
{
	return value.align16;
}


#endif
