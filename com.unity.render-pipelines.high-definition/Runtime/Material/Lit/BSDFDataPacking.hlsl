#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

/* Initializers */

// IMPORTANT: Assumes bsdfData.SSSData is 0s in the upper 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitSSSData(uint diffusionProfile, float subsurfaceMask, inout BSDFData bsdfData)
{
    bsdfData.SSSData |= (diffusionProfile << 24);
    bsdfData.SSSData |= UnpackByte(subsurfaceMask) << 16;

}

// IMPORTANT: Assumes bsdfData.SSSData is 0s in the lower 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitThickness(float thickness, inout BSDFData bsdfData)
{
    bsdfData.SSSData |= UnpackShort(thickness);
}

// IMPORTANT: Assumes bsdfData.anisoDataAndFlags is 0s in the upper 24 bits. Insert using UBFE if needs updating rather than initialize.
void InitAnisoData(float anisotropy, float roughnessT, float roughnessB, inout BSDFData bsdfData)
{
    bsdfData.anisoDataAndFlags |= (UnpackByte(anisotropy) << 24 | UnpackByte(roughnessB) << 16 | UnpackByte(roughnessT) << 8);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s in the upper 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitIridescenceData(float iridescenceThickness, float iridescenceMask, inout BSDFData bsdfData)
{
    bsdfData.iridescenceAndMasks |= (UnpackByte(iridescenceThickness) << 24 | UnpackByte(iridescenceMask) << 16);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s for the eight bits between 8th and 16th. Insert using UBFE if needs updating rather than initialize.
void InitTransmittanceMask(float transmittanceMask, inout BSDFData bsdfData)
{
    bsdfData.iridescenceAndMasks |= (UnpackByte(transmittanceMask) << 8);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s for the lowest 8 bits. Insert using UBFE if needs updating rather than initialize.
void InitCoatMask(float coatMask, inout BSDFData bsdfData)
{
    bsdfData.iridescenceAndMasks |= UnpackByte(coatMask);
}

void InitNormalWS(float3 normalWS, inout BSDFData bsdfData)
{
	bsdfData.normalWS = normalWS;
}

/* Setters */
void SetCoatRoughness(float newCoatRoughness, inout BSDFData bsdfData)
{
    BitFieldInsert(0x00ff0000, UnpackByte(newCoatRoughness) << 16, bsdfData.roughnessesAndOcclusions);
}

void SetRoughnessT(float newRoughnessT, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 16, UnpackByte(newRoughnessT) << 16, bsdfData.anisoDataAndFlags);
}

void SetRoughnessB(float newRoughnessB, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 8, UnpackByte(newRoughnessB) << 8, bsdfData.anisoDataAndFlags);
}

void SetDiffuseColor(float3 diffuseColor, inout BSDFData bsdfData)
{
	bsdfData.diffuseColor = diffuseColor;
}

void SetIOR(float ior, inout BSDFData bsdfData)
{
	bsdfData.ior = ior;
}

void SetNormalWS(float3 normalWS, inout BSDFData bsdfData)
{
	bsdfData.normalWS = normalWS;
}

void SetFresnel0(float3 fresnel0, inout BSDFData bsdfData)
{
	bsdfData.fresnel0 = PackToR11G11B10f(fresnel0);
}

void SetTangentWS(float3 tangentWS, inout BSDFData bsdfData)
{
	bsdfData.tangentWS = PackToR11G11B10f(tangentWS);
}

void SetBitangentWS(float3 bitangengtWS, inout BSDFData bsdfData)
{
	bsdfData.bitangentWS = PackToR11G11B10f(bitangengtWS);
}

/* Getters */
float3 GetFresnel0(BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.fresnel0);
}

float GetPerceptualRoughness(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 24, 8);
}

float GetCoatRoughness(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 16, 8);
}

float GetAmbientOcclusion(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 8, 8);
}

float GetSpecularOcclusion(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 0, 8);
}

uint GetDiffusionProfile(BSDFData bsdfData)
{
    return BitFieldExtract(bsdfData.SSSData, 24, 8);
}

float GetSubsurfaceMask(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.SSSData, 16, 8);
}

float GetThickness(BSDFData bsdfData)
{
    uint floatData = BitFieldExtract(bsdfData.SSSData, 0, 16);
    return saturate(floatData * rcp(65535.0f));
}

float GetAnisotropy(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 24, 8);
}

float GetRoughnessT(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 16, 8);
}

float GetRoughnessB(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 8, 8);
}

float GetIridescenceThickness(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 24, 8);
}

float GetIridescenceMask(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 16, 8);
}

float GetTransmittanceMask(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 8, 8);
}

float GetCoatMask(BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 0, 8);
}

float3 GetTangentWS(BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.tangentWS);
}

float3 GetBitangentWS(BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.bitangentWS);
}

float3 GetAbsorptionCoefficient(BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.absorptionCoefficient);
}

float GetIOR(BSDFData bsdfData)
{
	return bsdfData.ior;
}

float3 GetTransmittance(BSDFData bsdfData)
{
	return bsdfData.transmittance;
}

float3 GetNormalWS(BSDFData bsdfData)
{
	return bsdfData.normalWS;
}


float3 GetDiffuseColor(BSDFData bsdfData)
{
	return bsdfData.diffuseColor;
}