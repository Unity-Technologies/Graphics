#ifndef UNITY_MATERIAL_DISNEYGGX_INCLUDED
#define UNITY_MATERIAL_DISNEYGGX_INCLUDED

//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------


// Main structure that store the user data (i.e user input of master node in material graph)
struct SurfaceData
{
	float3	diffuseColor;
	float	ambientOcclusion;

	float3	specularColor;
	float	specularOcclusion;

	float3	normalWS;
	float	perceptualSmoothness;
	float	materialId;

	// MaterialID SSS - When enable, we only need one channel for specColor, so one is free to store information.
	float	subSurfaceRadius;
//	float	thickness;
//	int		subSurfaceProfile;

	// MaterialID Clear coat
//	float	coatCoverage;
//	float	coatRoughness;	
};

struct BSDFData
{
	float3	diffuseColor;
	float	matData0;

	float3	fresnel0;
	float	specularOcclusion;
	//float	matData1;

	float3	normalWS;
	float	perceptualRoughness;
	float	materialId;

	float	roughness;
};

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData input)
{
	BSDFData output;

	output.diffuseColor = input.diffuseColor;
	output.matData0 = input.subSurfaceRadius; // TEMP

	output.fresnel0 = input.specularColor;
	output.specularOcclusion = input.specularOcclusion;
	//output.matData1 = input.matData1;

	output.normalWS = input.normalWS;
	output.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(input.perceptualSmoothness);
	output.materialId = input.materialId;

	output.roughness = PerceptualRoughnessToRoughness(output.perceptualRoughness);
	
	return output;
}

//-----------------------------------------------------------------------------
// Packing helper functions specific to this surfaceData
//-----------------------------------------------------------------------------

float PackMaterialId(int materialId)
{
	return float(materialId) / 3.0;
}

int UnpackMaterialId(float f)
{
	return int(round(f * 3.0));
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

float3 GetBakedDiffuseLigthing(SurfaceData surfaceData, BuiltinData builtinData)
{
	return builtinData.bakeDiffuseLighting * surfaceData.ambientOcclusion * surfaceData.diffuseColor + builtinData.emissiveColor * builtinData.emissiveIntensity;
}

//-----------------------------------------------------------------------------
// conversion function for deferred
//-----------------------------------------------------------------------------

#define GBUFFER_MATERIAL_COUNT 3

// Encode SurfaceData (BSDF parameters) into GBuffer
void EncodeIntoGBuffer(	SurfaceData surfaceData,
						out float4 outGBuffer0, 
						out float4 outGBuffer1, 
						out float4 outGBuffer2)
{
	// RT0 - 8:8:8:8 sRGB
	outGBuffer0 = float4(surfaceData.diffuseColor, surfaceData.subSurfaceRadius);

	// RT1 - 8:8:8:8:
	outGBuffer1 = float4(surfaceData.specularColor, surfaceData.specularOcclusion /*, surfaceData.matData1 */);

	// RT2 - 10:10:10:2
	// Encode normal on 20bit with oct compression
	float2 octNormal = PackNormalOctEncode(surfaceData.normalWS);
	// We store perceptualRoughness instead of roughness because it save a sqrt ALU when decoding
	// (as we want both perceptualRoughness and roughness for the lighting due to Disney Diffuse model)
	outGBuffer2 = float4(octNormal * 0.5 + 0.5, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness), PackMaterialId(surfaceData.materialId));
}

BSDFData DecodeFromGBuffer(	float4 inGBuffer0, 
							float4 inGBuffer1, 
							float4 inGBuffer2)
{
	BSDFData bsdfData;
	bsdfData.diffuseColor = inGBuffer0.rgb;
	bsdfData.matData0 = inGBuffer0.a;

	bsdfData.fresnel0 = inGBuffer1.rgb;
	bsdfData.specularOcclusion = inGBuffer1.a;
	// bsdfData.matData1 = ?;

	bsdfData.normalWS = UnpackNormalOctEncode(float2(inGBuffer2.r * 2.0 - 1.0, inGBuffer2.g * 2 - 1));
	bsdfData.perceptualRoughness = inGBuffer2.b;
	bsdfData.materialId = UnpackMaterialId(inGBuffer2.a);

	bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);

	return bsdfData;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF functions for each light type
//-----------------------------------------------------------------------------

void EvaluateBSDF_Punctual(	float3 V, float3 positionWS, PunctualLightData lightData, BSDFData bsdfData,
							out float4 diffuseLighting,
							out float4 specularLighting)
{
	// All punctual light type in the same formula, attenuation is neutral depends on light type.
	// light.positionWS is the normalize light direction in case of directional light and invSqrAttenuationRadius is 0
	// mean dot(unL, unL) = 1 and mean GetDistanceAttenuation() will return 1
	// For point light and directional GetAngleAttenuation() return 1

	float3 unL = lightData.positionWS - positionWS * lightData.useDistanceAttenuation;
	float3 L = normalize(unL);

	float attenuation = GetDistanceAttenuation(unL, lightData.invSqrAttenuationRadius);
	attenuation *= GetAngleAttenuation(L, lightData.forward, lightData.angleScale, lightData.angleOffset);
	float illuminance = saturate(dot(bsdfData.normalWS, L)) * attenuation;

	diffuseLighting = float4(0.0, 0.0, 0.0, 1.0);
	specularLighting = float4(0.0, 0.0, 0.0, 1.0);

	if (illuminance > 0.0f)
	{
		float NdotV = abs(dot(bsdfData.normalWS, V)) + 1e-5f; // TODO: check Eric idea about doing that when writting into the GBuffer (with our forward decal)
		float3 H = normalize(V + L);
		float LdotH = saturate(dot(L, H));
		float NdotH = saturate(dot(bsdfData.normalWS, H));
		float NdotL = saturate(dot(bsdfData.normalWS, L));
		float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
		float Vis = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughness);
		float D = D_GGX(NdotH, bsdfData.roughness);
		specularLighting.rgb = F * Vis * D;
		#ifdef DIFFUSE_LAMBERT_BRDF
		float diffuseTerm = Lambert();
		#else
		float diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotH, bsdfData.perceptualRoughness);
		#endif
		diffuseLighting.rgb = bsdfData.diffuseColor * diffuseTerm;

		diffuseLighting.rgb *= lightData.color * illuminance;
		specularLighting.rgb *= lightData.color * illuminance;
	}
}

#endif // UNITY_MATERIAL_DISNEYGGX_INCLUDED
