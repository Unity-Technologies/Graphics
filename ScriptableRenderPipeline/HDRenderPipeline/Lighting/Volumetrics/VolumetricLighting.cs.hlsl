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
    float asymmetry;
    float align16_0;
    float align16_1;
    float align16_2;
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
float GetAsymmetry(VolumeProperties value)
{
	return value.asymmetry;
}
float GetAlign16_0(VolumeProperties value)
{
	return value.align16_0;
}
float GetAlign16_1(VolumeProperties value)
{
	return value.align16_1;
}
float GetAlign16_2(VolumeProperties value)
{
	return value.align16_2;
}


#endif
