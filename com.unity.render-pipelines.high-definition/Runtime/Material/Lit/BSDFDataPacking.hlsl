#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

/* Initializers */

// IMPORTANT: Assumes bsdfData.SSSData is 0s in the upper 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitSSSData(uint diffusionProfile, float subsurfaceMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.SSSData |= (diffusionProfile << 24);
    bsdfData.SSSData |= UnpackByte(subsurfaceMask) << 16;

}

// IMPORTANT: Assumes bsdfData.SSSData is 0s in the lower 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitThickness(float thickness, inout BSDFDataPacked bsdfData)
{
    bsdfData.SSSData |= UnpackShort(thickness);
}

// IMPORTANT: Assumes bsdfData.anisoDataAndFlags is 0s in the upper 24 bits. Insert using UBFE if needs updating rather than initialize.
void InitAnisoData(float anisotropy, float roughnessT, float roughnessB, inout BSDFDataPacked bsdfData)
{
    bsdfData.anisoDataAndFlags |= (UnpackByte(anisotropy) << 24 | UnpackByte(roughnessB) << 16 | UnpackByte(roughnessT) << 8);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s in the upper 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitIridescenceData(float iridescenceThickness, float iridescenceMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.iridescenceAndMasks |= (UnpackByte(iridescenceThickness) << 24 | UnpackByte(iridescenceMask) << 16);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s for the eight bits between 8th and 16th. Insert using UBFE if needs updating rather than initialize.
void InitTransmittanceMask(float transmittanceMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.iridescenceAndMasks |= (UnpackByte(transmittanceMask) << 8);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s for the lowest 8 bits. Insert using UBFE if needs updating rather than initialize.
void InitCoatMask(float coatMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.iridescenceAndMasks |= UnpackByte(coatMask);
}

void InitNormalWS(float3 normalWS, inout BSDFDataPacked bsdfData)
{
	bsdfData.normalWS = normalWS;
}

/* Setters */
void SetCoatRoughness(float newCoatRoughness, inout BSDFDataPacked bsdfData)
{
    BitFieldInsert(0x00ff0000, UnpackByte(newCoatRoughness) << 16, bsdfData.roughnessesAndOcclusions);
}

void SetRoughnessT(float newRoughnessT, inout BSDFDataPacked bsdfData)
{
    BitFieldInsert(0xff << 16, UnpackByte(newRoughnessT) << 16, bsdfData.anisoDataAndFlags);
}

void SetRoughnessB(float newRoughnessB, inout BSDFDataPacked bsdfData)
{
    BitFieldInsert(0xff << 8, UnpackByte(newRoughnessB) << 8, bsdfData.anisoDataAndFlags);
}

void SetDiffuseColor(float3 diffuseColor, inout BSDFDataPacked bsdfData)
{
	bsdfData.diffuseColor = diffuseColor;
}

void SetIOR(float ior, inout BSDFDataPacked bsdfData)
{
	bsdfData.ior = ior;
}

void SetNormalWS(float3 normalWS, inout BSDFDataPacked bsdfData)
{
	bsdfData.normalWS = normalWS;
}

/* Getters */
float3 GetFresnel0(BSDFDataPacked bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.fresnel0);
}

float GetPerceptualRoughness(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 24, 8);
}

float GetCoatRoughness(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 16, 8);
}

float GetAmbientOcclusion(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 8, 8);
}

float GetSpecularOcclusion(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 0, 8);
}

uint GetDiffusionProfile(BSDFDataPacked bsdfData)
{
    return BitFieldExtract(bsdfData.SSSData, 24, 8);
}

float GetSubsurfaceMask(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.SSSData, 16, 8);
}

float GetThickness(BSDFDataPacked bsdfData)
{
    uint floatData = BitFieldExtract(bsdfData.SSSData, 0, 16);
    return saturate(floatData * rcp(65535.0f));
}

float GetAnisotropy(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 24, 8);
}

float GetRoughnessT(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 16, 8);
}

float GetRoughnessB(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 8, 8);
}

float GetIridescenceThickness(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 24, 8);
}

float GetIridescenceMask(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 16, 8);
}

float GetTransmittanceMask(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 8, 8);
}

float GetCoatMask(BSDFDataPacked bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 0, 8);
}

float3 GetTangentWS(BSDFDataPacked bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.tangentWS);
}

float3 GetBitangentWS(BSDFDataPacked bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.bitangentWS);
}

float3 GetAbsorptionCoefficient(BSDFDataPacked bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.absorptionCoefficient);
}

float GetIOR(BSDFDataPacked bsdfData)
{
	return bsdfData.ior;
}

float3 GetTransmittance(BSDFDataPacked bsdfData)
{
	return bsdfData.transmittance;
}

float3 GetNormalWS(BSDFDataPacked bsdfData)
{
	return bsdfData.normalWS;
}


float3 GetDiffuseColor(BSDFDataPacked bsdfData)
{
	return bsdfData.diffuseColor;
}