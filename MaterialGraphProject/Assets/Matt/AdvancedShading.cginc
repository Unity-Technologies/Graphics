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
	half3 vLTLight = light.dir + normal * _TDistortion;
	half  fLTDot = pow(saturate(dot(viewDir, -vLTLight)), _TPower) * _TScale;
	half3 fLT = _TAttenuation * (fLTDot + _TAmbient) * (thickness);
	return diffColor * ((light.color * fLT) * _TransmissionOverallStrength);
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
#if defined(SHADINGMODELID_UNLIT) || defined(SHADINGMODELID_STANDARD) || defined(SHADINGMODELID_SUBSURFACE) || defined(SHADINGMODELID_SKIN) || defined(SHADINGMODELID_FOLIAGE)
	{
		return UnlitShading(diffColor);
	}
#elif defined(SHADINGMODELID_UNLIT) || defined(SHADINGMODELID_STANDARD) || defined(SHADINGMODELID_SUBSURFACE) || defined(SHADINGMODELID_SKIN) || defined(SHADINGMODELID_FOLIAGE)
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

