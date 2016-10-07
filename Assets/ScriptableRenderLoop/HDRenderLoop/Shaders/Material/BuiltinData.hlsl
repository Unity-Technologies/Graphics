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

void GetBuiltinDataDebug(uint paramId, BuiltinData builtinData, inout float3 result, inout float outputIsLinear)
{
	if (paramId == MaterialDebugBakeDiffuseLighting)
	{
		// TODO: require a remap
		result = builtinData.bakeDiffuseLighting;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugEmissiveColor)
	{
		result = builtinData.emissiveColor;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugEmissiveIntensity)
	{
		// TODO: require a reamp
		result = builtinData.emissiveIntensity.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugVelocity)
	{
		result = float3(builtinData.velocity, 0.0);
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugDistortion)
	{
		result = float3(builtinData.distortion, 0.0);
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugDistortionBlur)
	{
		result = builtinData.distortionBlur.xxx;
		outputIsLinear = true;
	}
}
#endif // UNITY_BUILTIN_DATA_INCLUDED
