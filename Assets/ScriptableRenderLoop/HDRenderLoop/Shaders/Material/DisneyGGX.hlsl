#ifndef UNITY_MATERIAL_DISNEYGGX_INCLUDED
#define UNITY_MATERIAL_DISNEYGGX_INCLUDED

//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// Main structure that store the user data (i.e user input of master node in material graph)
struct SurfaceData
{
	// TODO: define what is the best parametrization for artsits, seems that metal smoothness is the winner, but would like at add back a specular parameter.
	// Bonus, if we store as specular color we can define a liner specular 0..1 mapping 2% to 20%
	float3	diffuseColor;
	float	ambientOcclusion;

	float3	specularColor; // Should be YCbCr but need to validate that it is fine first! MEan have reflection probe
	float	specularOcclusion;

	float3	normalWS;
	float	perceptualSmoothness;
	float	materialId;

	// TODO: create a system surfaceData for thing like that + Transparent
	// As we collect some lighting information (Lightmap, lightprobe/proxy volume)
	// and emissive ahead (i.e in Gbuffer pass when doing deferred), we need to
	// to have them in the SurfaceData structure.
	float3	diffuseLighting;
	float3	emissiveColor; // Linear space
	float	emissiveIntensity;

	// MaterialID SSS - When enable, we only need one channel for specColor, so one is free to store information.
	float	subSurfaceRadius;
//	float	thickness;
//	int		subSurfaceProfile;

	// MaterialID Clear coat
//	float	coatCoverage;
//	float	coatRoughness;

	// Distortion
//	float2	distortionVector;
//	float	distortionBlur;		// Define the mipmap level to use

//	float2	velocityVector;
};

struct BSDFData
{
	float3	diffuseColor;
	float	matData0;

	float3	fresnel0; // Should be YCbCr but need to validate that it is fine first! MEan have reflection probe
	float	specularOcclusion;
	//float	matData1;

	float3	normalWS;
	float	perceptualRoughness;
	float	materialId;

	float	roughness;

	// System
	float3	diffuseLightingAndEmissive;
};

//-----------------------------------------------------------------------------
// conversion function for forward and deferred
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData data)
{
	BSDFData output;

	output.diffuseColor = data.diffuseColor;
	output.matData0 = data.subSurfaceRadius; // TEMP

	output.fresnel0 = data.specularColor;
	output.specularOcclusion = data.specularOcclusion;
	//output.matData1 = data.matData1;

	output.normalWS = data.normalWS;
	output.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(data.perceptualSmoothness);
	output.materialId = data.materialId;

	output.roughness = PerceptualRoughnessToRoughness(output.perceptualRoughness);
	
	output.diffuseLightingAndEmissive = data.diffuseLighting * data.ambientOcclusion * data.diffuseColor + data.emissiveColor * data.emissiveIntensity;

	return output;
}

// Packing function specific to this surfaceData
float PackMaterialId(int materialId)
{
	return float(materialId) / 3.0;
}

int UnpackMaterialId(float f)
{
	return int(round(f * 3.0));
}

#define GBUFFER_COUNT 4

// This will encode UnityStandardData into GBuffer
void EncodeIntoGBuffer(SurfaceData data, out float4 outGBuffer0, out float4 outGBuffer1, out float4 outGBuffer2, out float4 outGBuffer3)
{
	// RT0 - 8:8:8:8 sRGB
	outGBuffer0 = float4(data.diffuseColor, data.subSurfaceRadius);

	// RT1 - 8:8:8:8:
	outGBuffer1 = float4(data.specularColor, data.specularOcclusion /*, data.matData1 */);

	// RT2 - 10:10:10:2
	// Encode normal on 20bit with oct compression
	float2 octNormal = PackNormalOctEncode(data.normalWS);
	// We store perceptualRoughness instead of roughness because it save a sqrt ALU when decoding
	// (as we want both perceptualRoughness and roughness for the lighting due to Disney Diffuse model)
	outGBuffer2 = float4(octNormal * 0.5 + 0.5, PerceptualSmoothnessToPerceptualRoughness(data.perceptualSmoothness), PackMaterialId(data.materialId));

	// RT3 - 11:11:10 float
	outGBuffer3 = float4(data.diffuseLighting * data.ambientOcclusion * data.diffuseColor + data.emissiveColor * data.emissiveIntensity, 0.0f);
}

// This decode the Gbuffer in a BSDFData struct
BSDFData DecodeFromGBuffer(float4 inGBuffer0, float4 inGBuffer1, float4 inGBuffer2, float4 inGBuffer3)
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

	bsdfData.diffuseLightingAndEmissive = inGBuffer3.rgb;

	return bsdfData;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF functions for each light type
//-----------------------------------------------------------------------------

void EvaluateBSDF_Punctual(	float3 V, float3 positionWS, PunctualLightData light, BSDFData bsdfData,
							out float4 diffuseLighting,
							out float4 specularLighting)
{
	// All punctual light type in the same formula, attenuation is neutral depends on light type.
	// light.positionWS is the normalize light direction in case of directional light and invSqrAttenuationRadius is 0
	// mean dot(unL, unL) = 1 and mean GetDistanceAttenuation() will return 1
	// For point light and directional GetAngleAttenuation() return 1

	float3 unL = light.positionWS - positionWS * light.useDistanceAttenuation;
	float3 L = normalize(unL);

	float attenuation = GetDistanceAttenuation(unL, light.invSqrAttenuationRadius);
	attenuation *= GetAngleAttenuation(L, light.forward, light.angleScale, light.angleOffset);
	float illuminance = saturate(dot(bsdfData.normalWS, L)) * attenuation;

	diffuseLighting = float4(0.0f, 0.0f, 0.0f, 1.0f);
	specularLighting = float4(0.0f, 0.0f, 0.0f, 1.0f);

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
		float disneyDiffuse = DisneyDiffuse(NdotV, NdotL, LdotH, bsdfData.perceptualRoughness);
		diffuseLighting.rgb = bsdfData.diffuseColor * disneyDiffuse;

		diffuseLighting.rgb *= light.color * illuminance;
		specularLighting.rgb *= light.color * illuminance;
	}
}

#endif // UNITY_MATERIAL_DISNEYGGX_INCLUDED