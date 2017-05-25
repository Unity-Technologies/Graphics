Shader "Brandon/Electricity"
{
	Properties
	{

	}

SubShader
{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry"
		}

		Blend One Zero

		Cull Back

		ZTest LEqual

		ZWrite On


	LOD 200

	CGPROGRAM
	#include "UnityCG.cginc"
	//#include "AdvancedBRDF.cginc"
	//#include "AdvancedShading.cginc"
	//#include "AdvancedLighting.cginc"

	#define SHADINGMODELID_UNLIT


// ------------------------------------------------------------------
//  Diffuse

// From UE4 - Used for Cloth (Deprecated)
float3 Diffuse_Lambert(float3 DiffuseColor)
{
	return DiffuseColor * (1 / UNITY_PI);
}

// ------------------------------------------------------------------
//  Fresnel

// From UE4 - Used for Cloth
// [Schlick 1994, "An Inexpensive BRDF Model for Physically-Based Rendering"]
float3 F_Schlick(float3 SpecularColor, float VoH)
{
	float Fc = Pow5(1 - VoH);					// 1 sub, 3 mul
												//return Fc + (1 - Fc) * SpecularColor;		// 1 add, 3 mad
												// Anything less than 2% is physically impossible and is instead considered to be shadowing
	return saturate(50.0 * SpecularColor.g) * Fc + (1 - Fc) * SpecularColor;
}

// ------------------------------------------------------------------
//  Distribution

// From UE4 - USed for Cloth
// GGX / Trowbridge-Reitz
// [Walter et al. 2007, "Microfacet models for refraction through rough surfaces"]
float D_GGX(float roughness, float NdotH)
{
	float a = roughness * roughness;
	float a2 = a * a;
	float d = (NdotH * a2 - NdotH) * NdotH + 1;	// 2 mad
	return a2 / (UNITY_PI*d*d);					// 4 mul, 1 rcp
}

// Anisotropic GGX
// Taken from HDRenderPipeline
float D_GGXAnisotropic(float TdotH, float BdotH, float NdotH, float roughnessT, float roughnessB)
{
	float f = TdotH * TdotH / (roughnessT * roughnessT) + BdotH * BdotH / (roughnessB * roughnessB) + NdotH * NdotH;
	return 1.0 / (roughnessT * roughnessB * f * f);
}

// From UE4 - Used for Cloth
float D_InvGGX(float roughness, float NdotH)
{
	float a = roughness * roughness;
	float a2 = a * a;
	float A = 4;
	float d = (NdotH - a2 * NdotH) * NdotH + a2;
	return 1/(UNITY_PI * (1 + A*a2)) * (1 + 4 * a2*a2 / (d*d)); //RCP
}

// ------------------------------------------------------------------
//  Visibility

// From UE4 - Used for Cloth
// Appoximation of joint Smith term for GGX
// [Heitz 2014, "Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs"]
float Vis_SmithJointApprox(float Roughness, float NoV, float NoL)
{
	float a = (Roughness*Roughness);
	float Vis_SmithV = NoL * (NoV * (1 - a) + a);
	float Vis_SmithL = NoV * (NoL * (1 - a) + a);
	// Note: will generate NaNs with Roughness = 0.  MinRoughness is used to prevent this
	return 0.5 * 1/(Vis_SmithV + Vis_SmithL); //RCP
}

// From UE4 - Used for Cloth
float Vis_Cloth(float NoV, float NoL)
{
	return 1/(4 * (NoL + NoV - NoL * NoV)); //RCP
}

// ------------------------------------------------------------------
//  SORT THESE

// Smith Joint GGX Anisotropic Visibility
// Taken from https://cedec.cesa.or.jp/2015/session/ENG/14698.html
float SmithJointGGXAnisotropic(float TdotV, float BdotV, float NdotV, float TdotL, float BdotL, float NdotL, float roughnessT, float roughnessB)
{
	float aT = roughnessT;
	float aT2 = aT * aT;
	float aB = roughnessB;
	float aB2 = aB * aB;

	float lambdaV = NdotL * sqrt(aT2 * TdotV * TdotV + aB2 * BdotV * BdotV + NdotV * NdotV);
	float lambdaL = NdotV * sqrt(aT2 * TdotL * TdotL + aB2 * BdotL * BdotL + NdotL * NdotL);

	return 0.5 / (lambdaV + lambdaL);
}

// Convert Anistropy to roughness
void ConvertAnisotropyToRoughness(float roughness, float anisotropy, out float roughnessT, out float roughnessB)
{
	// (0 <= anisotropy <= 1), therefore (0 <= anisoAspect <= 1)
	// The 0.9 factor limits the aspect ratio to 10:1.
	float anisoAspect = sqrt(1.0 - 0.9 * anisotropy);
	roughnessT = roughness / anisoAspect; // Distort along tangent (rougher)
	roughnessB = roughness * anisoAspect; // Straighten along bitangent (smoother)
}

// Schlick Fresnel
float FresnelSchlick(float f0, float f90, float u)
{
	float x = 1.0 - u;
	float x5 = x * x;
	x5 = x5 * x5 * x;
	return (f90 - f0) * x5 + f0; // sub mul mul mul sub mad
}

//Clamp roughness
float ClampRoughnessForAnalyticalLights(float roughness)
{
	return max(roughness, 0.000001);
}

//Calculate tangent warp for IBL (Reference Version - not used)
float3 SpecularGGXIBLRef(float3 viewDir, float3 normalDir, float3 tangentDir, float3 bitangentDir, float roughnessT, float roughnessB)
{
	return float3(1, 1, 1);
	//Hidden in UnityAnisotropicLighting.cginc
}

// Sample Anisotropic Direction for IBL (Reference Version - not used)
void SampleAnisoGGXDir(float2 u, float3 viewDir, float3 normalDir, float3 tangent, float3 bitangent, float roughnessT, float roughnessB, out float3 halfDir, out float3 lightDir)
{
	// AnisoGGX NDF sampling
	halfDir = sqrt(u.x / (1.0 - u.x)) * (roughnessT * cos((UNITY_PI * 2) * u.y) * tangent + roughnessB * sin((UNITY_PI * 2) * u.y) * bitangent) + normalDir;
	halfDir = normalize(halfDir);

	// Convert sample from half angle to incident angle
	lightDir = 2.0 * saturate(dot(viewDir, halfDir)) * halfDir - viewDir;
}

// Ref: Donald Revie - Implementing Fur Using Deferred Shading (GPU Pro 2)
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to the normal.
// The returned normal is NOT normalized.
float3 ComputeGrainNormal(float3 grainDir, float3 V)
{
	float3 B = cross(-V, grainDir);
	return cross(B, grainDir);
}

//Modify Normal for Anisotropic IBL (Realtime version)
// Fake anisotropic by distorting the normal.
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to N.
// Anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction)
float3 GetAnisotropicModifiedNormal(float3 grainDir, float3 N, float3 V, float anisotropy)
{
	float3 grainNormal = ComputeGrainNormal(grainDir, V);
	// TODO: test whether normalizing 'grainNormal' is worth it.
	return normalize(lerp(N, grainNormal, anisotropy));
}

/// REGION END - ANISOTROPY

/// REGION START - SUBSURFACE SCATTERING

half Fresnel(half3 H, half3 V, half F0)
{
	half base = 1.0 - dot(V, H);
	half exponential = pow(base, 5.0);
	return exponential + F0 * (1.0 - exponential);
}
/*
inline half3 KelemenSzirmayKalosSpecular(half3 normal, half3 lightDir, half3 viewDir, float roughness, float rho_s)
{
	half3 result = half3(0, 0, 0);
	half NdotL = dot(normal, lightDir);
	if (NdotL > 0.0)
	{
		half3 h = lightDir + viewDir;
		half3 H = normalize(h);
		half NdotH = dot(normal, H);
		half PH = pow(2.0 * tex2D(_BeckmannPrecomputedTex, half2(NdotH, roughness)).r, 10.0);
		half F = Fresnel(H, viewDir, 0.028);
		half frSpec = max(PH * F / dot(h, h), 0);
		half term = NdotL * rho_s * frSpec;
		result = half3(term, term, term);
	}
	return result;
}*/
/*
half3 SkinDiffuse(float curv, float3 NdotL)
{
	float3 lookup = NdotL * 0.5 + 0.5;
	float3 diffuse;

	diffuse.r = tex2D(_DiffusionProfileTexture, float2(lookup.r, curv)).r;
	diffuse.g = tex2D(_DiffusionProfileTexture, float2(lookup.g, curv)).g;
	diffuse.b = tex2D(_DiffusionProfileTexture, float2(lookup.b, curv)).b;

	return diffuse;
}*/

/// REGION END - SUBSURFACE SCATTERING

// Upgrade NOTE: replaced 'defined SHADINGMODELID_CLEARCOAT' with 'defined (SHADINGMODELID_CLEARCOAT)'
// Upgrade NOTE: replaced 'defined SHADINGMODELID_CLOTH' with 'defined (SHADINGMODELID_CLOTH)'
// Upgrade NOTE: replaced 'defined SHADINGMODELID_EYE' with 'defined (SHADINGMODELID_EYE)'
// Upgrade NOTE: replaced 'defined SHADINGMODELID_FOLIAGE' with 'defined (SHADINGMODELID_FOLIAGE)'
// Upgrade NOTE: replaced 'defined SHADINGMODELID_HAIR' with 'defined (SHADINGMODELID_HAIR)'
// Upgrade NOTE: replaced 'defined SHADINGMODELID_SKIN' with 'defined (SHADINGMODELID_SKIN)'
// Upgrade NOTE: replaced 'defined SHADINGMODELID_SUBSURFACE' with 'defined (SHADINGMODELID_SUBSURFACE)'

// ------------------------------------------------------------------
// Shading models

//#pragma multi_compile SHADINGMODELID_UNLIT SHADINGMODELID_STANDARD SHADINGMODELID_SUBSURFACE SHADINGMODELID_SKIN SHADINGMODELID_FOLIAGE SHADINGMODELID_CLEARCOAT SHADINGMODELID_CLOTH SHADINGMODELID_EYE

// ------------------------------------------------------------------
//  Input

half		_ShadingModel;

sampler2D	_AnisotropyMap;
half		_Anisotropy;
sampler2D	_TangentMap;

half4 		_TranslucentColor;
sampler2D	_TranslucencyMap;

sampler2D	_FuzzTex;
half3		_FuzzColor;
half		_Cloth;

sampler2D	_IrisNormal;
sampler2D	_IrisMask;
half		_IrisDistance;

half _TDistortion;
half _TScale;
half _TAmbient;
half _TPower;
half _TAttenuation;
half _TransmissionOverallStrength;

// ------------------------------------------------------------------
//  Maths helpers

// Octahedron Normal Vectors
// [Cigolle 2014, "A Survey of Efficient Representations for Independent Unit Vectors"]
//						Mean	Max
// oct		8:8			0.33709 0.94424
// snorm	8:8:8		0.17015 0.38588
// oct		10:10		0.08380 0.23467
// snorm	10:10:10	0.04228 0.09598
// oct		12:12		0.02091 0.05874

float2 UnitVectorToOctahedron(float3 N)
{
	N.xy /= dot(float3(1,1,1), abs(N));
	if (N.z <= 0)
	{
		N.xy = (1 - abs(N.yx)) * (N.xy >= 0 ? float2(1, 1) : float2(-1, -1));
	}
	return N.xy;
}

float3 OctahedronToUnitVector(float2 Oct)
{
	float3 N = float3(Oct, 1 - dot(float2(1,1), abs(Oct)));
	if (N.z < 0)
	{
		N.xy = (1 - abs(N.yx)) * (N.xy >= 0 ? float2(1, 1) : float2(-1, -1));
	}
	return float3(1, 1, 1);
	return normalize(N);
}

// ------------------------------------------------------------------
//  Surface helpers

half Anisotropy(float2 uv)
{
	return tex2D(_AnisotropyMap, uv) * _Anisotropy;
}

half3 Fuzz(float2 uv)
{
	return tex2D(_FuzzTex, uv) * _FuzzColor;
}

half Cloth()
{
	return _Cloth;
}

half4 Iris(float2 uv)
{
	float2 n = UnitVectorToOctahedron(normalize(UnpackNormal(tex2D(_IrisNormal, uv)).rgb)) * 0.5 + 0.5;
	float m = saturate(tex2D(_IrisMask, uv).r);	// Iris Mask
	float d = saturate(_IrisDistance);				// Iris Distance
	return float4(n.x, n.y, m, d);
}

half3 Translucency(float2 uv)
{
	return tex2D(_TranslucencyMap, uv).rgb * _TranslucentColor.rgb;
}

// ------------------------------------------------------------------
//  Unlit Shading Function

float4 UnlitShading(float3 diffColor)
{
	return half4(diffColor, 1);
}

// ------------------------------------------------------------------
//  Standard Shading Function

float4 StandardShading(float3 diffColor, float3 specColor, float oneMinusReflectivity, float smoothness, float3 normal, float3x3 worldVectors,
	float anisotropy, float metallic, float3 viewDir, UnityLight light, UnityIndirect gi)
{
	//Unpack world vectors
	float3 tangent = worldVectors[0];
	float3 bitangent = worldVectors[1];
	//Normal shift
	float shiftAmount = dot(normal, viewDir);
	normal = shiftAmount < 0.0f ? normal + viewDir * (-shiftAmount + 1e-5f) : normal;
	//Regular vectors
	float NdotL = saturate(dot(normal, light.dir)); //sat?
	float NdotV = abs(dot(normal, viewDir)); //abs?
	float LdotV = dot(light.dir, viewDir);
	float3 H = Unity_SafeNormalize(light.dir + viewDir);
	float invLenLV = rsqrt(abs(2 + 2 * normalize(LdotV)));
	//float invLenLV = rsqrt(abs(2 + 2 * LdotV));
	//float NdotH = (NdotL + normalize(NdotV)) * invLenLV;
	float NdotH = saturate(dot(normal, H));
	//float NdotH = saturate((NdotL + normalize(NdotV)) * invLenLV);
	//float H = (light.dir + viewDir) * invLenLV;
	float LdotH = saturate(dot(light.dir, H));
	//Tangent vectors
	float TdotH = dot(tangent, H);
	float TdotL = dot(tangent, light.dir);
	float BdotH = dot(bitangent, H);
	float BdotL = dot(bitangent, light.dir);
	float TdotV = dot(viewDir, tangent);
	float BdotV = dot(viewDir, bitangent);
	//Fresnels
	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
	float3 F = FresnelLerp(specColor, grazingTerm, NdotV); //Original Schlick - Replace from SRP?
														   //float3 fresnel0 = lerp(specColor, diffColor, metallic);
														   //float3 F = FresnelSchlick(fresnel0, 1.0, LdotH);
														   //Calculate roughness
	float roughnessT;
	float roughnessB;
	float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	ConvertAnisotropyToRoughness(roughness, anisotropy, roughnessT, roughnessB);
	//Clamp roughness
	//roughness = ClampRoughnessForAnalyticalLights(roughness);
	roughnessT = ClampRoughnessForAnalyticalLights(roughnessT);
	roughnessB = ClampRoughnessForAnalyticalLights(roughnessB);
	//Visibility & Distribution terms
	float V = SmithJointGGXAnisotropic(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB);
	float D = D_GGXAnisotropic(TdotH, BdotH, NdotH, roughnessT, roughnessB);
	//Specular term
	float3 specularTerm = V * D; //*UNITY_PI;
#	ifdef UNITY_COLORSPACE_GAMMA
	specularTerm = sqrt(max(1e-4h, specularTerm));
#	endif
	// specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
	specularTerm = max(0, specularTerm * NdotL);
#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm = 0.0;
#endif
	//Diffuse term
	float diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotH, perceptualRoughness) * NdotL;// - Need this NdotL multiply?
																						//Reduction
	half surfaceReduction;
#	ifdef UNITY_COLORSPACE_GAMMA
	surfaceReduction = 1.0 - 0.28*roughness*perceptualRoughness;		// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#	else
	surfaceReduction = 1.0 / (roughness*roughness + 1.0);			// fade \in [0.5;1]
#	endif
																	//Final
	half3 color = (diffColor * (gi.diffuse + light.color * diffuseTerm))
		+ specularTerm * light.color * FresnelTerm(specColor, LdotH)
		+ surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, NdotV);
	return half4(color, 1);
}

// ------------------------------------------------------------------
//  Cloth Shading Function

//float3 ClothShading(FGBufferData GBuffer, float3 LobeRoughness, float3 LobeEnergy, float3 L, float3 V, half3 N)
float4 ClothShading(float3 diffColor, float3 specColor, float3 fuzzColor, float cloth, float oneMinusReflectivity, float smoothness, float3 normal, float3 viewDir, UnityLight light, UnityIndirect gi, float3x3 worldVectors, float anisotropy)
{
	const float3 FuzzColor = saturate(fuzzColor);
	const float  Cloth = saturate(cloth);

	//Regular vectors
	float NdotL = saturate(dot(normal, light.dir)); //sat?
	float NdotV = abs(dot(normal, viewDir)); //abs?
	float LdotV = dot(light.dir, viewDir);
	//float invLenLV = rsqrt(abs(2 + 2 * normalize(LdotV)));
	////float invLenLV = rsqrt(abs(2 + 2 * LdotV));
	//float NdotH = (NdotL + normalize(NdotV)) * invLenLV;
	//float NdotH = saturate((NdotL + normalize(NdotV)) * invLenLV);
	float3 H = Unity_SafeNormalize(light.dir + viewDir);
	//float H = (light.dir + viewDir) * invLenLV;
	float LdotH = saturate(dot(light.dir, H));

	//float3 H = normalize(viewDir + light.dir);
	//float NdotL = saturate(dot(normal, light.dir));
	//float NdotV = saturate(abs(dot(normal, viewDir)) + 1e-5);
	float NdotH = saturate(dot(normal, H));
	float VdotH = saturate(dot(viewDir, H));
	//float LdotH = saturate(dot(light.dir, H));

	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));

	// Diffuse
	float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	float diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotH, perceptualRoughness) * NdotL;// - Need this NdotL multiply?

																						// Cloth - Asperity Scattering - Inverse Beckmann Layer
	float3 F1 = FresnelTerm(fuzzColor, LdotH);// FresnelLerp(fuzzColor, grazingTerm, NdotV);// FresnelTerm(FuzzColor, LdotH);// F_Schlick(FuzzColor, VdotH);
	float  D1 = D_InvGGX(roughness, NdotH);
	float  V1 = Vis_Cloth(NdotV, NdotL);
	//Specular term
	float3 specularTerm1 = V1 * D1; //*UNITY_PI;
#	ifdef UNITY_COLORSPACE_GAMMA
	specularTerm1 = sqrt(max(1e-4h, specularTerm1));
#	endif
	// specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
	// specularTerm1 = max(0, specularTerm1 * NdotL);
#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm1 = 0.0;
#endif
	float3 Spec1 = specularTerm1 * light.color * FresnelTerm(fuzzColor, LdotH);

	// Generalized microfacet specular
	/*float3 F2 = F_Schlick(specColor, VdotH);
	float  D2 = D_GGX(roughness, NdotH);
	float  V2 = Vis_SmithJointApprox(roughness, NdotV, NdotL);
	float3 Spec2 = D2 * V2 * F2 * light.color;*/

	//Unpack world vectors
	float3 tangent = worldVectors[0];
	float3 bitangent = worldVectors[1];
	//Tangent vectors
	float TdotH = dot(tangent, H);
	float TdotL = dot(tangent, light.dir);
	float BdotH = dot(bitangent, H);
	float BdotL = dot(bitangent, light.dir);
	float TdotV = dot(viewDir, tangent);
	float BdotV = dot(viewDir, bitangent);
	//Fresnels
	float3 F2 = FresnelLerp(specColor, grazingTerm, NdotV);// FresnelTerm(specColor, LdotH);// FresnelLerp(specColor, grazingTerm, NdotV); //Original Schlick - Replace from SRP?
	float roughnessT;
	float roughnessB;
	//float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	//float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	ConvertAnisotropyToRoughness(roughness, anisotropy, roughnessT, roughnessB);
	//Clamp roughness
	//roughness = ClampRoughnessForAnalyticalLights(roughness);
	roughnessT = ClampRoughnessForAnalyticalLights(roughnessT);
	roughnessB = ClampRoughnessForAnalyticalLights(roughnessB);
	//Visibility & Distribution terms
	float V2 = SmithJointGGXAnisotropic(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB);
	float D2 = D_GGXAnisotropic(TdotH, BdotH, NdotH, roughnessT, roughnessB);
	//Specular term
	float3 specularTerm2 = V2 * D2; //*UNITY_PI;
#	ifdef UNITY_COLORSPACE_GAMMA
	specularTerm2 = sqrt(max(1e-4h, specularTerm2));
#	endif
	// specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
	specularTerm2 = max(0, specularTerm2 * NdotL);
#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm2 = 0.0;
#endif
	float3 Spec2 = specularTerm2 * light.color * FresnelTerm(specColor, LdotH);

	float3 Spec = lerp(Spec2, Spec1, Cloth);

	//Reduction
	half surfaceReduction;
#	ifdef UNITY_COLORSPACE_GAMMA
	surfaceReduction = 1.0 - 0.28*roughness*perceptualRoughness;		// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#	else
	surfaceReduction = 1.0 / (roughness*roughness + 1.0);			// fade \in [0.5;1]
#	endif
																	//Final
																	//half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
	half3 color = (diffColor * (gi.diffuse + light.color * diffuseTerm))
		+ Spec
		+ surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, NdotV);
	return half4(color, 1);
}

// ------------------------------------------------------------------
//  Eye Shading Function

//float3 EyeShading(FGBufferData GBuffer, float3 LobeRoughness, float3 LobeEnergy, float3 L, float3 V, half3 N)
float4 EyeShading(float3 diffColor, float3 specColor, float3 viewDir, half3 normal, float smoothness, float oneMinusReflectivity, UnityLight light, UnityIndirect gi)
{
	float3 H = normalize(viewDir + light.dir);
	float NdotL = saturate(dot(normal, light.dir));
	float NdotV = saturate(abs(dot(normal, viewDir)) + 1e-5);
	float NdotH = saturate(dot(normal, H));
	float VdotH = saturate(dot(viewDir, H));
	float LdotH = saturate(dot(light.dir, H));

	// Generalized microfacet specular
	float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
	float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

	float D = D_GGX(roughness, NdotH);// *LobeEnergy[1];
	float V = Vis_SmithJointApprox(roughness, NdotV, NdotL);
	float3 F = F_Schlick(specColor, VdotH);

	float3 specularTerm = V * D; //*UNITY_PI;
#	ifdef UNITY_COLORSPACE_GAMMA
	specularTerm = sqrt(max(1e-4h, specularTerm));
#	endif
	// specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
	specularTerm = max(0, specularTerm * NdotL);
#if defined(_SPECULARHIGHLIGHTS_OFF)
	specularTerm = 0.0;
#endif
	half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
	half surfaceReduction;
#	ifdef UNITY_COLORSPACE_GAMMA
	surfaceReduction = 1.0 - 0.28*roughness*perceptualRoughness;		// 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#	else
	surfaceReduction = 1.0 / (roughness*roughness + 1.0);			// fade \in [0.5;1]
#	endif

	float diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotH, perceptualRoughness) * NdotL; // TODO - Unreal does not apply diffuse in Shading function
																						 //Final
	half3 color = (diffColor * (gi.diffuse + light.color * diffuseTerm))
		+ specularTerm * light.color * FresnelTerm(specColor, LdotH)
		+ surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, NdotV);
	return half4(color, 1);
}

// ------------------------------------------------------------------
//  Subsurface Shading Function

float3 SubsurfaceShadingSimple(float3 diffColor, float3 normal, float3 viewDir, float3 thickness, UnityLight light)
{
	half3 vLTLight = light.dir + normal * 1;
	half  fLTDot = pow(saturate(dot(viewDir, -vLTLight)), 3.5) * 1.5;
	half3 fLT = 1 * (fLTDot + 1.2) * (thickness);
	return diffColor * ((light.color * fLT) * 0.4);
}

// ------------------------------------------------------------------
//  Eye Subsurface Shading Function

//float3 EyeSubsurfaceShading(FGBufferData GBuffer, float3 L, float3 V, half3 N)
float3 EyeSubsurfaceShading(float3 diffColor, float3 specColor, float3 viewDir, half3 normal, float smoothness, float4 iris, UnityLight light)
{
	float2 irisNormal = iris.rg;
	float irisMask = iris.z;
	float irisDistance = iris.w;

	float3 H = normalize(viewDir + light.dir);
	float VdotH = saturate(dot(viewDir, H));
	float NdotV = saturate(abs(dot(normal, viewDir)) + 1e-5);
	float LdotH = saturate(dot(light.dir, H));

	// F_Schlick
	//float F0 = GBuffer.Specular * 0.08;
	//float Fc = Pow5(1 - VoH);
	//float F = Fc + (1 - Fc) * F0;
	float3 fresnel0 = lerp(specColor, diffColor, smoothness);
	float3 F = FresnelSchlick(fresnel0, 1.0, LdotH);

	//float  IrisDistance = GBuffer.CustomData.w;
	//float  IrisMask = GBuffer.CustomData.z;

	float3 IrisNormal;
	IrisNormal = OctahedronToUnitVector(irisNormal * 2 - 1);

	// Blend in the negative intersection normal to create some concavity
	// Not great as it ties the concavity to the convexity of the cornea surface
	// No good justification for that. On the other hand, if we're just looking to
	// introduce some concavity, this does the job.
	float3 CausticNormal = normalize(lerp(IrisNormal, -normal, irisMask*irisDistance));

	float NdotL = saturate(dot(IrisNormal, light.dir));
	float Power = lerp(12, 1, NdotL);
	float Caustic = 0.6 + 0.2 * (Power + 1) * pow(saturate(dot(CausticNormal, light.dir)), Power);
	float Iris = NdotL * Caustic;

	// http://blog.stevemcauley.com/2011/12/03/energy-conserving-wrapped-diffuse/
	float Wrap = 0.15;
	float Sclera = saturate((dot(normal, light.dir) + Wrap) / (1 + Wrap) * (1 + Wrap));

	return (1 - F) * lerp(Sclera, Iris, irisMask) * diffColor / UNITY_PI;
}

// ------------------------------------------------------------------
//  Shading function selectors

//float3 SurfaceShading(/*FGBufferData GBuffer,*/ float3 LobeRoughness, float3 LobeEnergy, float3 L, float3 V, half3 N, uint2 Random)
float4 SurfaceShading(float3 diffColor, float3 specColor, float oneMinusReflectivity, float smoothness, float3 normal,
	float3x3 worldVectors, float anisotropy, float4 customData, float metallic, float3 viewDir, UnityLight light, UnityIndirect gi)
{
#if defined(SHADINGMODELID_UNLIT)
	{
		return UnlitShading(diffColor);
	}
#elif defined(SHADINGMODELID_STANDARD) || defined(SHADINGMODELID_SUBSURFACE) || defined(SHADINGMODELID_SKIN) || defined(SHADINGMODELID_FOLIAGE)
	{
		return StandardShading(diffColor, specColor, oneMinusReflectivity, smoothness,
			normal, worldVectors, anisotropy, metallic, viewDir, light, gi);
	}
#elif defined (SHADINGMODELID_CLEARCOAT)
	{
		return float4(1, 1, 1, 1); //ClearCoatShading(GBuffer, LobeRoughness, LobeEnergy, L, V, N);
	}
#elif defined (SHADINGMODELID_CLOTH)
	{
		return ClothShading(diffColor, specColor, customData.rgb, customData.a, oneMinusReflectivity, smoothness, normal, viewDir, light, gi, worldVectors, anisotropy);
	}
#elif defined (SHADINGMODELID_EYE)
	{
		return EyeShading(diffColor, specColor, viewDir, normal, smoothness, oneMinusReflectivity, light, gi); //EyeShading(GBuffer, LobeRoughness, LobeEnergy, L, V, N);
	}
#endif
	return float4(0, 0, 0, 0);
}

//float3 SubsurfaceShading(/*FGBufferData GBuffer,*/ float3 L, float3 V, half3 N, float Shadow, uint2 Random)
float3 SubsurfaceShading(float3 diffColor, float3 specColor, float3 normal, float smoothness, float3 viewDir, float4 customData, UnityLight light)
{
#if defined (SHADINGMODELID_SUBSURFACE)
	{
		return SubsurfaceShadingSimple(diffColor, normal, viewDir, customData.rgb, light);
	}
#elif defined (SHADINGMODELID_SKIN)
	{
		return float3(0, 0, 0); //SubsurfaceShadingPreintegratedSkin(GBuffer, L, V, N);
	}
#elif defined (SHADINGMODELID_FOLIAGE)
	{
		return float3(0, 0, 0); //SubsurfaceShadingTwoSided(SubsurfaceColor, L, V, N);
	}
#elif defined (SHADINGMODELID_HAIR)
	{
		return float3(0, 0, 0); //HairShading(GBuffer, L, V, N, Shadow, 1, 0, Random);
	}
#elif defined (SHADINGMODELID_EYE)
	{
		return EyeSubsurfaceShading(diffColor, specColor, viewDir, normal, smoothness, customData, light); //EyeSubsurfaceShading(GBuffer, L, V, N);
	}
#endif
	return float3(0, 0, 0);
}

//#endif UNITY_ADVANCED_SHADINGMODELS_INCLUDED

//-------------------------------------------------------------------------------------
// Lighting Helpers

// Glossy Environment
half3 Unity_AnisotropicGlossyEnvironment(UNITY_ARGS_TEXCUBE(tex), half4 hdr, Unity_GlossyEnvironmentData glossIn, half anisotropy) //Reference IBL from HD Pipe (Add half3 L input and replace R)
{
	half perceptualRoughness = glossIn.roughness /* perceptualRoughness */;

	// TODO: CAUTION: remap from Morten may work only with offline convolution, see impact with runtime convolution!
	// For now disabled
#if 0
	float m = PerceptualRoughnessToRoughness(perceptualRoughness); // m is the real roughness parameter
	const float fEps = 1.192092896e-07F;        // smallest such that 1.0+FLT_EPSILON != 1.0  (+1e-4h is NOT good here. is visibly very wrong)
	float n = (2.0 / max(fEps, m*m)) - 2.0;        // remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf

	n /= 4;                                     // remap from n_dot_h formulatino to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html

	perceptualRoughness = pow(2 / (n + 2), 0.25);      // remap back to square root of real roughness (0.25 include both the sqrt root of the conversion and sqrt for going from roughness to perceptualRoughness)
#else
	// MM: came up with a surprisingly close approximation to what the #if 0'ed out code above does.
	perceptualRoughness = perceptualRoughness*(1.7 - 0.7*perceptualRoughness);
#endif


	half mip = perceptualRoughnessToMipmapLevel(perceptualRoughness);
	half3 R = glossIn.reflUVW;// -half3(anisotropy, 0, 0);
	half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(tex, R, mip);

	return DecodeHDR(rgbm, hdr);
}

// Indirect Specular
inline half3 UnityGI_AnisotropicIndirectSpecular(UnityGIInput data, half occlusion, Unity_GlossyEnvironmentData glossIn, half anisotropy, half3x3 worldVectors)
{
	half3 specular;
	float3 tangentX = worldVectors[0];
	float3 tangentY = worldVectors[1];
	float3 N = worldVectors[2];
	float3 V = data.worldViewDir;
	float3 iblNormalWS = GetAnisotropicModifiedNormal(tangentY, N, V, anisotropy);
	float3 iblR = reflect(-V, iblNormalWS);

#ifdef UNITY_SPECCUBE_BOX_PROJECTION
	// we will tweak reflUVW in glossIn directly (as we pass it to Unity_GlossyEnvironment twice for probe0 and probe1), so keep original to pass into BoxProjectedCubemapDirection

	half3 originalReflUVW = glossIn.reflUVW;
	glossIn.reflUVW = BoxProjectedCubemapDirection(iblR, data.worldPos, data.probePosition[0], data.boxMin[0], data.boxMax[0]);
#endif

#ifdef _GLOSSYREFLECTIONS_OFF
	specular = unity_IndirectSpecColor.rgb;
#else
	half3 env0 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], glossIn, anisotropy);
	//half3 env0 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], glossIn, anisotropy, L); //Reference IBL from HD Pipe
#ifdef UNITY_SPECCUBE_BLENDING
	const float kBlendFactor = 0.99999;
	float blendLerp = data.boxMin[0].w;
	UNITY_BRANCH
		if (blendLerp < kBlendFactor)
		{
#ifdef UNITY_SPECCUBE_BOX_PROJECTION
			glossIn.reflUVW = BoxProjectedCubemapDirection(iblR, data.worldPos, data.probePosition[1], data.boxMin[1], data.boxMax[1]);
#endif
			half3 env1 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), data.probeHDR[1], glossIn, anisotropy);
			//half3 env1 = Unity_AnisotropicGlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), data.probeHDR[1], glossIn, anisotropy, L); //Reference IBL from HD Pipe
			specular = lerp(env1, env0, blendLerp);
		}
		else
		{
			specular = env0;
		}
#else
	specular = env0;
#endif
#endif

	return specular * occlusion;// *weightOverPdf; //Reference IBL from HD Pipe
								//return specular * occlusion * weightOverPdf; //Reference IBL from HD Pipe
}

// Global Illumination
inline UnityGI UnityAnisotropicGlobalIllumination(UnityGIInput data, half occlusion, half3 normalWorld, Unity_GlossyEnvironmentData glossIn, half anisotropy, half3x3 worldVectors)
{
	UnityGI o_gi = UnityGI_Base(data, occlusion, normalWorld);
	o_gi.indirect.specular = UnityGI_AnisotropicIndirectSpecular(data, occlusion, glossIn, anisotropy, worldVectors);
	return o_gi;
}

//-------------------------------------------------------------------------------------
// Lighting Functions

//Surface Description
struct SurfaceOutputAdvanced
{
	fixed3 Albedo;		// base (diffuse or specular) color
	fixed3 Normal;		// tangent space normal, if written
	half3 Emission;
	half Metallic;		// 0=non-metal, 1=metal
						// Smoothness is the user facing name, it should be perceptual smoothness but user should not have to deal with it.
						// Everywhere in the code you meet smoothness it is perceptual smoothness
	half Smoothness;	// 0=rough, 1=smooth
	half Occlusion;		// occlusion (default 1)
	fixed Alpha;		// alpha for transparencies
	half3 Tangent;
	half Anisotropy;
	half4 CustomData;
	float3x3 WorldVectors;
	//half ShadingModel;
};

inline half4 LightingAdvanced(SurfaceOutputAdvanced s, half3 viewDir, UnityGI gi)
{
	s.Normal = normalize(s.Normal);

	half oneMinusReflectivity;
	half3 specColor;
	s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	// this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
	half outputAlpha;
	s.Albedo = PreMultiplyAlpha(s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

	half4 c = SurfaceShading(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, s.WorldVectors, s.Anisotropy, s.CustomData, s.Metallic, viewDir, gi.light, gi.indirect);
	c.rgb += SubsurfaceShading(s.Albedo, specColor, s.Normal, s.Smoothness, viewDir, s.CustomData, gi.light);

	//c.rgb += UNITY_BRDF_GI(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, s.Occlusion, gi);
	c.a = outputAlpha;
	return c;
}

//This is pointless as always forward?
inline half4 LightingAdvanced_Deferred(SurfaceOutputAdvanced s, half3 viewDir, UnityGI gi, out half4 outGBuffer0, out half4 outGBuffer1, out half4 outGBuffer2)
{
	half oneMinusReflectivity;
	half3 specColor;
	s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	half4 c = SurfaceShading(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, s.WorldVectors, s.Anisotropy, s.CustomData, s.Metallic, viewDir, gi.light, gi.indirect);
	c.rgb += SubsurfaceShading(s.Albedo, specColor, s.Normal, s.Smoothness, viewDir, s.CustomData, gi.light);

	UnityStandardData data;
	data.diffuseColor = s.Albedo;
	data.occlusion = s.Occlusion;
	data.specularColor = specColor;
	data.smoothness = s.Smoothness;
	data.normalWorld = s.Normal;

	UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

	half4 emission = half4(s.Emission + c.rgb, 1);
	return emission;
}

inline void LightingAdvanced_GI(SurfaceOutputAdvanced s, UnityGIInput data, inout UnityGI gi)
{
#if defined(UNITY_PASS_DEFERRED) && UNITY_ENABLE_REFLECTION_BUFFERS
	gi = UnityGlobalIllumination(data, s.Occlusion, s.Normal);
#else
	Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.Smoothness, data.worldViewDir, s.Normal, lerp(unity_ColorSpaceDielectricSpec.rgb, s.Albedo, s.Metallic));
	gi = UnityAnisotropicGlobalIllumination(data, s.Occlusion, s.Normal, g, s.Anisotropy, s.WorldVectors);
#endif
}


	///END


	//#pragma target 5.0
	#pragma surface surf Advanced vertex:vert
	#pragma glsl
	#pragma debug

		inline float4 unity_uvrotator_float (float4 arg1, float arg2)
		{
			arg1.xy -= 0.5;
			float s = sin(arg2);
			float c = cos(arg2);
			float2x2 rMatrix = float2x2(c, -s, s, c);
			rMatrix *= 0.5;
			rMatrix += 0.5;
			rMatrix = rMatrix*2 - 1;
			arg1.xy = mul(arg1.xy, rMatrix);
			arg1.xy += 0.5;
			return arg1;
		}
		inline float4 unity_remap_float (float4 arg1, float2 arg2, float2 arg3)
		{
			return arg3.x + (arg1 - arg2.x) * (arg3.y - arg3.x) / (arg2.y - arg2.x);
		}
		inline float unity_noise_randomValue (float2 uv)
		{
			return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453);
		}
		inline float unity_noise_interpolate (float a, float b, float t)
		{
			return (1.0-t)*a + (t*b);
		}
		inline float unity_valueNoise (float2 uv)
		{
			float2 i = floor(uv);
			float2 f = frac(uv);
			f = f * f * (3.0 - 2.0 * f);
			uv = abs(frac(uv) - 0.5);
			float2 c0 = i + float2(0.0, 0.0);
			float2 c1 = i + float2(1.0, 0.0);
			float2 c2 = i + float2(0.0, 1.0);
			float2 c3 = i + float2(1.0, 1.0);
			float r0 = unity_noise_randomValue(c0);
			float r1 = unity_noise_randomValue(c1);
			float r2 = unity_noise_randomValue(c2);
			float r3 = unity_noise_randomValue(c3);
			float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
			float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
			float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
			return t;
		}
		inline float unity_noise_float (float2 uv)
		{
			float t = 0.0;
			for(int i = 0; i < 3; i++)
			{
				float freq = pow(2.0, float(i));
				float amp = pow(0.5, float(3-i));
				t += unity_valueNoise(float2(uv.x/freq, uv.y/freq))*amp;
			}
			return t;
		}
		inline float unity_add_float (float arg1, float arg2)
		{
			return arg1 + arg2;
		}
		inline float unity_multiply_float (float arg1, float arg2)
		{
			return arg1 * arg2;
		}
		inline float unity_linenode_float (float2 uv, float2 a, float2 b)
		{
			float2 aTob = b - a;
			float2 aTop = uv - a;
			float t = dot(aTop, aTob) / dot(aTob, aTob);
			t = clamp(t, 0.0, 1.0);
			float d = 1.0 / length(uv - (a + aTob * t));
			return clamp(d, 0.0, 1.0);
		}
		inline float4 unity_multiply_float (float4 arg1, float4 arg2)
		{
			return arg1 * arg2;
		}
		inline float2 unity_uvpanner_float (float2 UV, float HorizontalOffset, float VerticalOffset)
		{
			return float2(UV.x + HorizontalOffset, UV.y + VerticalOffset);
		}
		inline float4 unity_add_float (float4 arg1, float4 arg2)
		{
			return arg1 + arg2;
		}
		inline float3 unity_contrast_float (float3 arg1, float arg2, float arg3)
		{
			return (arg1 - arg3) * arg2 + arg3;
		}
		inline float3 unity_rgbtolinear_float (float3 arg1)
		{
			float3 linearRGBLo = arg1 / 12.92;
			float3 linearRGBHi = pow(max(abs((arg1 + 0.055) / 1.055), 1.192092896e-07), float3(2.4, 2.4, 2.4));
			return float3(arg1 <= 0.04045) ? linearRGBLo : linearRGBHi;
		}
		inline float3 unity_multiply_float (float3 arg1, float3 arg2)
		{
			return arg1 * arg2;
		}



	struct Input
	{
			float4 color : COLOR;
			half4 meshUV0;
			float4 worldTangent;
			float3 worldNormal;

	};

	void vert (inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input,o);
			o.meshUV0 = v.texcoord;
			o.worldTangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	}

	void surf (Input IN, inout SurfaceOutputAdvanced o)
	{
			half4 uv0 = IN.meshUV0;
			float3 worldSpaceTangent = normalize(IN.worldTangent.xyz);
			float3 worldSpaceNormal = normalize(IN.worldNormal);
			float3 worldSpaceBitangent = cross(worldSpaceNormal, worldSpaceTangent) * IN.worldTangent.w;
			float4 Vector4_a3c7f2ae_8f7c_46e8_b635_951db6f16381_Uniform = float4 (2, 4, 8, 0);
			float4 UV_387647e5_6f77_4a7b_8008_3c32ecb1453f_UV = uv0;
			float4 Split_6b549b87_2eac_4af6_ab86_3017e402a94e = float4(UV_387647e5_6f77_4a7b_8008_3c32ecb1453f_UV);
			float4 UVRotator_15c2f67b_99b2_4fdd_be0a_938dedf88cdc_Output = unity_uvrotator_float (uv0, _Time.z);
			float4 Remap_2928c5f7_c4d5_45c0_90fe_75dbd0a2a6c4_Output = unity_remap_float (UVRotator_15c2f67b_99b2_4fdd_be0a_938dedf88cdc_Output, float2 (0,1), float2 (2,20));
			float Noise_8fcea665_4e1e_4a8e_8c2b_1fc6a2edf6a1_Output = unity_noise_float (Remap_2928c5f7_c4d5_45c0_90fe_75dbd0a2a6c4_Output);
			float Add_ddda4931_27aa_4fcf_915e_f770d42073e6_Output = unity_add_float (Split_6b549b87_2eac_4af6_ab86_3017e402a94e.g, _Time.y);
			float Add_ca58a1fe_6b0c_46ba_a476_a24b77e0e730_Output = unity_add_float (Noise_8fcea665_4e1e_4a8e_8c2b_1fc6a2edf6a1_Output, Add_ddda4931_27aa_4fcf_915e_f770d42073e6_Output);
			float Multiply_4ef4be75_4e02_4615_bbf1_ce5b48ac5555_Output = unity_multiply_float (Add_ca58a1fe_6b0c_46ba_a476_a24b77e0e730_Output, 3);
			float Sin_922b3b70_1ea5_425f_86a4_56b044d27fa8_Output = sin (Multiply_4ef4be75_4e02_4615_bbf1_ce5b48ac5555_Output);
			float Multiply_54844c22_2bc3_4718_b622_0366b51b96d1_Output = unity_multiply_float (Sin_922b3b70_1ea5_425f_86a4_56b044d27fa8_Output, 0.2);
			float Add_185f7dff_95e8_4f5f_a8bf_27c691c7f253_Output = unity_add_float (Split_6b549b87_2eac_4af6_ab86_3017e402a94e.r, Multiply_54844c22_2bc3_4718_b622_0366b51b96d1_Output);
			float4 Combine_16bde180_37d2_452c_b723_26163ad12bc1_Output = float4(Add_185f7dff_95e8_4f5f_a8bf_27c691c7f253_Output,0.0, 0.0, 0.0);
			float4 Remap_3c0ddc39_a3a7_4433_857c_3250dd30c5e5_Output = unity_remap_float (Combine_16bde180_37d2_452c_b723_26163ad12bc1_Output, float2 (0,1), float2 (-30,30));
			float Line_2433c58e_3fa0_4089_96b2_3e25461fcbcd_Output = unity_linenode_float (Remap_3c0ddc39_a3a7_4433_857c_3250dd30c5e5_Output, float2 (0,100), float2 (0,-100));
			float SmoothStep_d0588f6b_821f_4f55_a0fe_9186d12be4bd_Output = smoothstep (-0.05, 2, Line_2433c58e_3fa0_4089_96b2_3e25461fcbcd_Output);
			float4 Multiply_fbe1fcb2_7d7c_462a_95a2_742b54fd2a63_Output = unity_multiply_float (Vector4_a3c7f2ae_8f7c_46e8_b635_951db6f16381_Uniform, SmoothStep_d0588f6b_821f_4f55_a0fe_9186d12be4bd_Output);
			float4 Vector4_b9304822_6fc5_40f1_ba86_42708db45a78_Uniform = float4 (8, 4, 2, 0);
			float Add_892e57df_d662_42ca_9dda_060b39b252bb_Output = unity_add_float (Split_6b549b87_2eac_4af6_ab86_3017e402a94e.g, Multiply_54844c22_2bc3_4718_b622_0366b51b96d1_Output);
			float4 Combine_5eb28541_3afc_4a24_bb30_f90ccc744363_Output = float4(Split_6b549b87_2eac_4af6_ab86_3017e402a94e.g,Add_892e57df_d662_42ca_9dda_060b39b252bb_Output,0.0, 0.0);
			float4 Remap_947534db_4810_48fb_a867_18b95154f841_Output = unity_remap_float (Combine_5eb28541_3afc_4a24_bb30_f90ccc744363_Output, float2 (0,1), float2 (-9.78,12.02));
			float2 UVPanner_ebbe6cbf_3de6_4b24_bf81_04d15b812156_Output = unity_uvpanner_float (Remap_947534db_4810_48fb_a867_18b95154f841_Output, 0, 12.5);
			float Line_f9a561bc_7d0e_4481_bc3b_d342ed5be518_Output = unity_linenode_float (UVPanner_ebbe6cbf_3de6_4b24_bf81_04d15b812156_Output, float2 (-100,0), float2 (100,0));
			float SmoothStep_f126a46d_d897_4335_b2e5_49cc714919fe_Output = smoothstep (0.01, 1, Line_f9a561bc_7d0e_4481_bc3b_d342ed5be518_Output);
			float4 Multiply_eab282fa_ad21_436c_9bb6_9c38e2bc7cf9_Output = unity_multiply_float (Vector4_b9304822_6fc5_40f1_ba86_42708db45a78_Uniform, SmoothStep_f126a46d_d897_4335_b2e5_49cc714919fe_Output);
			float4 Add_f14dec2c_0ef2_40de_bb1f_c284dceb844b_Output = unity_add_float (Multiply_fbe1fcb2_7d7c_462a_95a2_742b54fd2a63_Output, Multiply_eab282fa_ad21_436c_9bb6_9c38e2bc7cf9_Output);
			float4 Vector4_35f9492c_bcf2_4ef7_ad4c_0bbe15408bc2_Uniform = float4 (2, 4, 8, 0);
			float Multiply_259baa1d_31c5_4aff_85ec_7d79476cde10_Output = unity_multiply_float (Add_ca58a1fe_6b0c_46ba_a476_a24b77e0e730_Output, 3);
			float Cos_c6a82522_1a99_4663_bfc0_8b811e9f7378_Output = cos (Multiply_259baa1d_31c5_4aff_85ec_7d79476cde10_Output);
			float Multiply_90f1d419_aba0_4a37_9950_226f3fd5bc20_Output = unity_multiply_float (Cos_c6a82522_1a99_4663_bfc0_8b811e9f7378_Output, 0.1);
			float Add_77d513d4_b57e_4cbf_9350_90be33e32635_Output = unity_add_float (Split_6b549b87_2eac_4af6_ab86_3017e402a94e.r, Multiply_90f1d419_aba0_4a37_9950_226f3fd5bc20_Output);
			float4 Combine_654339c0_1833_4634_a080_ff73d27007e5_Output = float4(Add_77d513d4_b57e_4cbf_9350_90be33e32635_Output,0.0, 0.0, 0.0);
			float4 Remap_7b259185_ee2d_4655_b91a_6b275982da85_Output = unity_remap_float (Combine_654339c0_1833_4634_a080_ff73d27007e5_Output, float2 (0,1), float2 (-30,30));
			float Line_ac3825f2_fbeb_4ccb_b70d_290d9376e336_Output = unity_linenode_float (Remap_7b259185_ee2d_4655_b91a_6b275982da85_Output, float2 (0,100), float2 (0,-100));
			float SmoothStep_d04f6b50_d4bd_4a5d_a6ca_f7c36edc40cc_Output = smoothstep (0.06, 2.06, Line_ac3825f2_fbeb_4ccb_b70d_290d9376e336_Output);
			float4 Multiply_868ddd52_8fbe_459d_ac84_02675612a436_Output = unity_multiply_float (Vector4_35f9492c_bcf2_4ef7_ad4c_0bbe15408bc2_Uniform, SmoothStep_d04f6b50_d4bd_4a5d_a6ca_f7c36edc40cc_Output);
			float4 Add_d8ec0770_c4d5_40ea_963a_da9075e65d9a_Output = unity_add_float (Add_f14dec2c_0ef2_40de_bb1f_c284dceb844b_Output, Multiply_868ddd52_8fbe_459d_ac84_02675612a436_Output);
			float4 Split_222221b8_9d57_4ca5_b259_9645755ec3c9 = float4(Add_d8ec0770_c4d5_40ea_963a_da9075e65d9a_Output);
			float Vector1_68b22f57_e72d_47be_8bc4_415f1abb9c35_Uniform = 0;
			float4 Combine_d1f9e19f_d857_49f7_9890_a67aabe95135_Output = float4(Split_222221b8_9d57_4ca5_b259_9645755ec3c9.r,Split_222221b8_9d57_4ca5_b259_9645755ec3c9.g,Split_222221b8_9d57_4ca5_b259_9645755ec3c9.b,Vector1_68b22f57_e72d_47be_8bc4_415f1abb9c35_Uniform);
			float3 Contrast_7228c80f_bebc_4ffc_b19d_d0c76bfc141a_Output = unity_contrast_float (Combine_d1f9e19f_d857_49f7_9890_a67aabe95135_Output, 0.7, 0);
			float3 RGBtoLinear_eec2671b_8e7e_4acd_860c_77b7e77264d1_Output = unity_rgbtolinear_float (Contrast_7228c80f_bebc_4ffc_b19d_d0c76bfc141a_Output);
			float3 Multiply_ddefb6af_f566_4442_ac93_327dbc7fbf71_Output = unity_multiply_float (RGBtoLinear_eec2671b_8e7e_4acd_860c_77b7e77264d1_Output, float3 (10,10,10));
			o.Emission = Multiply_ddefb6af_f566_4442_ac93_327dbc7fbf71_Output;
			o.Alpha = Vector1_68b22f57_e72d_47be_8bc4_415f1abb9c35_Uniform;

	}
	ENDCG
}


	FallBack "Diffuse"
	CustomEditor "LegacyIlluminShaderGUI"
}
