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
