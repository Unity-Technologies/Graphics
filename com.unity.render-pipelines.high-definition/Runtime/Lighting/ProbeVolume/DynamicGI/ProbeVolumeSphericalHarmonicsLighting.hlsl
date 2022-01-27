#ifndef PROBE_VOLUME_SPHERICAL_HARMONICS_LIGHTING_H
#define PROBE_VOLUME_SPHERICAL_HARMONICS_LIGHTING_H

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

// SH Constants from Ramamoorthi:
// https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf
//
// L0 = 1.0 / (2.0 * sqrt(PI)) = 0.282095
// L11,L10,L1-1: sqrt(3.0) / (2.0 * sqrt(PI)) == 0.48860 (x; z; y)
// L21,L2-1,L2-2: sqrt(15) / (2 * sqrt(PI)) == 1.092548 (xz; yz; xy)
// L20 = sqrt(5) / (4 * sqrt(PI)) == 0.315392 (3z^2 - 1)
// L22 = sqrt(15) / (4.0 * sqrt(PI)) == 0.546274 (x^2 - y^2)
//
//
// L0 constant factored to include cosine convolution and divide by PI.
// 1.0 / (2.0 * sqrt(PI)) * PI * (1.0 / PI)
// 1.0 / (2.0 * sqrt(PI))
//
//
// L11, L10, L1-1 constants factored to include cosine convolution and divide by PI.
// (sqrt(3.0) / (2.0 * sqrt(PI))) * (2.0 * PI / 3.0) * (1 / PI)
// (sqrt(3.0) / (2.0 * sqrt(PI))) * (2.0 / 3.0)
// (sqrt(3.0) / sqrt(PI)) * (1.0 / 3.0)
// (sqrt(3.0) / (3.0 * sqrt(PI))
//
//
// L21,L2-1,L2-2 constants factored to include cosine convolution and divide by PI.
// (sqrt(15.0) / (2.0 * sqrt(PI))) * (PI / 4.0) * (1 / PI)
// (sqrt(15.0) / (2.0 * sqrt(PI))) * (1.0 / 4.0)
// (sqrt(15.0) / (2.0 * 4.0 * sqrt(PI)))
// (sqrt(15.0) / (8.0 * sqrt(PI)))
//
//
// L20 constant factored to include cosine convolution and divide by PI.
// sqrt(5.0) / (4.0 * sqrt(PI)) * (PI / 4.o) * (1.0 / PI)
// sqrt(5.0) / (4.0 * sqrt(PI)) * (1.0 / 4.o)
// sqrt(5.0) / (4.0 * 4.0 * sqrt(PI))
// sqrt(5.0) / (16.0 * sqrt(PI))
//
//
// L22 constant factored to include cosine convolution and divide by PI.
// sqrt(15.0) / (4.0 * sqrt(PI)) * (PI / 4.0) * (1.0 / PI)
// sqrt(15.0) / (4.0 * sqrt(PI)) * (1.0 / 4.0)
// sqrt(15.0) / (4.0 * 4.0 * sqrt(PI))
// sqrt(15.0) / (16.0 * sqrt(PI))
//
//
// A note about this term:
// L20 = sqrt(5) / (4 * sqrt(PI)) * (3 * z^2 - 1)
//
// Unity factors it like so:
// L20 = ((3 * sqrt(5) / (4 * sqrt(PI))) * z^2 - (sqrt(5) / (4 * sqrt(PI))))
// in order to fold the constants to save an instruction.


// Constants from Unity SphericalHarmonicsL2.cpp - used in kNormalization term,
// these kNormalization terms are multiplied against the raw SH2 data before returning results from the lightmapper.
// As you can see from the factoring above, these constants from ppsloan have the cosine term and divide by PI factored in.
// SH Normalization Constants from SetSHEMapConstants function in the Stupid Spherical Harmonics Tricks paper:
// http://www.ppsloan.org/publications/StupidSH36.pdf
// #define fC0 (1.0 / (2.0 * sqrt(PI)))
// #define fC1 (sqrt(3.0) / (3.0 * sqrt(PI)))
// #define fC2 (sqrt(15.0) / (8.0 * sqrt(PI)))
// #define fC3 (sqrt(5.0) / (16.0 * sqrt(PI)))
// #define fC4 (0.5f * fC2)
// const float SphericalHarmonicsL2::kNormalizationConstants[] = { fC0, -fC1, fC1, -fC1, fC2, -fC2, fC3, -fC2, fC4 };

// Now compare the above normalization constants with those used for SHEvalDirection9() within accumulation in the lightmapper.
// Notice that other than the factoring that creates KALMOSTONETHIRD, the constants are the raw Ramamoorthi constants - no cosine or divide by pi factored in.
// This makes sense, because we generate SH terms like so:
// 1) Accumulate samples projected to SH terms (via SHEvalDirection9()).
// 2) Convolve Cosine Factor
// 3) project surface normal to SH terms (again, via SHEvalDirection9()).
// 4) Divide by PI.
//
// This can be simplified to:
// 1) Accumulate samples projected to SH terms (via SHEvalDirection9()).
// 2) Factor: Cosine Convolution, surface normal projection weights (ignoring normal contribution), and divide by pi together.
// 3) Apply factored constants to surface normal projected weights.
#define K1DIV2SQRTPI (1.0 / (2.0 * sqrt(PI)))
#define KSQRT3DIV2SQRTPI (sqrt(3.0) / (2.0 * sqrt(PI)))
#define KSQRT15DIV2SQRTPI (sqrt(15.0) / (2.0 * sqrt(PI)))
#define K3SQRT5DIV4SQRTPI (3.0 * sqrt(5.0) / (4.0 * sqrt(PI)))
#define KSQRT15DIV4SQRTPI (sqrt(15.0) / (4.0 * sqrt(PI)))
//  - comes from the missing -1 in K3SQRT5DIV4SQRTPI when compared to appendix A2 in http://www.ppsloan.org/publications/StupidSH36.pdf
#define KALMOSTONETHIRD (sqrt(5.0) / (4.0 * sqrt(PI)))

#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_0 PI
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_1 (2.0 * PI / 3.0)
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_2 (2.0 * PI / 3.0)
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_3 (2.0 * PI / 3.0)
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_4 (PI / 4.0)
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_5 (PI / 4.0)
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_6 (PI / 4.0)
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_7 (PI / 4.0)
#define SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_8 (PI / 4.0)

#define SPHERICAL_HARMONIC_DELTA_FUNCTION_L0 (1.0 / (2.0 * sqrt(PI)))
#define SPHERICAL_HARMONIC_DELTA_FUNCTION_L1 (sqrt(3.0) / (2.0 * sqrt(PI)))
#define SPHERICAL_HARMONIC_DELTA_FUNCTION_L2 (2.0 * sqrt(5.0) / (4.0 * sqrt(PI)))

#define SPHERICAL_HARMONIC_DELTA_FUNCTION_INVERSE_L0 (2.0 * sqrt(PI))
#define SPHERICAL_HARMONIC_DELTA_FUNCTION_INVERSE_L1 ((2.0 * sqrt(PI)) / sqrt(3.0))
#define SPHERICAL_HARMONIC_DELTA_FUNCTION_INVERSE_L2 ((4.0 * sqrt(PI)) / (2.0 * sqrt(5.0)))

// Identical to what the lightmapper invokes while accumulating SH terms from samples on the GPU.
float SHEvaluateDirectionFromCoefficientIndex(float3 v, int coefficientIndex)
{
    // Appendix A2 Polynomial Forms of SH Basis
    // http://www.ppsloan.org/publications/StupidSH36.pdf
    switch (coefficientIndex)
    {
        case 0: return K1DIV2SQRTPI;
        case 1: return -v.y * KSQRT3DIV2SQRTPI;
        case 2: return v.z * KSQRT3DIV2SQRTPI;
        case 3: return -v.x * KSQRT3DIV2SQRTPI;
        case 4: return v.x * v.y * KSQRT15DIV2SQRTPI;
        case 5: return -v.y * v.z * KSQRT15DIV2SQRTPI;
        case 6: return (v.z * v.z * K3SQRT5DIV4SQRTPI) + (-KALMOSTONETHIRD);
        case 7: return -v.x * v.z * KSQRT15DIV2SQRTPI;
        case 8: return (v.x * v.x - v.y * v.y) * KSQRT15DIV4SQRTPI;

        default: return 0.0;
    }
}

// Same as above but with the normal dependant factors removed.
// Pay special attention to case 6:
// (v.z * v.z * K3SQRT5DIV4SQRTPI) + (-KALMOSTONETHIRD)
// v.z * v.z * K3SQRT5DIV4SQRTPI - KALMOSTONETHIRD
// v.z * v.z * 3.0 * KALMOSTONETHIRD - KALMOSTONETHIRD
// notice that in order to get rid of the v.z * v.z terms, we need to defer handling the -KALMOSTONETHIRD term.
// The way we do this is pre-scale by KALMOSTONETHIRD, and that at runtime we need to perform these operators as a post op:
// dc -= sh[6];
// sh[6] *= 3.0;
float SHEvaluateDirectionConstantOnlyFromCoefficientIndex(int coefficientIndex)
{
    // Appendix A2 Polynomial Forms of SH Basis
    // http://www.ppsloan.org/publications/StupidSH36.pdf
    switch (coefficientIndex)
    {
        case 0: return K1DIV2SQRTPI;
        case 1: return -KSQRT3DIV2SQRTPI;
        case 2: return KSQRT3DIV2SQRTPI;
        case 3: return -KSQRT3DIV2SQRTPI;
        case 4: return KSQRT15DIV2SQRTPI;
        case 5: return -KSQRT15DIV2SQRTPI;
        case 6: return KALMOSTONETHIRD;
        case 7: return -KSQRT15DIV2SQRTPI;
        case 8: return KSQRT15DIV4SQRTPI;

        default: return 0.0;
    }
}

float SHEvaluateDirectionNormalOnlyFromCoefficientIndex(float3 v, int coefficientIndex)
{
    // Appendix A2 Polynomial Forms of SH Basis
    // http://www.ppsloan.org/publications/StupidSH36.pdf
    switch (coefficientIndex)
    {
        case 0: return 1.0;
        case 1: return v.y;
        case 2: return v.z;
        case 3: return v.x;
        case 4: return v.x * v.y;
        case 5: return v.y * v.z;
        case 6: return v.z * v.z * 3.0 - 1.0;
        case 7: return v.x * v.z;
        case 8: return (v.x * v.x - v.y * v.y);

        default: return 0.0;
    }
}

#define SH_COEFFICIENT_COUNT (9)
#define SH_PACKED_COEFFICIENT_COUNT (7)
#define ZH_COEFFICIENT_COUNT (3)

struct SHIncomingIrradiance
{
    float3 data[SH_COEFFICIENT_COUNT];
};

struct SHIncomingIrradianceScalar
{
    float data[SH_COEFFICIENT_COUNT];
};

struct ZHWindow
{
    float data[ZH_COEFFICIENT_COUNT];
};

struct SHWindow
{
    float data[SH_COEFFICIENT_COUNT];
};

struct SHOutgoingRadiosity
{
    float3 data[SH_COEFFICIENT_COUNT];
};

struct SHOutgoingRadiosityScalar
{
    float data[SH_COEFFICIENT_COUNT];
};

struct SHOutgoingRadiosityWithProjectedConstants
{
    float3 data[SH_COEFFICIENT_COUNT];
};

struct SHOutgoingRadiosityWithProjectedConstantsPacked
{
    float4 data[SH_PACKED_COEFFICIENT_COUNT];
};


float3 IncomingRadianceComputeL1(SHIncomingIrradiance sh, float3 direction)
{
    float3 incomingRadiance = 0;
    for (int i = 0; i < 4; ++i)
    {
        incomingRadiance += sh.data[i] * SHEvaluateDirectionFromCoefficientIndex(direction, i);
    }
    return incomingRadiance;
}

float3 IncomingRadianceCompute(SHIncomingIrradiance sh, float3 direction)
{
    float3 incomingRadiance = 0;
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        incomingRadiance += sh.data[i] * SHEvaluateDirectionFromCoefficientIndex(direction, i);
    }
    return incomingRadiance;
}

void SHIncomingIrradianceAccumulate(inout SHIncomingIrradiance shIncomingIrradiance, float3 direction, float3 incomingRadiance)
{
    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shIncomingIrradiance.data[i] += SHEvaluateDirectionFromCoefficientIndex(direction, i) * incomingRadiance;
    }
}

void SHIncomingIrradianceAccumulateFromSHIncomingIrradiance(inout SHIncomingIrradiance shIncomingIrradiance, SHIncomingIrradiance x)
{
    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shIncomingIrradiance.data[i] += x.data[i];
    }
}

SHOutgoingRadiosity SHOutgoingRadiosityComputeFromIncomingIrradiance(SHIncomingIrradiance shIncomingIrradiance)
{
    // https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
    const float shConvolveCosineLobeConstants[9] =
    {
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_0,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_1,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_2,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_3,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_4,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_5,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_6,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_7,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_8
    };

    SHOutgoingRadiosity shOutgoingRadiosity;

    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shOutgoingRadiosity.data[i] = shIncomingIrradiance.data[i] * shConvolveCosineLobeConstants[i] * INV_PI;
    }

    return shOutgoingRadiosity;
}

SHIncomingIrradiance SHIncomingIrradianceCompute(SHOutgoingRadiosity shOutgoingRadiosity)
{
    // https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
    const float shConvolveCosineLobeConstants[9] =
    {
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_0,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_1,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_2,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_3,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_4,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_5,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_6,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_7,
        SPHERICAL_HARMONIC_COSINE_CONVOLVE_CONSTANT_8
    };

    SHIncomingIrradiance shIncomingIrradiance;

    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shIncomingIrradiance.data[i] = (shOutgoingRadiosity.data[i] / shConvolveCosineLobeConstants[i]) * PI;
    }

    return shIncomingIrradiance;
}

SHOutgoingRadiosityWithProjectedConstants SHOutgoingRadiosityWithProjectedConstantsCompute(SHOutgoingRadiosity shOutgoingRadiosity)
{
    SHOutgoingRadiosityWithProjectedConstants shOutgoingRadiosityWithProjectedConstants;
    
    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shOutgoingRadiosityWithProjectedConstants.data[i] = shOutgoingRadiosity.data[i] * SHEvaluateDirectionConstantOnlyFromCoefficientIndex(i);
    }

    return shOutgoingRadiosityWithProjectedConstants;
}


SHOutgoingRadiosityWithProjectedConstantsPacked SHOutgoingRadiosityWithProjectedConstantsPackedCompute(SHOutgoingRadiosityWithProjectedConstants shOutgoingRadiosityWithProjectedConstants)
{
    SHOutgoingRadiosityWithProjectedConstantsPacked shOutgoingRadiosityWithProjectedConstantsPacked;

    // Constant (DC terms):
    shOutgoingRadiosityWithProjectedConstantsPacked.data[0].x = shOutgoingRadiosityWithProjectedConstants.data[0].x; // shAr.w
    shOutgoingRadiosityWithProjectedConstantsPacked.data[0].y = shOutgoingRadiosityWithProjectedConstants.data[0].y; // shAg.w
    shOutgoingRadiosityWithProjectedConstantsPacked.data[0].z = shOutgoingRadiosityWithProjectedConstants.data[0].z; // shAb.w
    // Linear: (used by L1 and L2)
    // Swizzle the coefficients to be in { x, y, z } order.
    shOutgoingRadiosityWithProjectedConstantsPacked.data[0].w = shOutgoingRadiosityWithProjectedConstants.data[3].x; // shAr.x
    shOutgoingRadiosityWithProjectedConstantsPacked.data[1].x = shOutgoingRadiosityWithProjectedConstants.data[1].x; // shAr.y
    shOutgoingRadiosityWithProjectedConstantsPacked.data[1].y = shOutgoingRadiosityWithProjectedConstants.data[2].x; // shAr.z
    shOutgoingRadiosityWithProjectedConstantsPacked.data[1].z = shOutgoingRadiosityWithProjectedConstants.data[3].y; // shAg.x
    shOutgoingRadiosityWithProjectedConstantsPacked.data[1].w = shOutgoingRadiosityWithProjectedConstants.data[1].y; // shAg.y
    shOutgoingRadiosityWithProjectedConstantsPacked.data[2].x = shOutgoingRadiosityWithProjectedConstants.data[2].y; // shAg.z
    shOutgoingRadiosityWithProjectedConstantsPacked.data[2].y = shOutgoingRadiosityWithProjectedConstants.data[3].z; // shAb.x
    shOutgoingRadiosityWithProjectedConstantsPacked.data[2].z = shOutgoingRadiosityWithProjectedConstants.data[1].z; // shAb.y
    shOutgoingRadiosityWithProjectedConstantsPacked.data[2].w = shOutgoingRadiosityWithProjectedConstants.data[2].z; // shAb.z
    // Quadratic: (used by L2)
    shOutgoingRadiosityWithProjectedConstantsPacked.data[3].x = shOutgoingRadiosityWithProjectedConstants.data[4].x; // shBr.x
    shOutgoingRadiosityWithProjectedConstantsPacked.data[3].y = shOutgoingRadiosityWithProjectedConstants.data[5].x; // shBr.y
    shOutgoingRadiosityWithProjectedConstantsPacked.data[3].z = shOutgoingRadiosityWithProjectedConstants.data[6].x; // shBr.z
    shOutgoingRadiosityWithProjectedConstantsPacked.data[3].w = shOutgoingRadiosityWithProjectedConstants.data[7].x; // shBr.w
    shOutgoingRadiosityWithProjectedConstantsPacked.data[4].x = shOutgoingRadiosityWithProjectedConstants.data[4].y; // shBg.x
    shOutgoingRadiosityWithProjectedConstantsPacked.data[4].y = shOutgoingRadiosityWithProjectedConstants.data[5].y; // shBg.y
    shOutgoingRadiosityWithProjectedConstantsPacked.data[4].z = shOutgoingRadiosityWithProjectedConstants.data[6].y; // shBg.z
    shOutgoingRadiosityWithProjectedConstantsPacked.data[4].w = shOutgoingRadiosityWithProjectedConstants.data[7].y; // shBg.w
    shOutgoingRadiosityWithProjectedConstantsPacked.data[5].x = shOutgoingRadiosityWithProjectedConstants.data[4].z; // shBb.x
    shOutgoingRadiosityWithProjectedConstantsPacked.data[5].y = shOutgoingRadiosityWithProjectedConstants.data[5].z; // shBb.y
    shOutgoingRadiosityWithProjectedConstantsPacked.data[5].z = shOutgoingRadiosityWithProjectedConstants.data[6].z; // shBb.z
    shOutgoingRadiosityWithProjectedConstantsPacked.data[5].w = shOutgoingRadiosityWithProjectedConstants.data[7].z; // shBb.w
    shOutgoingRadiosityWithProjectedConstantsPacked.data[6].x = shOutgoingRadiosityWithProjectedConstants.data[8].x; // shCr.x
    shOutgoingRadiosityWithProjectedConstantsPacked.data[6].y = shOutgoingRadiosityWithProjectedConstants.data[8].y; // shCr.y
    shOutgoingRadiosityWithProjectedConstantsPacked.data[6].z = shOutgoingRadiosityWithProjectedConstants.data[8].z; // shCr.z
    shOutgoingRadiosityWithProjectedConstantsPacked.data[6].w = 0.0;

    return shOutgoingRadiosityWithProjectedConstantsPacked;
}

SHOutgoingRadiosityWithProjectedConstants SHOutgoingRadiosityWithProjectedConstantsCompute(SHOutgoingRadiosityWithProjectedConstantsPacked shOutgoingRadiosityWithProjectedConstantsPacked)
{
    SHOutgoingRadiosityWithProjectedConstants shOutgoingRadiosityWithProjectedConstants;

    // Constant (DC terms):
    shOutgoingRadiosityWithProjectedConstants.data[0] = shOutgoingRadiosityWithProjectedConstantsPacked.data[0].xyz;

    // Linear: (used by L1 and L2)
    // Swizzle the coefficients to be in { x, y, z } order.
    shOutgoingRadiosityWithProjectedConstants.data[3].r = shOutgoingRadiosityWithProjectedConstantsPacked.data[0].w;
    shOutgoingRadiosityWithProjectedConstants.data[1].r = shOutgoingRadiosityWithProjectedConstantsPacked.data[1].x;
    shOutgoingRadiosityWithProjectedConstants.data[2].r = shOutgoingRadiosityWithProjectedConstantsPacked.data[1].y;

    shOutgoingRadiosityWithProjectedConstants.data[3].g = shOutgoingRadiosityWithProjectedConstantsPacked.data[1].z;
    shOutgoingRadiosityWithProjectedConstants.data[1].g = shOutgoingRadiosityWithProjectedConstantsPacked.data[1].w;
    shOutgoingRadiosityWithProjectedConstants.data[2].g = shOutgoingRadiosityWithProjectedConstantsPacked.data[2].x;

    shOutgoingRadiosityWithProjectedConstants.data[3].b = shOutgoingRadiosityWithProjectedConstantsPacked.data[2].y;
    shOutgoingRadiosityWithProjectedConstants.data[1].b = shOutgoingRadiosityWithProjectedConstantsPacked.data[2].z;
    shOutgoingRadiosityWithProjectedConstants.data[2].b = shOutgoingRadiosityWithProjectedConstantsPacked.data[2].w;

    // Quadratic: (used by L2)
    shOutgoingRadiosityWithProjectedConstants.data[4].r = shOutgoingRadiosityWithProjectedConstantsPacked.data[3].x;
    shOutgoingRadiosityWithProjectedConstants.data[5].r = shOutgoingRadiosityWithProjectedConstantsPacked.data[3].y;
    shOutgoingRadiosityWithProjectedConstants.data[6].r = shOutgoingRadiosityWithProjectedConstantsPacked.data[3].z;
    shOutgoingRadiosityWithProjectedConstants.data[7].r = shOutgoingRadiosityWithProjectedConstantsPacked.data[3].w;

    shOutgoingRadiosityWithProjectedConstants.data[4].g = shOutgoingRadiosityWithProjectedConstantsPacked.data[4].x;
    shOutgoingRadiosityWithProjectedConstants.data[5].g = shOutgoingRadiosityWithProjectedConstantsPacked.data[4].y;
    shOutgoingRadiosityWithProjectedConstants.data[6].g = shOutgoingRadiosityWithProjectedConstantsPacked.data[4].z;
    shOutgoingRadiosityWithProjectedConstants.data[7].g = shOutgoingRadiosityWithProjectedConstantsPacked.data[4].w;

    shOutgoingRadiosityWithProjectedConstants.data[4].b = shOutgoingRadiosityWithProjectedConstantsPacked.data[5].x;
    shOutgoingRadiosityWithProjectedConstants.data[5].b = shOutgoingRadiosityWithProjectedConstantsPacked.data[5].y;
    shOutgoingRadiosityWithProjectedConstants.data[6].b = shOutgoingRadiosityWithProjectedConstantsPacked.data[5].z;
    shOutgoingRadiosityWithProjectedConstants.data[7].b = shOutgoingRadiosityWithProjectedConstantsPacked.data[5].w;

    shOutgoingRadiosityWithProjectedConstants.data[8].rgb = shOutgoingRadiosityWithProjectedConstantsPacked.data[6].xyz;

    return shOutgoingRadiosityWithProjectedConstants;
}

SHOutgoingRadiosity SHOutgoingRadiosityCompute(SHOutgoingRadiosityWithProjectedConstants shOutgoingRadiosityWithProjectedConstants)
{
    SHOutgoingRadiosity shOutgoingRadiosity;
    
    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shOutgoingRadiosity.data[i] = shOutgoingRadiosityWithProjectedConstants.data[i] / SHEvaluateDirectionConstantOnlyFromCoefficientIndex(i);
    }

    return shOutgoingRadiosity;
}

float3 OutgoingRadianceCompute(SHOutgoingRadiosity shOutgoingRadiosity, float3 direction)
{
    float3 outgoingRadiance = 0;

    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        outgoingRadiance += shOutgoingRadiosity.data[i] * SHEvaluateDirectionFromCoefficientIndex(direction, i);
    }

    return outgoingRadiance;
}

float3 OutgoingRadianceCompute(SHOutgoingRadiosityWithProjectedConstants shOutgoingRadiosityWithProjectedConstants, float3 direction)
{
    float3 outgoingRadiance = 0;

    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        outgoingRadiance += shOutgoingRadiosityWithProjectedConstants.data[i] * SHEvaluateDirectionNormalOnlyFromCoefficientIndex(direction, i);
    }

    return outgoingRadiance;
}

void SHOutgoingRadiosityBlend(inout SHOutgoingRadiosity destOutgoingRadiosity, float destBlend, SHOutgoingRadiosity srcOutgoingRadiosity, float srcBlend)
{
    [unroll]
    for(int i=0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        destOutgoingRadiosity.data[i] = destOutgoingRadiosity.data[i] * destBlend + srcOutgoingRadiosity.data[i] * srcBlend;
    }
}

// http://filmicworlds.com/blog/simple-and-fast-spherical-harmonic-rotation/
// https://zvxryb.github.io/blog/2015/09/03/sh-lighting-part2/
void SHOutgoingRadiosityRotateBand1(float3x3 M, inout float3 x[3])
{
    float3x3 SH = float3x3(-x[2], -x[0], x[1]);

    x[0] = mul(-float3(M[0][1], M[1][1], M[2][1]), SH);
    x[1] = mul(float3(M[0][2], M[1][2], M[2][2]), SH);
    x[2] = mul(-float3(M[0][0], M[1][0], M[2][0]), SH);
}

void SHOutgoingRadiosityRotateBand2(float3x3 M, inout float3 x[5])
{
    // Decomposed + factored version of 5x5 matrix multiply of invA * sh from source.
    const float k0 = 0.9152912328637689;
    const float k1 = 0.9152912328637689 * 2.0;
    const float k2 = 1.5853309190424043;
    float3 sh0 = x[1] * -0.5 + (x[3] * 0.5 + x[4]); // 2x MADD
    float3 sh1 = (x[0] + (k2 / k0) * x[2] + x[3] + x[4]) * 0.5;
    float3 sh2 = x[0];
    float3 sh3 = -x[3];
    float3 sh4 = -x[1];

    const float k = 1.0 / sqrt(2.0);
    const float kInv = sqrt(2.0);
    const float k3 = k0 * 2.0 * K3SQRT5DIV4SQRTPI * k * k; // sqrt(3.0) / 2.0
    const float k4 = k0 * 2.0 * -KALMOSTONETHIRD;

    // Decomposed + factored version of 5x5 matrix multiply of 5 normals projected to 5 SH2 bands.
    // Column 0
    {
        float3 rn0 = float3(M[0][0], M[0][1], M[0][2]) * kInv; // (float3(1, 0, 0) * M) / k;
        x[0] = (rn0.x * rn0.y) * sh0;
        x[1] = (-rn0.y * rn0.z) * sh0;
        x[2] = (rn0.z * rn0.z * k3 + k4) * sh0;
        x[3] = (-rn0.x * rn0.z) * sh0;
        x[4] = (rn0.x * rn0.x - rn0.y * rn0.y) * sh0;
    }

    // Column 1
    {
        float3 rn1 = float3(M[2][0], M[2][1], M[2][2]) * kInv; // (float3(0, 0, 1) * M) / k;
        x[0] += (rn1.x * rn1.y) * sh1;
        x[1] += (-rn1.y * rn1.z) * sh1;
        x[2] += (rn1.z * rn1.z * k3 + k4) * sh1;
        x[3] += (-rn1.x * rn1.z) * sh1;
        x[4] += (rn1.x * rn1.x - rn1.y * rn1.y) * sh1;
    }

    // Column 2
    {
        float3 rn2 = float3(M[0][0] + M[1][0], M[0][1] + M[1][1], M[0][2] + M[1][2]); // (float3(k, k, 0) * M) / k;
        x[0] += (rn2.x * rn2.y) * sh2;
        x[1] += (-rn2.y * rn2.z) * sh2;
        x[2] += (rn2.z * rn2.z * k3 + k4) * sh2;
        x[3] += (-rn2.x * rn2.z) * sh2;
        x[4] += (rn2.x * rn2.x - rn2.y * rn2.y) * sh2;
    }

    // Column 3
    {
        float3 rn3 = float3(M[0][0] + M[2][0], M[0][1] + M[2][1], M[0][2] + M[2][2]); // (float3(k, 0, k) * M) / k;
        x[0] += (rn3.x * rn3.y) * sh3;
        x[1] += (-rn3.y * rn3.z) * sh3;
        x[2] += (rn3.z * rn3.z * k3 + k4) * sh3;
        x[3] += (-rn3.x * rn3.z) * sh3;
        x[4] += (rn3.x * rn3.x - rn3.y * rn3.y) * sh3;
    }

    // Column 4
    {
        float3 rn4 = float3(M[1][0] + M[2][0], M[1][1] + M[2][1], M[1][2] + M[2][2]); // (float3(0, k, k) * M) / k;
        x[0] += (rn4.x * rn4.y) * sh4;
        x[1] += (-rn4.y * rn4.z) * sh4;
        x[2] += (rn4.z * rn4.z * k3 + k4) * sh4;
        x[3] += (-rn4.x * rn4.z) * sh4;
        x[4] += (rn4.x * rn4.x - rn4.y * rn4.y) * sh4;
    }

    x[4] *= 0.5;
}

void SHOutgoingRadiosityRotate(float3x3 M, inout SHOutgoingRadiosity shOutgoingRadiosity)
{
    float3 x1[3];
    x1[0] = shOutgoingRadiosity.data[1];
    x1[1] = shOutgoingRadiosity.data[2];
    x1[2] = shOutgoingRadiosity.data[3];
    SHOutgoingRadiosityRotateBand1(M, x1);
    float3 x2[5];
    x2[0] = shOutgoingRadiosity.data[4];
    x2[1] = shOutgoingRadiosity.data[5];
    x2[2] = shOutgoingRadiosity.data[6];
    x2[3] = shOutgoingRadiosity.data[7];
    x2[4] = shOutgoingRadiosity.data[8];
    SHOutgoingRadiosityRotateBand2(M, x2);
    shOutgoingRadiosity.data[1] = x1[0];
    shOutgoingRadiosity.data[2] = x1[1];
    shOutgoingRadiosity.data[3] = x1[2];
    shOutgoingRadiosity.data[4] = x2[0];
    shOutgoingRadiosity.data[5] = x2[1];
    shOutgoingRadiosity.data[6] = x2[2];
    shOutgoingRadiosity.data[7] = x2[3];
    shOutgoingRadiosity.data[8] = x2[4];
}

void SHIncomingIrradianceRotate(float3x3 M, inout SHIncomingIrradiance shIncomingIrradiance)
{
    // SH Rotation of Irradiance is identical to rotation of radiosity.
    // Under the hood here, copy the data to radiosity, rotate, then copy back
    // TODO: Make sure the compiler is doing the right thing here (avoiding this copy).
    SHOutgoingRadiosity shOutgoingRadiosity;
    
    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        shOutgoingRadiosity.data[i] = shIncomingIrradiance.data[i];
    }

    SHOutgoingRadiosityRotate(M, shOutgoingRadiosity);

    [unroll]
    for (i = 0; i < 9; ++i)
    {
        shIncomingIrradiance.data[i] = shOutgoingRadiosity.data[i];
    }
}

void SHOutgoingRadiosityWithProjectedConstantsRotateBand1(float3x3 M, inout float3 x[3])
{
    float3x3 SH = float3x3(x[2], x[0], x[1]);

    x[0] = mul(float3(M[0][1], M[1][1], M[2][1]), SH);
    x[1] = mul(float3(M[0][2], M[1][2], M[2][2]), SH);
    x[2] = mul(float3(M[0][0], M[1][0], M[2][0]), SH);
}

void SHOutgoingRadiosityWithProjectedConstantsRotateBand2(float3x3 M, inout float3 x[5])
{
    // Decomposed + factored version of 5x5 matrix multiply of invA * sh from source.
    float3 sh0 = x[1] * 0.5 + (x[3] * -0.5 + x[4] * 2.0);
    float3 sh1 = x[0] * 0.5f + 3.0f * x[2] - x[3] * 0.5f + x[4];
    float3 sh2 = x[0];
    float3 sh3 = x[3];
    float3 sh4 = x[1];

    const float kInv = sqrt(2.0);
    const float k3 = 0.25f;
    const float k4 = -1.0f / 6.0f;

    // Decomposed + factored version of 5x5 matrix multiply of 5 normals projected to 5 SH2 bands.
    // Column 0
    {
        float3 rn0 = float3(M[0][0], M[0][1], M[0][2]) * kInv; // (float3(1, 0, 0) * M) / k;
        x[0] = (rn0.x * rn0.y) * sh0;
        x[1] = (rn0.y * rn0.z) * sh0;
        x[2] = (rn0.z * rn0.z * k3 + k4) * sh0;
        x[3] = (rn0.x * rn0.z) * sh0;
        x[4] = (rn0.x * rn0.x - rn0.y * rn0.y) * sh0;
    }

    // Column 1
    {
        float3 rn1 = float3(M[2][0], M[2][1], M[2][2]) * kInv; // (float3(0, 0, 1) * M) / k;
        x[0] += (rn1.x * rn1.y) * sh1;
        x[1] += (rn1.y * rn1.z) * sh1;
        x[2] += (rn1.z * rn1.z * k3 + k4) * sh1;
        x[3] += (rn1.x * rn1.z) * sh1;
        x[4] += (rn1.x * rn1.x - rn1.y * rn1.y) * sh1;
    }

    // Column 2
    {
        float3 rn2 = float3(M[0][0] + M[1][0], M[0][1] + M[1][1], M[0][2] + M[1][2]); // (float3(k, k, 0) * M) / k;
        x[0] += (rn2.x * rn2.y) * sh2;
        x[1] += (rn2.y * rn2.z) * sh2;
        x[2] += (rn2.z * rn2.z * k3 + k4) * sh2;
        x[3] += (rn2.x * rn2.z) * sh2;
        x[4] += (rn2.x * rn2.x - rn2.y * rn2.y) * sh2;
    }

    // Column 3
    {
        float3 rn3 = float3(M[0][0] + M[2][0], M[0][1] + M[2][1], M[0][2] + M[2][2]); // (float3(k, 0, k) * M) / k;
        x[0] += (rn3.x * rn3.y) * sh3;
        x[1] += (rn3.y * rn3.z) * sh3;
        x[2] += (rn3.z * rn3.z * k3 + k4) * sh3;
        x[3] += (rn3.x * rn3.z) * sh3;
        x[4] += (rn3.x * rn3.x - rn3.y * rn3.y) * sh3;
    }

    // Column 4
    {
        float3 rn4 = float3(M[1][0] + M[2][0], M[1][1] + M[2][1], M[1][2] + M[2][2]); // (float3(0, k, k) * M) / k;
        x[0] += (rn4.x * rn4.y) * sh4;
        x[1] += (rn4.y * rn4.z) * sh4;
        x[2] += (rn4.z * rn4.z * k3 + k4) * sh4;
        x[3] += (rn4.x * rn4.z) * sh4;
        x[4] += (rn4.x * rn4.x - rn4.y * rn4.y) * sh4;
    }

    x[4] *= 0.25;
}


void SHOutgoingRadiosityWithProjectedConstantsRotate(float3x3 M, inout SHOutgoingRadiosityWithProjectedConstants shOutgoingRadiosityWithProjectedConstants)
{
    float3 x1[3];
    x1[0] = shOutgoingRadiosityWithProjectedConstants.data[1];
    x1[1] = shOutgoingRadiosityWithProjectedConstants.data[2];
    x1[2] = shOutgoingRadiosityWithProjectedConstants.data[3];
    SHOutgoingRadiosityWithProjectedConstantsRotateBand1(M, x1);
    float3 x2[5];
    x2[0] = shOutgoingRadiosityWithProjectedConstants.data[4];
    x2[1] = shOutgoingRadiosityWithProjectedConstants.data[5];
    x2[2] = shOutgoingRadiosityWithProjectedConstants.data[6];
    x2[3] = shOutgoingRadiosityWithProjectedConstants.data[7];
    x2[4] = shOutgoingRadiosityWithProjectedConstants.data[8];
    SHOutgoingRadiosityWithProjectedConstantsRotateBand2(M, x2);
    shOutgoingRadiosityWithProjectedConstants.data[1] = x1[0];
    shOutgoingRadiosityWithProjectedConstants.data[2] = x1[1];
    shOutgoingRadiosityWithProjectedConstants.data[3] = x1[2];
    shOutgoingRadiosityWithProjectedConstants.data[4] = x2[0];
    shOutgoingRadiosityWithProjectedConstants.data[5] = x2[1];
    shOutgoingRadiosityWithProjectedConstants.data[6] = x2[2];
    shOutgoingRadiosityWithProjectedConstants.data[7] = x2[3];
    shOutgoingRadiosityWithProjectedConstants.data[8] = x2[4];
}

void SHOutgoingRadiosityScalarRotateBand1(float3x3 M, inout float x[3])
{
    float3 SH = float3(-x[2], -x[0], x[1]);

    x[0] = dot(SH, -float3(M[0][1], M[1][1], M[2][1]));
    x[1] = dot(SH, float3(M[0][2], M[1][2], M[2][2]));
    x[2] = dot(SH, -float3(M[0][0], M[1][0], M[2][0]));
}

void SHOutgoingRadiosityScalarRotateBand2(float3x3 M, inout float x[5])
{
    // Decomposed + factored version of 5x5 matrix multiply of invA * sh from source.
    const float k0 = 0.9152912328637689;
    const float k1 = 0.9152912328637689 * 2.0;
    const float k2 = 1.5853309190424043;
    float sh0 = x[1] * -0.5 + (x[3] * 0.5 + x[4]); // 2x MADD
    float sh1 = (x[0] + (k2 / k0) * x[2] + x[3] + x[4]) * 0.5;
    float sh2 = x[0];
    float sh3 = -x[3];
    float sh4 = -x[1];

    const float k = 1.0 / sqrt(2.0);
    const float kInv = sqrt(2.0);
    const float k3 = k0 * 2.0 * K3SQRT5DIV4SQRTPI * k * k; // sqrt(3.0) / 2.0
    const float k4 = k0 * 2.0 * -KALMOSTONETHIRD;

    // Decomposed + factored version of 5x5 matrix multiply of 5 normals projected to 5 SH2 bands.
    // Column 0
    {
        float3 rn0 = float3(M[0][0], M[0][1], M[0][2]) * kInv; // (float3(1, 0, 0) * M) / k;
        x[0] = (rn0.x * rn0.y) * sh0;
        x[1] = (-rn0.y * rn0.z) * sh0;
        x[2] = (rn0.z * rn0.z * k3 + k4) * sh0;
        x[3] = (-rn0.x * rn0.z) * sh0;
        x[4] = (rn0.x * rn0.x - rn0.y * rn0.y) * sh0;
    }

    // Column 1
    {
        float3 rn1 = float3(M[2][0], M[2][1], M[2][2]) * kInv; // (float3(0, 0, 1) * M) / k;
        x[0] += (rn1.x * rn1.y) * sh1;
        x[1] += (-rn1.y * rn1.z) * sh1;
        x[2] += (rn1.z * rn1.z * k3 + k4) * sh1;
        x[3] += (-rn1.x * rn1.z) * sh1;
        x[4] += (rn1.x * rn1.x - rn1.y * rn1.y) * sh1;
    }

    // Column 2
    {
        float3 rn2 = float3(M[0][0] + M[1][0], M[0][1] + M[1][1], M[0][2] + M[1][2]); // (float3(k, k, 0) * M) / k;
        x[0] += (rn2.x * rn2.y) * sh2;
        x[1] += (-rn2.y * rn2.z) * sh2;
        x[2] += (rn2.z * rn2.z * k3 + k4) * sh2;
        x[3] += (-rn2.x * rn2.z) * sh2;
        x[4] += (rn2.x * rn2.x - rn2.y * rn2.y) * sh2;
    }

    // Column 3
    {
        float3 rn3 = float3(M[0][0] + M[2][0], M[0][1] + M[2][1], M[0][2] + M[2][2]); // (float3(k, 0, k) * M) / k;
        x[0] += (rn3.x * rn3.y) * sh3;
        x[1] += (-rn3.y * rn3.z) * sh3;
        x[2] += (rn3.z * rn3.z * k3 + k4) * sh3;
        x[3] += (-rn3.x * rn3.z) * sh3;
        x[4] += (rn3.x * rn3.x - rn3.y * rn3.y) * sh3;
    }

    // Column 4
    {
        float3 rn4 = float3(M[1][0] + M[2][0], M[1][1] + M[2][1], M[1][2] + M[2][2]); // (float3(0, k, k) * M) / k;
        x[0] += (rn4.x * rn4.y) * sh4;
        x[1] += (-rn4.y * rn4.z) * sh4;
        x[2] += (rn4.z * rn4.z * k3 + k4) * sh4;
        x[3] += (-rn4.x * rn4.z) * sh4;
        x[4] += (rn4.x * rn4.x - rn4.y * rn4.y) * sh4;
    }

    x[4] *= 0.5;
}

void SHOutgoingRadiosityScalarRotate(float3x3 M, inout SHOutgoingRadiosityScalar shOutgoingRadiosityScalar)
{
    float x1[3];
    x1[0] = shOutgoingRadiosityScalar.data[1];
    x1[1] = shOutgoingRadiosityScalar.data[2];
    x1[2] = shOutgoingRadiosityScalar.data[3];
    SHOutgoingRadiosityScalarRotateBand1(M, x1);
    float x2[5];
    x2[0] = shOutgoingRadiosityScalar.data[4];
    x2[1] = shOutgoingRadiosityScalar.data[5];
    x2[2] = shOutgoingRadiosityScalar.data[6];
    x2[3] = shOutgoingRadiosityScalar.data[7];
    x2[4] = shOutgoingRadiosityScalar.data[8];
    SHOutgoingRadiosityScalarRotateBand2(M, x2);
    shOutgoingRadiosityScalar.data[1] = x1[0];
    shOutgoingRadiosityScalar.data[2] = x1[1];
    shOutgoingRadiosityScalar.data[3] = x1[2];
    shOutgoingRadiosityScalar.data[4] = x2[0];
    shOutgoingRadiosityScalar.data[5] = x2[1];
    shOutgoingRadiosityScalar.data[6] = x2[2];
    shOutgoingRadiosityScalar.data[7] = x2[3];
    shOutgoingRadiosityScalar.data[8] = x2[4];
}

void ZHWindowRotateBand1(float3x3 M, float zhWindowL1, out float shWindowL1[3])
{
    // Same as SHOutgoingRadiosityScalarRotate1 but specialized for zonal harmonics,
    // which only have 1 non zero value per order - the value along Z. In the case of SHL2 this is c0, c2, and c6.
    shWindowL1[0] = zhWindowL1 * -M[2][1];
    shWindowL1[1] = zhWindowL1 * M[2][2];
    shWindowL1[2] = zhWindowL1 * -M[2][0];
}

void ZHWindowRotateBand2(float3x3 M, float zhWindowL2, out float shWindowL2[5])
{
    // Same as SHOutgoingRadiosityScalarRotate2 but specialized for zonal harmonics,
    // which only have 1 non zero value per order - the value along Z. In the case of SHL2 this is c0, c2, and c6.

    // Decomposed + factored version of 5x5 matrix multiply of invA * sh from source.
    const float k0 = 0.9152912328637689;
    const float k1 = 0.9152912328637689 * 2.0;
    const float k2 = 1.5853309190424043;
    float sh1 = ((k2 / k0) * zhWindowL2) * 0.5;

    const float k = 1.0 / sqrt(2.0);
    const float kInv = sqrt(2.0);
    const float k3 = k0 * 2.0 * K3SQRT5DIV4SQRTPI * k * k; // sqrt(3.0) / 2.0
    const float k4 = k0 * 2.0 * -KALMOSTONETHIRD;

    // Decomposed + factored version of 5x5 matrix multiply of 5 normals projected to 5 SH2 bands.
    // Column 1
    {
        float3 rn1 = float3(M[2][0], M[2][1], M[2][2]) * kInv; // (float3(0, 0, 1) * M) / k;
        shWindowL2[0] = (rn1.x * rn1.y) * sh1;
        shWindowL2[1] = (-rn1.y * rn1.z) * sh1;
        shWindowL2[2] = (rn1.z * rn1.z * k3 + k4) * sh1;
        shWindowL2[3] = (-rn1.x * rn1.z) * sh1;
        shWindowL2[4] = (rn1.x * rn1.x - rn1.y * rn1.y) * sh1;
    }

    shWindowL2[4] *= 0.5;
}

void ZHWindowRotate(float3x3 M, out SHWindow shWindow, in ZHWindow zhWindow)
{
    float shWindowL1[3];
    float zhWindowL1 = zhWindow.data[1];
    ZHWindowRotateBand1(M, zhWindowL1, shWindowL1);
    float shWindowL2[5];
    float zhWindowL2 = zhWindow.data[2];
    ZHWindowRotateBand2(M, zhWindowL2, shWindowL2);
    shWindow.data[0] = zhWindow.data[0];
    shWindow.data[1] = shWindowL1[0];
    shWindow.data[2] = shWindowL1[1];
    shWindow.data[3] = shWindowL1[2];
    shWindow.data[4] = shWindowL2[0];
    shWindow.data[5] = shWindowL2[1];
    shWindow.data[6] = shWindowL2[2];
    shWindow.data[7] = shWindowL2[3];
    shWindow.data[8] = shWindowL2[4];
}

// source: Building an Orthonormal Basis, Revisited
// http://jcgt.org/published/0006/01/01/
// Same as reference implementation, except transposed.
float3x3 ComputeTangentToWorldMatrix(float3 n)
{
    float3x3 res;
    res[2][0] = n.x;
    res[2][1] = n.y;
    res[2][2] = n.z;

    float s = (n.z >= 0.0f) ? 1.0f : -1.0f;
    float a = -1.0f / (s + n.z);
    float b = n.x * n.y * a;

    res[0][0] = 1.0f + s * n.x * n.x * a;
    res[0][1] = s * b;
    res[0][2] = -s * n.x;

    res[1][0] = b;
    res[1][1] = s + n.y * n.y * a;
    res[1][2] = -n.y;

    return res;
}

void FrameFromNormal(float3 normal, out float3 tangent, out float3 binormal)
{
    float3x3 tangentToWorldMatrix = ComputeTangentToWorldMatrix(normal);
    float3x3 worldToTangentMatrix = tangentToWorldMatrix;
    binormal = worldToTangentMatrix[1];
    tangent = worldToTangentMatrix[0];
}

void SHIncomingIrradianceConvolveSHWindow(inout SHIncomingIrradiance shIncomingIrradiance, SHWindow shWindow)
{
    [unroll]
    for (int i = 0; i < SH_COEFFICIENT_COUNT; ++i)
    {
        shIncomingIrradiance.data[i] *= shWindow.data[i];
    }
}

SHWindow SHWindowComputeFromZHWindow(ZHWindow zhWindow, float3 zhDirection)
{
    SHWindow shWindow;
    float3x3 tangentToWorldMatrix = ComputeTangentToWorldMatrix(zhDirection);
    ZHWindowRotate(tangentToWorldMatrix, shWindow, zhWindow);

    return shWindow;
}

SHIncomingIrradiance SHIncomingIrradianceComputeFromSHWindowAndRadiance(SHWindow shWindow, float3 radiance)
{
    SHIncomingIrradiance shIncomingIrradiance;

    [unroll]
    for (int c = 0; c < SH_COEFFICIENT_COUNT; ++c)
    {
        shIncomingIrradiance.data[c] = shWindow.data[c] * radiance;
    }

    return shIncomingIrradiance;
}

void SHIncomingIrradianceConvolveZHWindow(inout SHIncomingIrradiance shIncomingIrradiance, ZHWindow zhWindow)
{
    shIncomingIrradiance.data[0] *= zhWindow.data[0];

    shIncomingIrradiance.data[1] *= zhWindow.data[1];
    shIncomingIrradiance.data[2] *= zhWindow.data[1];
    shIncomingIrradiance.data[3] *= zhWindow.data[1];

    shIncomingIrradiance.data[4] *= zhWindow.data[2];
    shIncomingIrradiance.data[5] *= zhWindow.data[2];
    shIncomingIrradiance.data[6] *= zhWindow.data[2];
    shIncomingIrradiance.data[7] *= zhWindow.data[2];
    shIncomingIrradiance.data[8] *= zhWindow.data[2];
}

void SHIncomingIrradianceConvolveZHWindowWithoutDeltaFunction(inout SHIncomingIrradiance shIncomingIrradiance, ZHWindow zhWindow)
{
    zhWindow.data[0] *= SPHERICAL_HARMONIC_DELTA_FUNCTION_INVERSE_L0;
    zhWindow.data[1] *= SPHERICAL_HARMONIC_DELTA_FUNCTION_INVERSE_L1;
    zhWindow.data[2] *= SPHERICAL_HARMONIC_DELTA_FUNCTION_INVERSE_L2;

    shIncomingIrradiance.data[0] *= zhWindow.data[0];

    shIncomingIrradiance.data[1] *= zhWindow.data[1];
    shIncomingIrradiance.data[2] *= zhWindow.data[1];
    shIncomingIrradiance.data[3] *= zhWindow.data[1];

    shIncomingIrradiance.data[4] *= zhWindow.data[2];
    shIncomingIrradiance.data[5] *= zhWindow.data[2];
    shIncomingIrradiance.data[6] *= zhWindow.data[2];
    shIncomingIrradiance.data[7] *= zhWindow.data[2];
    shIncomingIrradiance.data[8] *= zhWindow.data[2];
}

void SHIncomingIrradianceConvolveDirectionalZHWindow(inout SHIncomingIrradiance shIncomingIrradiance, ZHWindow zhWindow, float3 zhDirection)
{
    SHWindow shWindow = SHWindowComputeFromZHWindow(zhWindow, zhDirection);

    SHIncomingIrradianceConvolveSHWindow(shIncomingIrradiance, shWindow);
}

// optimal linear direction, related to bent normal, etc.
float3 SHOutgoingRadiosityScalarGetOptimalLinearDirection(const SHOutgoingRadiosityScalar shOutgoingRadiosityScalar)
{
    return float3(-shOutgoingRadiosityScalar.data[3], -shOutgoingRadiosityScalar.data[1], shOutgoingRadiosityScalar.data[2]);
}

//////////// Francesco's Versions

real3 SHEvalLinearL1(real3 N, real3 shAr, real3 shAg, real3 shAb)
{
    real3 x1;
    x1.r = dot(shAr, N);
    x1.g = dot(shAg, N);
    x1.b = dot(shAb, N);

    return x1;
}


float3 FCCEvaluate(SHIncomingIrradiance shIncomingIrradiance, float3 direction)
{
    float4 shAr = float4(shIncomingIrradiance.data[1].r, shIncomingIrradiance.data[2].r, shIncomingIrradiance.data[3].r, shIncomingIrradiance.data[0].r);
    float4 shAg = float4(shIncomingIrradiance.data[1].g, shIncomingIrradiance.data[2].g, shIncomingIrradiance.data[3].g, shIncomingIrradiance.data[0].g);
    float4 shAb = float4(shIncomingIrradiance.data[1].b, shIncomingIrradiance.data[2].b, shIncomingIrradiance.data[3].b, shIncomingIrradiance.data[0].b);
    float3 L1Eval = SHEvalLinearL1(direction, shAr.xyz, shAg.xyz, shAb.xyz);

    float3 output = shIncomingIrradiance.data[0];
    output += L1Eval;

    output += SHEvalLinearL2(direction, float4(shIncomingIrradiance.data[4].r, shIncomingIrradiance.data[5].r, shIncomingIrradiance.data[6].r, shIncomingIrradiance.data[7].r),
                                        float4(shIncomingIrradiance.data[4].g, shIncomingIrradiance.data[5].g, shIncomingIrradiance.data[6].g, shIncomingIrradiance.data[7].g),
                                        float4(shIncomingIrradiance.data[4].b, shIncomingIrradiance.data[5].b, shIncomingIrradiance.data[6].b, shIncomingIrradiance.data[7].b),
                                        float4(shIncomingIrradiance.data[8], 1.0f));
    return output;
}


// Constants from SetSHEMapConstants function in the Stupid Spherical Harmonics Tricks paper:
// http://www.ppsloan.org/publications/StupidSH36.pdf
//  [SH basis coeff] * [clamped cosine convolution factor]
#define fC0 (rsqrt(PI * 4.0) * rsqrt(PI * 4.0))  // Equivalent (0.282095 * (1.0 / (2.0 * sqrtPI)))
#define fC1 (rsqrt(PI * 4.0 / 3.0) * rsqrt(PI * 3.0)) // Equivalent to (0.488603 * (sqrt ( 3.0) / ( 3.0 * sqrtPI)))
#define fC2 (rsqrt(PI * 4.0 / 15.0) * rsqrt(PI * 64.0 / 15.0)) // Equivalent to (1.092548 * (sqrt (15.0) / ( 8.0 * sqrtPI)))
#define fC3 (rsqrt(PI * 16.0 / 5.0) * rsqrt(PI * 256.0 / 5.0)) // Equivalent to (0.315392 * (sqrt ( 5.0) / (16.0 * sqrtPI)))
#define fC4 (rsqrt(PI * 16.0 / 15.0) * rsqrt(PI * 256.0 / 15.0)) // Equivalent to  (0.546274 * 0.5 * (sqrt (15.0) / ( 8.0 * sqrtPI)))

void FCCAddToOutputRepresentation(float3 value, float3 direction, inout SHIncomingIrradiance shIncomingIrradiance)
{
    static const float ConvolveCosineLobeBandFactor[] = { fC0, -fC1, fC1, -fC1, fC2, -fC2, fC3, -fC2, fC4 };

    const float kNormalization = 2.9567930857315701067858823529412f; // 16*kPI/17

    float weight = kNormalization;

    float3 L0 = value * ConvolveCosineLobeBandFactor[0] * weight;
    float3 L1_0 = -direction.y * value * ConvolveCosineLobeBandFactor[1] * weight;
    float3 L1_1 = direction.z * value * ConvolveCosineLobeBandFactor[2] * weight;
    float3 L1_2 = -direction.x * value * ConvolveCosineLobeBandFactor[3] * weight;

    shIncomingIrradiance.data[0] += L0;
    shIncomingIrradiance.data[1] += L1_0;
    shIncomingIrradiance.data[2] += L1_1;
    shIncomingIrradiance.data[3] += L1_2;

    float3 L2_0 = direction.x * direction.y * value * ConvolveCosineLobeBandFactor[4] * weight;
    float3 L2_1 = -direction.y * direction.z * value * ConvolveCosineLobeBandFactor[5] * weight;
    float3 L2_2 = (3.0 * direction.z * direction.z - 1.0f) * value * ConvolveCosineLobeBandFactor[6] * weight;
    float3 L2_3 = -direction.x * direction.z * value * ConvolveCosineLobeBandFactor[7] * weight;
    float3 L2_4 = (direction.x * direction.x - direction.y * direction.y) * value * ConvolveCosineLobeBandFactor[8] * weight;

    shIncomingIrradiance.data[4] += L2_0;
    shIncomingIrradiance.data[5] += L2_1;
    shIncomingIrradiance.data[6] += L2_2;
    shIncomingIrradiance.data[7] += L2_3;
    shIncomingIrradiance.data[8] += L2_4;
}

float3 DecodeSH(float l0, float3 l1)
{
    // TODO: We're working on irradiance instead of radiance coefficients
    //       Add safety margin 2 to avoid out-of-bounds values
    const float l1scale = 2;//1.7320508f; // 3/(2*sqrt(3)) * 2

    return (l1 - 0.5f) * 2.0f * l1scale * l0;
}

void DecodeSH_L2(float3 l0, inout float4 l2_R, inout float4 l2_G, inout float4 l2_B, inout float4 l2_C)
{
    // TODO: We're working on irradiance instead of radiance coefficients
    //       Add safety margin 2 to avoid out-of-bounds values
    const float l2scale = 3.5777088f; // 4/sqrt(5) * 2

    l2_R = (l2_R - 0.5f) * l2scale * l0.r;
    l2_G = (l2_G - 0.5f) * l2scale * l0.g;
    l2_B = (l2_B - 0.5f) * l2scale * l0.b;
    l2_C = (l2_C - 0.5f) * l2scale;

    l2_C.r *= l0.r;
    l2_C.g *= l0.g;
    l2_C.b *= l0.b;
}

float ComputeZHNewWindowCoefficient(float g, float l)
{
    if (g < 1e-5) { return 1.0; }
    float p = 1.0 + (0.8 - 1.0) * pow(g / 0.18, 4.0);

    float w = 1.0 / max(1e-5, g);
    
    float numerator = sin(PI * pow(l, p) / (pow(2.0, p - 1.0) * w));
    float denominator = PI * pow(l, p) / (pow(2.0, p - 1.0) * w);

    return pow(numerator / denominator, 4.0);
}

float3 ComputeWindowFromManualDeringIntensity(float x)
{
    // New window function from:
    // Mentioned in ghost of tsushima
    // https://www.desmos.com/calculator/bopiih3uka
    return float3(
        ComputeZHNewWindowCoefficient(x * 0.18, 1.0),
        ComputeZHNewWindowCoefficient(x * 0.18, 2.0),
        ComputeZHNewWindowCoefficient(x * 0.18, 3.0)
    );
}
#endif
