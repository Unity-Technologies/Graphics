#include "Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
#include "VectorLogic.hlsl"

// This compression assumes a few things:
// 1) The coefs must be with respect to the standard orthonormal SH basis. For example,
//    the first basis function must be 1/sqrt(4pi).
// 2) Irradiance is calculated by projecting radiance into SH, then converting to
//    irradiance using Ramamoorthi's method, https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf.
// 3) The coefs are exact (as opposed to noisy approximations).
// If these assumptions are met then L1 is guaranteed to be in [0,1] after compression. L0 terms
// are unaffected. If (2) and (3) are not met, then the L1 terms should still be _approximately_ in
// [0,1].
namespace IrradianceCompression
{
    void Compress(inout SphericalHarmonics::RGBL1 irradiance)
    {
        const float3 multiplier = VECTOR_LOGIC_SELECT(irradiance.l0 == 0.0f, 0.0f, sqrt(3.0f) / 4.0f * rcp(irradiance.l0));
        const float3 addend = VECTOR_LOGIC_SELECT(irradiance.l0 == 0.0f, 0.0f, 0.5f);

        UNITY_UNROLL // To get rid of some shader warnings on some platforms
        for (uint i = 0; i < 3; ++i)
            irradiance.l1s[i] = irradiance.l1s[i] * multiplier + addend;
    }

    void Decompress(inout SphericalHarmonics::RGBL1 irradiance)
    {
        const float3 multiplier = 4.0f / sqrt(3.0f) * irradiance.l0;
        const float3 addend = VECTOR_LOGIC_SELECT(irradiance.l0 == 0.0f, 0.0f, -0.5f);

        UNITY_UNROLL // To get rid of some shader warnings on some platforms
        for (uint i = 0; i < 3; ++i)
            irradiance.l1s[i] = (irradiance.l1s[i] + addend) * multiplier;
    }

    SphericalHarmonics::RGBL1 LoadAndDecompress(
        TYPED_TEXTURE2D_X(float3, l0),
        TYPED_TEXTURE2D_X(float3, l10),
        TYPED_TEXTURE2D_X(float3, l11),
        TYPED_TEXTURE2D_X(float3, l12),
        uint2 pos)
    {
        SphericalHarmonics::RGBL1 result;
        result.l0 = l0[COORD_TEXTURE2D_X(pos)];
        result.l1s[0] = l10[COORD_TEXTURE2D_X(pos)];
        result.l1s[1] = l11[COORD_TEXTURE2D_X(pos)];
        result.l1s[2] = l12[COORD_TEXTURE2D_X(pos)];
        IrradianceCompression::Decompress(result);
        return result;
    }

    void CompressAndStore(
        SphericalHarmonics::RGBL1 irradiance,
        RW_TEXTURE2D_X(float3, l0),
        RW_TEXTURE2D_X(float3, l10),
        RW_TEXTURE2D_X(float3, l11),
        RW_TEXTURE2D_X(float3, l12),
        uint2 pos)
    {
        IrradianceCompression::Compress(irradiance);
        l0[COORD_TEXTURE2D_X(pos)] = irradiance.l0;
        l10[COORD_TEXTURE2D_X(pos)] = irradiance.l1s[0];
        l11[COORD_TEXTURE2D_X(pos)] = irradiance.l1s[1];
        l12[COORD_TEXTURE2D_X(pos)] = irradiance.l1s[2];
    }
}
