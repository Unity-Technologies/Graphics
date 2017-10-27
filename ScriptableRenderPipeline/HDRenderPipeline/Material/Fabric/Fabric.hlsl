//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// SurfaceData is define in Fabric.cs which generate Fabric.cs.hlsl
#include "Fabric.cs.hlsl"
#include "../Lit/SubsurfaceScatteringSettings.cs.hlsl"

#define WANT_SSS_CODE
#include "../LightEvaluationShare1.hlsl"

void FillMaterialIdStandardData(float3 baseColor, float specular, float metallic, float roughness, float3 normalWS, float3 tangentWS, float anisotropy, inout BSDFData bsdfData)
{
	bsdfData.diffuseColor = baseColor * 0.5;
	bsdfData.fresnel0 = baseColor;

	// TODO: encode specular

	bsdfData.tangentWS = tangentWS;
	bsdfData.bitangentWS = cross(normalWS, tangentWS);
	ConvertAnisotropyToRoughness(roughness, anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);
	bsdfData.anisotropy = 1;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData)
{
    ApplyDebugToSurfaceData(surfaceData);

	BSDFData bsdfData;
	ZERO_INITIALIZE(BSDFData, bsdfData);

	bsdfData.specularOcclusion = surfaceData.specularOcclusion;
	bsdfData.normalWS = surfaceData.normalWS;
	bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
	bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
	bsdfData.materialId = surfaceData.materialId;

	FillMaterialIdStandardData(surfaceData.baseColor, surfaceData.specular, 0, bsdfData.roughness, surfaceData.normalWS, surfaceData.tangentWS, surfaceData.anisotropy, bsdfData);
	bsdfData.materialId = surfaceData.anisotropy > 0.0 ? MATERIALID_LIT_ANISO : bsdfData.materialId;

    FillMaterialIdSSSData(surfaceData.baseColor, surfaceData.subsurfaceProfile, surfaceData.subsurfaceRadius, surfaceData.thickness, bsdfData);

	return bsdfData;
}

float4 EncodeSplitLightingGBuffer0(SurfaceData surfaceData)
{
	return float4(surfaceData.baseColor, 1.0);
}

float4 EncodeSplitLightingGBuffer1(SurfaceData surfaceData)
{
	return float4(surfaceData.subsurfaceRadius, 1.0, 0.0, PackByte(surfaceData.subsurfaceProfile)); //TODO: Fabric UI
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// GetBakedDiffuseLigthing function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, bsdfData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3 GetBakedDiffuseLigthing(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData, PreLightData preLightData)
{
	// Premultiply bake diffuse lighting information with DisneyDiffuse pre-integration
	return builtinData.bakeDiffuseLighting * preLightData.diffuseFGD * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
	LightTransportData lightTransportData;

	// diffuseColor for lightmapping should basically be diffuse color.
	// But rough metals (black diffuse) still scatter quite a lot of light around, so
	// we want to take some of that into account too.

	lightTransportData.diffuseColor = bsdfData.diffuseColor;
	lightTransportData.emissiveColor = builtinData.emissiveColor;

	return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

// Note from Seb: The code was like that but it is in fact inverted... I haven't modify it to not break current data, but better to put it in correct order
#ifdef _FABRIC_SILK
	#define BSDF BSDF_Silk
#else
	#define BSDF BSDF_CottonWool
#endif

void BSDF_CottonWool(float3 V, float3 L, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
	out float3 diffuseLighting,
	out float3 specularLighting)
{

	// Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
	float NdotL = saturate(dot(bsdfData.normalWS, L)); // Must have the same value without the clamp
	float NdotV = preLightData.NdotV;                  // Get the unaltered (geometric) version
	float LdotV = dot(L, V);
	float invLenLV = rsqrt(abs(2 * LdotV + 2));           // invLenLV = rcp(length(L + V))
	float NdotH = saturate((NdotL + NdotV) * invLenLV);
	float LdotH = saturate(invLenLV * LdotV + invLenLV);

	NdotV = max(NdotV, MIN_N_DOT_V);             // Use the modified (clamped) version

	float3 F = F_Schlick(0.2, LdotH);

	float Vis;
	float D;
	// TODO: this way of handling aniso may not be efficient, or maybe with material classification, need to check perf here
	// Maybe always using aniso maybe a win ?
	float3 H = (L + V) * invLenLV;
	bsdfData.roughness = 1- ClampRoughnessForAnalyticalLights(bsdfData.roughness);
	float roughnessSq = bsdfData.roughness*bsdfData.roughness;
	float cnorm = 1.0 / (PI * (4.0 * roughnessSq + 1.0));

	float NdotH2 = NdotH*NdotH;
	float cot2 = NdotH2 / (1.0 - NdotH2);
	float sin2 = 1.0 - NdotH2;
	float sin4 = sin2 * sin2;
	float amp = 4.0;
	D = cnorm * (1.0 + (amp * exp(-cot2 / roughnessSq) / sin4));

	#ifdef LIT_USE_BSDF_PRE_LAMBDAV
		Vis = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughness, preLightData.ggxLambdaV);
	#else
		Vis = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughness);
	#endif

	float NdotLwrap = sqrt(NdotL);

	specularLighting = NdotLwrap*F *(Vis * D)*_FuzzTint;

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    float  diffuseTerm = Lambert();
#elif LIT_DIFFUSE_GGX_BRDF
    float3 diffuseTerm = DiffuseGGX(bsdfData.diffuseColor, NdotV, NdotL, NdotH, LdotV, bsdfData.roughness);
#else
    float  diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotV, bsdfData.perceptualRoughness);
#endif

	diffuseLighting = bsdfData.diffuseColor * diffuseTerm*lerp(1,0.5,bsdfData.roughness);
}

void BSDF_Silk(float3 V, float3 L, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
	out float3 diffuseLighting,
	out float3 specularLighting)
{
	// Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
	float NdotL = saturate(dot(bsdfData.normalWS, L)); // Must have the same value without the clamp
	float NdotV = preLightData.NdotV;                  // Get the unaltered (geometric) version
	float LdotV = dot(L, V);
	float invLenLV = rsqrt(abs(2 * LdotV + 2));           // invLenLV = rcp(length(L + V))
	float NdotH = saturate((NdotL + NdotV) * invLenLV);
	float LdotH = saturate(invLenLV * LdotV + invLenLV);

	NdotV = max(NdotV, MIN_N_DOT_V);             // Use the modified (clamped) version

	float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    float DV;

	if (bsdfData.materialId == MATERIALID_LIT_ANISO)
	{
        float3 H = (L + V) * invLenLV;
        // For anisotropy we must not saturate these values
        float TdotH = dot(bsdfData.tangentWS, H);
        float TdotL = dot(bsdfData.tangentWS, L);
        float BdotH = dot(bsdfData.bitangentWS, H);
        float BdotL = dot(bsdfData.bitangentWS, L);

        bsdfData.roughnessT = ClampRoughnessForAnalyticalLights(bsdfData.roughnessT);
        bsdfData.roughnessB = ClampRoughnessForAnalyticalLights(bsdfData.roughnessB);

        // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
        DV = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH,
                                   preLightData.TdotV, preLightData.BdotV, preLightData.NdotV,
                                   TdotL, BdotL, NdotL,
                                   bsdfData.roughnessT, bsdfData.roughnessB
        #ifdef LIT_USE_BSDF_PRE_LAMBDAV
                                 , preLightData.partLambdaV);
        #else
                                   );
        #endif
	}
	else
	{
        bsdfData.roughness = ClampRoughnessForAnalyticalLights(bsdfData.roughness);

        DV = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughness
        #ifdef LIT_USE_BSDF_PRE_LAMBDAV
                            , preLightData partLambdaV);
        #else
                              );
        #endif
	}
	specularLighting = F * DV * _FuzzTint;

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    float  diffuseTerm = Lambert();
#elif LIT_DIFFUSE_GGX_BRDF
    float3 diffuseTerm = DiffuseGGX(bsdfData.diffuseColor, NdotV, NdotL, NdotH, LdotV, bsdfData.roughness);
#else
    float  diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotV, bsdfData.perceptualRoughness);
#endif

    diffuseLighting = bsdfData.diffuseColor * diffuseTerm;
}

#endif // #ifdef HAS_LIGHTLOOP

#include "../LightEvaluationShare2.hlsl"

