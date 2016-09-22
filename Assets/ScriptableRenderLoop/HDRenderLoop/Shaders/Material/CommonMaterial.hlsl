#ifndef UNITY_COMMON_MATERIAL_INCLUDED
#define UNITY_COMMON_MATERIAL_INCLUDED

//-----------------------------------------------------------------------------
// Parametrization function helpers
//-----------------------------------------------------------------------------

float PerceptualRoughnessToRoughness(float perceptualRoughness)
{
	return perceptualRoughness * perceptualRoughness;
}

float RoughnessToPerceptualRoughness(float roughness)
{
	return sqrt(roughness);
}

// Smoothness is the user facing name
// it should be perceptualSmoothness but we don't want the user to have to deal with this name
float SmoothnessToRoughness(float smoothness)
{
	return (1 - smoothness) * (1 - smoothness);
}

float SmoothnessToPerceptualRoughness(float smoothness)
{
	return (1 - smoothness);
}

#endif // UNITY_COMMON_MATERIAL_INCLUDED