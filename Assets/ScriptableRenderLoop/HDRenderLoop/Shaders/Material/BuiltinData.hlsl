#ifndef UNITY_BUILTIN_DATA_INCLUDED
#define UNITY_BUILTIN_DATA_INCLUDED

//-----------------------------------------------------------------------------
// BuiltinData
// This structure include common data that should be present in all material
// and are independent from the BSDF parametrization.
// Note: These parameters can be store in GBuffer if the writer wants
//-----------------------------------------------------------------------------

struct BuiltinData
{
	float	opacity;

	// These are lighting data.
	// We would prefer to split lighting and material information but for performance reasons, 
	// those lighting information are fill 
	// at the same time than material information.
	float3	bakeDiffuseLighting;	// This is the result of sampling lightmap/lightprobe/proxyvolume

	float3	emissiveColor;
	float	emissiveIntensity;

	// These is required for motion blur and temporalAA
	float2	velocity;

	// Distortion
	float2	distortion;
	float	distortionBlur;			// Define the color buffer mipmap level to use
};

#endif // UNITY_BUILTIN_DATA_INCLUDED