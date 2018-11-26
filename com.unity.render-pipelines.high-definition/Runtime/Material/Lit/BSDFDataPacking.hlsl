#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// TODO: REALLY, CLEANUP.DO IT BEFORE PR.

// TMP UTIL, MOVE SOMEWHERE ELSE OR USE UNPACK BYTE.
uint Get8BitUint(float f)
{
    return (uint)(f * 255.0f + 0.5f);
}


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

