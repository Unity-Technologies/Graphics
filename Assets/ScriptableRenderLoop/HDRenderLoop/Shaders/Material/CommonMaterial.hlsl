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

float PerceptualSmoothnessToRoughness(float perceptualSmoothness)
{
	return (1 - perceptualSmoothness) * (1 - perceptualSmoothness);
}

float PerceptualSmoothnessToPerceptualRoughness(float perceptualSmoothness)
{
	return (1 - perceptualSmoothness);
}

#endif // UNITY_COMMON_MATERIAL_INCLUDED