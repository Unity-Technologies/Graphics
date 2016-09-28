#ifndef UNITY_BSDF_INCLUDED
#define UNITY_BSDF_INCLUDED

#include "Common.hlsl"

//-----------------------------------------------------------------------------
// Fresnel term
//-----------------------------------------------------------------------------

float F_Schlick(float f0, float f90, float u)
{
	float x		= 1.0f - u;
	float x5	= x * x;
	x5			= x5 * x5 * x;
    return (f90 - f0) * x5 + f0; // sub mul mul mul sub mad
}

float F_Schlick(float f0, float u)
{
    return F_Schlick(f0, 1.0f, u);
}

float3 F_Schlick(float3 f0, float f90, float u)
{
	float x		= 1.0f - u;
	float x5	= x * x;
	x5			= x5 * x5 * x;
    return (f90 - f0) * x5 + f0; // sub mul mul mul sub mad
}

float3 F_Schlick(float3 f0, float u)
{
    return F_Schlick(f0, float3(1.0f, 1.0f, 1.0f), u);
}

//-----------------------------------------------------------------------------
// Specular BRDF
//-----------------------------------------------------------------------------

// With analytical light (not image based light) we clamp the minimun roughness to avoid numerical instability.
#define UNITY_MIN_ROUGHNESS 0.002

float D_GGX(float NdotH, float roughness)
{
	roughness = max(roughness, UNITY_MIN_ROUGHNESS);
    float a2 = roughness * roughness;
	float f = (NdotH * a2 - NdotH) * NdotH + 1.0f;
    return INV_PI * a2 / (f * f);
}

// roughnessT -> roughness in tangent direction
// roughnessB -> roughness in bitangent direction
float D_GGX_Aniso(float NdotH, float TdotH, float BdotH, float roughnessT, float roughnessB)
{
	// TODO: Do the clamp on the artists parameter
	float f = TdotH * TdotH / (roughnessT * roughnessT) + BdotH * BdotH / (roughnessB * roughnessB) + NdotH * NdotH;
	return INV_PI / (roughnessT * roughnessB * f * f);
}

// Ref: http://jcgt.org/published/0003/02/03/paper.pdf
float V_SmithJointGGX(float NdotL, float NdotV, float roughness)
{
#if 1
	// Original formulation:
	//	lambda_v	= (-1 + sqrt(a2 * (1 - NdotL2) / NdotL2 + 1)) * 0.5f;
	//	lambda_l	= (-1 + sqrt(a2 * (1 - NdotV2) / NdotV2 + 1)) * 0.5f;
	//	G			= 1 / (1 + lambda_v + lambda_l);

	// Reorder code to be more optimal
	half a			= roughness;
	half a2			= a * a;

	half lambdaV	= NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
	half lambdaL	= NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

	// Simplify visibility term: (2.0f * NdotL * NdotV) /  ((4.0f * NdotL * NdotV) * (lambda_v + lambda_l));
	return 0.5f / (lambdaV + lambdaL);
#else
    // Approximation of the above formulation (simplify the sqrt, not mathematically correct but close enough)
	half a = roughness;
	half lambdaV = NdotL * (NdotV * (1 - a) + a);
	half lambdaL = NdotV * (NdotL * (1 - a) + a);

	return 0.5f / (lambdaV + lambdaL);
#endif
}

// TODO: V_ term for aniso GGX from farcry

//-----------------------------------------------------------------------------
// Diffuse BRDF - diffuseColor is expected to be multiply by the caller
//-----------------------------------------------------------------------------

float Lambert()
{
	return INV_PI;
}

float DisneyDiffuse(float NdotV, float NdotL, float LdotH, float perceptualRoughness)
{
	float fd90 = 0.5 + 2 * LdotH * LdotH * perceptualRoughness;
	// Two schlick fresnel term
	float lightScatter = F_Schlick(1.0f, fd90, NdotL);
	float viewScatter = F_Schlick(1.0f, fd90, NdotV);

	return INV_PI * lightScatter * viewScatter;
}

#endif // UNITY_BSDF_INCLUDED
