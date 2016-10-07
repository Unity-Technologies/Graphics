#ifndef UNITY_MATERIAL_LIT_INCLUDED
#define UNITY_MATERIAL_LIT_INCLUDED

//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

#define LIT_STANDARD 0
#define LIT_SSS 1
#define LIT_CLEARCOAT 2
#define LIT_SPECULAR 3

// Main structure that store the user data (i.e user input of master node in material graph)
struct SurfaceData
{
    float3	baseColor;
    float	specularOcclusion;

    float3	normalWS;
    float	perceptualSmoothness;
    float	materialId;

    float	ambientOcclusion;

    // MaterialId dependent attribute

    // standard
    float3  tangentWS;
    float   anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction)
    float	metalic;
    float	specular; // 0.02, 0.04, 0.16, 0.2

    // SSS
    float	subSurfaceRadius;
    float	thickness;
    int		subSurfaceProfile;

    // Clearcoat
    float3  coatNormalWS;
    float   coatPerceptualSmoothness;

    // SpecColor
    float3  specularColor;
};

struct BSDFData
{
    float3	diffuseColor;

    float3  fresnel0;
    
    float	specularOcclusion;

    float3	normalWS;
    float	perceptualRoughness;
    float	roughness;
    float	materialId;

    // MaterialId dependent attribute

    // standard
    float3  tangentWS;
    float3  bitangentWS;
    float	roughnessT;
    float	roughnessB;

    // fold into fresnel0

    // SSS
    float	subSurfaceRadius;
    float	thickness;
    int		subSurfaceProfile;

    // Clearcoat
    float3  coatNormalWS;
    float   coatRoughness;

    // SpecColor
    // fold into fresnel0
};

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData)
{
    BSDFData bsdfData;

    bsdfData.specularOcclusion = surfaceData.specularOcclusion;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    bsdfData.materialId = surfaceData.materialId;

    if (bsdfData.materialId == LIT_STANDARD)
    {        
        bsdfData.diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metalic);
        bsdfData.fresnel0 = lerp(float3(surfaceData.specular, surfaceData.specular, surfaceData.specular), surfaceData.baseColor, surfaceData.metalic);

        bsdfData.tangentWS = surfaceData.tangentWS;
        bsdfData.bitangentWS = cross(surfaceData.normalWS, surfaceData.tangentWS);
        ConvertAnisotropyToRoughness(bsdfData.roughness, surfaceData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);
    }
    else if (bsdfData.materialId == LIT_SSS)
    {
        bsdfData.diffuseColor = surfaceData.baseColor;
        bsdfData.fresnel0 = 0.028; // TODO take from subSurfaceProfile
        bsdfData.subSurfaceRadius = surfaceData.subSurfaceRadius;
        bsdfData.thickness = surfaceData.thickness;        
    }
    else if (bsdfData.materialId == LIT_CLEARCOAT)
    {
        bsdfData.diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metalic);
        bsdfData.fresnel0 = lerp(float3(surfaceData.specular, surfaceData.specular, surfaceData.specular), surfaceData.baseColor, surfaceData.metalic);
        bsdfData.coatNormalWS = surfaceData.coatNormalWS;
        bsdfData.coatRoughness = PerceptualSmoothnessToRoughness(surfaceData.coatPerceptualSmoothness);
    }
    else if (bsdfData.materialId == LIT_SPECULAR)
    {
        bsdfData.diffuseColor = surfaceData.baseColor;
        bsdfData.fresnel0 = surfaceData.specularColor;
    }
 
    return bsdfData;
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
    float3 diffuseColor;

    if (surfaceData.materialId == LIT_STANDARD)
    {
        diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metalic);
     }
    else if (surfaceData.materialId == LIT_SSS)
    {
        diffuseColor = surfaceData.baseColor;
    }
    else if (surfaceData.materialId == LIT_CLEARCOAT)
    {
        diffuseColor = surfaceData.baseColor * (1.0 - surfaceData.metalic);
    }
    else if (surfaceData.materialId == LIT_SPECULAR)
    {
        diffuseColor = surfaceData.baseColor;
    }

    return builtinData.bakeDiffuseLighting * surfaceData.ambientOcclusion * diffuseColor + builtinData.emissiveColor * builtinData.emissiveIntensity;
}

//-----------------------------------------------------------------------------
// conversion function for deferred
//-----------------------------------------------------------------------------

#define GBUFFER_MATERIAL_COUNT 3

// Encode SurfaceData (BSDF parameters) into GBuffer
// Must be in sync with RT declared in HDRenderLoop.cs ::Rebuild
void EncodeIntoGBuffer(	SurfaceData surfaceData,
                        out float4 outGBuffer0, 
                        out float4 outGBuffer1, 
                        out float4 outGBuffer2)
{
    // RT0 - 8:8:8:8 sRGB
    outGBuffer0 = float4(surfaceData.baseColor, surfaceData.specularOcclusion);

    // RT1 - 10:10:10:2
    // Encode normal on 20bit with oct compression
    float2 octNormalWS = PackNormalOctEncode(surfaceData.normalWS);
    // We store perceptualRoughness instead of roughness because it save a sqrt ALU when decoding
    // (as we want both perceptualRoughness and roughness for the lighting due to Disney Diffuse model)
    // TODO: Store 2 bit of flag into perceptualSmoothness (one for SSR, other is free (deferred planar reflection ID ? / MatID extension ?)
    outGBuffer1 = float4(octNormalWS * 0.5 + 0.5, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness), PackMaterialId(surfaceData.materialId));

    // RT2 - 8:8:8:8
    if (surfaceData.materialId == LIT_STANDARD)
    {
        // Encode tangent on 16bit with oct compression
        float2 octTangentWS = PackNormalOctEncode(surfaceData.tangentWS);
        // TODO: store metal and specular together, specular should be an enum (fixed value)
        outGBuffer2 = float4(octTangentWS * 0.5 + 0.5, surfaceData.anisotropy, surfaceData.metalic);
    }
    else if (surfaceData.materialId == LIT_SSS)
    {
        outGBuffer2 = float4(surfaceData.subSurfaceRadius, surfaceData.thickness, 0.0, 0.0);
    }
    else if (surfaceData.materialId == LIT_CLEARCOAT)
    {
        // Encode coat normal on 16bit with oct compression
        float2 octCoatNormalWS = PackNormalOctEncode(surfaceData.coatNormalWS);
        // TODO: store metal and specular together, specular should be an enum (fixed value)
        outGBuffer2 = float4(octCoatNormalWS * 0.5 + 0.5, PerceptualSmoothnessToRoughness(surfaceData.coatPerceptualSmoothness), surfaceData.metalic);
    }
    else if (surfaceData.materialId == LIT_SPECULAR)
    {
        outGBuffer2 = float4(surfaceData.specularColor, 0.0);
    }
}

BSDFData DecodeFromGBuffer(	float4 inGBuffer0, 
                            float4 inGBuffer1, 
                            float4 inGBuffer2)
{
    BSDFData bsdfData;
   
    float3 baseColor = inGBuffer0.rgb;
    bsdfData.specularOcclusion = inGBuffer0.a;

    bsdfData.normalWS = UnpackNormalOctEncode(float2(inGBuffer1.r * 2.0 - 1.0, inGBuffer1.g * 2.0 - 1.0));
    bsdfData.perceptualRoughness = inGBuffer1.b;    
    bsdfData.roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    bsdfData.materialId = UnpackMaterialId(inGBuffer1.a);

    if (bsdfData.materialId == LIT_STANDARD)
    {
        float metalic = inGBuffer2.a;
        // TODO extract spec
        float specular = 0.04;
        float anisotropy = inGBuffer2.b;

        bsdfData.diffuseColor = baseColor * (1.0 - metalic);
        bsdfData.fresnel0 = lerp(float3(specular, specular, specular), baseColor, metalic);

        bsdfData.tangentWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
        bsdfData.bitangentWS = cross(bsdfData.normalWS, bsdfData.tangentWS);
        ConvertAnisotropyToRoughness(bsdfData.roughness, anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);
    }
    else if (bsdfData.materialId == LIT_SSS)
    {
        bsdfData.diffuseColor = baseColor;
        bsdfData.fresnel0 = 0.028; // TODO take from subSurfaceProfile
        bsdfData.subSurfaceRadius = inGBuffer2.r;
        bsdfData.thickness = inGBuffer2.g;
    }
    else if (bsdfData.materialId == LIT_CLEARCOAT)
    {
        float metalic = inGBuffer2.a;
        // TODO extract spec
        float specular = 0.04;

        bsdfData.diffuseColor = baseColor * (1.0 - metalic);
        bsdfData.fresnel0 = lerp(float3(specular, specular, specular), baseColor, metalic);
        bsdfData.coatNormalWS = UnpackNormalOctEncode(float2(inGBuffer2.rg * 2.0 - 1.0));
        bsdfData.coatRoughness = inGBuffer2.b;
    }
    else if (bsdfData.materialId == LIT_SPECULAR)
    {
        bsdfData.diffuseColor = baseColor;
        bsdfData.fresnel0 = inGBuffer2.rgb;
    }

    return bsdfData;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout float outputIsLinear)
{
	if (paramId == MaterialDebugBaseColor)
	{
		result = surfaceData.baseColor;
	}
	else if (paramId == MaterialDebugSpecularOcclusion)
	{
		result = surfaceData.specularOcclusion.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugNormalWS)
	{
		result = surfaceData.normalWS * 0.5 + 0.5;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugPerceptualSmoothness)
	{
		result = surfaceData.perceptualSmoothness.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugMaterialId)
	{
		// TODO: it is an enum display solid color instead
		result = surfaceData.materialId.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugAmbientOcclusion)
	{
		result = surfaceData.ambientOcclusion.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugTangentWS)
	{
		result = surfaceData.tangentWS * 0.5 + 0.5;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugAnisotropy)
	{
		result = surfaceData.anisotropy.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugMetalic)
	{
		result = surfaceData.metalic.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugSpecular)
	{
		// TODO: may require a reamp
		result = surfaceData.specular.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugSubSurfaceRadius)
	{
		result = surfaceData.subSurfaceRadius.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugThickness)
	{
		result = surfaceData.thickness.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugSubSurfaceProfile)
	{
		// TODO:  require solid color
		result = surfaceData.subSurfaceProfile.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugCoatNormalWS)
	{
		result = surfaceData.coatNormalWS * 0.5 + 0.5;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugCoatPerceptualSmoothness)
	{
		result = surfaceData.coatPerceptualSmoothness.xxx;
		outputIsLinear = true;
	}
	else if (paramId == MaterialDebugSpecularColor)
	{
		result = surfaceData.specularColor;
	}
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

#endif // UNITY_MATERIAL_LIT_INCLUDED
