#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// TODO: REALLY, CLEANUP.DO IT BEFORE PR.

// TMP UTIL, MOVE SOMEWHERE ELSE OR USE UNPACK BYTE.
uint Get8BitUint(float f)
{
    return (uint)(f * 255.0f + 0.5f);
}

float Extract8BitFloat(uint data, uint offset)
{
    uint floatData = BitFieldExtract(data, offset, 8);
    return saturate(floatData * rcp(255.0f));
}

/* Initializers */

// IMPORTANT: Assumes bsdfData.SSSData is 0s in the upper 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitSSSData(uint diffusionProfile, float subsurfaceMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.SSSData |= (diffusionProfile << 24);
    bsdfData.SSSData |= Get8BitUint(subsurfaceMask) << 16;

}

// IMPORTANT: Assumes bsdfData.SSSData is 0s in the lower 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitThickness(float thickness, inout BSDFDataPacked bsdfData)
{
    bsdfData.SSSData |= UnpackShort(thickness);
}

// IMPORTANT: Assumes bsdfData.anisoDataAndFlags is 0s in the upper 24 bits. Insert using UBFE if needs updating rather than initialize.
void InitAnisoData(float anisotropy, float roughnessT, float roughnessB, inout BSDFDataPacked bsdfData)
{
    bsdfData.anisoDataAndFlags |= (Get8BitUint(anisotropy) << 24 | Get8BitUint(roughnessB) << 16 | Get8BitUint(roughnessT) << 8);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s in the upper 16 bits. Insert using UBFE if needs updating rather than initialize.
void InitIridescenceData(float iridescenceThickness, float iridescenceMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.iridescenceAndMasks |= (Get8BitUint(iridescenceThickness) << 24 | Get8BitUint(iridescenceMask) << 16);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s for the eight bits between 8th and 16th. Insert using UBFE if needs updating rather than initialize.
void InitTransmittanceMask(float transmittanceMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.iridescenceAndMasks |= (Get8BitUint(transmittanceMask) << 8);
}

// IMPORTANT: Assumes bsdfData.iridescenceAndMasks is 0s for the lowest 8 bits. Insert using UBFE if needs updating rather than initialize.
void InitCoatMask(float coatMask, inout BSDFDataPacked bsdfData)
{
    bsdfData.iridescenceAndMasks |= Get8BitUint(coatMask);
}

/* Setters */
void SetCoatRoughness(float newCoatRoughness, out BSDFDataPacked bsdfData)
{
    BitFieldInsert(0x00ff0000, Get8BitUint(newCoatRoughness) << 16, bsdfData.roughnessesAndOcclusions);
}

void SetRoughnessT(float newRoughnessT, out BSDFDataPacked bsdfData)
{
    BitFieldInsert(0xff << 16, Get8BitUint(newRoughnessT) << 16, bsdfData.anisoDataAndFlags);
}

void SetRoughnessB(float newRoughnessB, out BSDFDataPacked bsdfData)
{
    BitFieldInsert(0xff << 8, Get8BitUint(newRoughnessB) << 8, bsdfData.anisoDataAndFlags);
}

/* Getters */
float3 GetFresnel0(BSDFDataPacked bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.fresnel0);
}

float GetPerceptualRoughness(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.roughnessesAndOcclusions, 24);
}

float GetCoatRoughness(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.roughnessesAndOcclusions, 16);
}

float GetAmbientOcclusion(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.roughnessesAndOcclusions, 8);
}

float GetSpecularOcclusion(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.roughnessesAndOcclusions, 0);
}

uint GetDiffusionProfile(BSDFDataPacked bsdfData)
{
    return BitFieldExtract(bsdfData.SSSData, 24, 8);
}

float GetSubsurfaceMask(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.SSSData, 16);
}

float GetThickness(BSDFDataPacked bsdfData)
{
    uint floatData = BitFieldExtract(bsdfData.SSSData, 0, 16);
    return saturate(floatData * rcp(65535.0f));
}

float GetAnisotropy(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.anisoDataAndFlags, 24);
}

float GetRoughnessT(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.anisoDataAndFlags, 16);
}

float GetRoughnessB(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.anisoDataAndFlags, 8);
}

float GetIridescenceThickness(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.iridescenceAndMasks, 24);
}

float GetIridescenceMask(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.iridescenceAndMasks, 16);
}

float GetTransmittanceMask(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.iridescenceAndMasks, 8);
}

float GetCoatMask(BSDFDataPacked bsdfData)
{
    return Extract8BitFloat(bsdfData.iridescenceAndMasks, 0);
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

