#ifndef PROBE_PROPAGATION_BASIS_INTERNAL
#define PROBE_PROPAGATION_BASIS_INTERNAL

// https://www.desmos.com/calculator/li7vrrctk6
// https://www.shadertoy.com/view/NlyXzR
float ComputeSGAmplitudeFromSharpnessBasis26Fit(float sharpness)
{
    return sharpness * 0.0734695 + 0.00862805;
}

float ComputeSGClampedCosineWindowAmplitudeFromSharpnessBasis26Fit(float sharpness)
{
    return sharpness * 0.0633994 + 0.144223;
}


float ComputeSGAmplitudeMultiplierFromAxisDirection(float3 axisDirection)
{
    int componentNonZeroCount = 0;
    componentNonZeroCount += abs(axisDirection.x) > 1e-5 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.y) > 1e-5 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.z) > 1e-5 ? 1 : 0;
    return (componentNonZeroCount == 3)
        ? 0.912 // diagonal
        : ((componentNonZeroCount == 2)
            ? 0.9595 // edge
            : 1.2445); // center
}

float ComputeSGAmplitudeFromSharpnessAndAxisBasis26Fit(float sharpness, float3 axisDirection)
{
    return ComputeSGAmplitudeFromSharpnessBasis26Fit(sharpness) * ComputeSGAmplitudeMultiplierFromAxisDirection(axisDirection);
}

float ComputeSGClampedCosineWindowAmplitudeFromSharpnessAndAxisBasis26Fit(float sharpness, float3 axisDirection)
{
    return ComputeSGClampedCosineWindowAmplitudeFromSharpnessBasis26Fit(sharpness) * ComputeSGAmplitudeMultiplierFromAxisDirection(axisDirection);
}

void ComputeAmbientDiceSharpAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, float3 axisDirection)
{
    int componentNonZeroCount = 0;
    componentNonZeroCount += abs(axisDirection.x) > 1e-3 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.y) > 1e-3 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.z) > 1e-3 ? 1 : 0;

    amplitude = (componentNonZeroCount == 3)
        ? 0.3087 // diagonal
        : ((componentNonZeroCount == 2)
            ? 0.693 // edge
            : 0.64575); // center
    sharpness = (componentNonZeroCount == 3)
        ? 9.0 // diagonal
        : ((componentNonZeroCount == 2)
            ? 6.0 // edge
            : 6.0); // center
}

void ComputeAmbientDiceSofterAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, float3 axisDirection)
{
    int componentNonZeroCount = 0;
    componentNonZeroCount += abs(axisDirection.x) > 1e-3 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.y) > 1e-3 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.z) > 1e-3 ? 1 : 0;

    amplitude = (componentNonZeroCount == 3)
        ? 0.209916 // diagonal
        : ((componentNonZeroCount == 2)
            ? 0.47124 // edge
            : 0.43911); // center
    sharpness = 4.0;
}

void ComputeAmbientDiceSuperSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, float3 axisDirection)
{
    amplitude = 0.23;
    sharpness = 2.0;
}

void ComputeAmbientDiceUltraSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, float3 axisDirection)
{
    amplitude = 0.15;
    sharpness = 1.0;
}

// https://www.shadertoy.com/view/NlyXzR
void ComputeAmbientDiceHitAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(out float amplitude, out float sharpness, float3 axisDirection)
{
    int componentNonZeroCount = 0;
    componentNonZeroCount += abs(axisDirection.x) > 1e-3 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.y) > 1e-3 ? 1 : 0;
    componentNonZeroCount += abs(axisDirection.z) > 1e-3 ? 1 : 0;

    amplitude = 0.0;
    sharpness = 0.0;

#if defined(BASIS_AMBIENT_DICE_SHARP)
    ComputeAmbientDiceSharpAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(amplitude, sharpness, axisDirection);
#elif defined(BASIS_AMBIENT_DICE_SOFTER)
    ComputeAmbientDiceSofterAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(amplitude, sharpness, axisDirection);
#elif defined(BASIS_AMBIENT_DICE_SUPER_SOFT)
    ComputeAmbientDiceSuperSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(amplitude, sharpness, axisDirection);
#elif defined(BASIS_AMBIENT_DICE_ULTRA_SOFT)
    ComputeAmbientDiceUltraSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(amplitude, sharpness, axisDirection);
#endif
}

// data for fit generated with: https://gist.github.com/pastasfuture/e1a7d80d6ed1104540b22edc15ce655a
// Fit coefficient function fit generated in desmos (for non zero parameters c0, c2, and c6): https://www.desmos.com/calculator/rbbckjkrpo
// Reference implementation: https://www.shadertoy.com/view/7tsXzH
// Deringed.
// Dering constraint is evaluated post diffuse BRDF convolution,
// Note: These ComputeSphericalHarmonicFromSphericalGaussian functions can be replaced with hardcoded constants once we make sharpness hardcoded.
float ZHWindowComputeFromSphericalGaussianC0(float sharpness)
{
    // sharpness * 1.22962 + 0.823224 is always positive. abs is to simply make the compiler happy.
    return pow(abs(sharpness) * 1.22987 + 0.823136, -1.25474) * 3.73286 + 0.0284762;
}

float ZHWindowComputeFromSphericalGaussianC1(float sharpness)
{
    return exp2(-3.90277 * pow(abs(sharpness * 0.487078 + -0.0258492), -1.46823)) * -0.801594 + 0.852515;
}

float ZHWindowComputeFromSphericalGaussianC2(float sharpness)
{
    float lhs = -0.130722 + 0.443697 * sharpness + -0.114069 * sharpness * sharpness + 0.0096001 * sharpness * sharpness * sharpness;
    float rhs = exp2(-6.45728 * pow(abs(sharpness * 3.33279 + -5.67536), -0.900997)) * -0.879522 + 0.873016;
    return sharpness > 4.086 ? rhs : lhs;
}

// Fit to pre-deringed data.
// https://www.desmos.com/calculator/z8p92bbqha
// Dering constraint is evaluated post diffuse BRDF convolution,
// so it is still possible (and likely) that the raw irradiance does contain ringing.
float ZHWindowComputeFromSphericalGaussianCosineWindowC0(float sharpness)
{
    // sharpness abs is to simply make the compiler happy.
    return pow(abs(sharpness) * 0.386259 + 1.55835, -1.52664) * 1.72488 + 0.0285548;
}

float ZHWindowComputeFromSphericalGaussianCosineWindowC1(float sharpness)
{
    return exp2(-6.82032 * pow(abs(sharpness * -1.24824 + -0.429144), -1.11639)) * -0.854767 + 0.866656;
}

float ZHWindowComputeFromSphericalGaussianCosineWindowC2(float sharpness)
{
    float lhs = 0.499141 + 0.0221239 * sharpness + -0.0207668 * sharpness * sharpness + 0.00275593 * sharpness * sharpness * sharpness;
    float rhs = exp2(-5.2741 * pow(abs(sharpness * -1.21738 + -0.189674), -1.06935)) * -0.670162 + 0.676213;
    return sharpness > 2.135 ? rhs : lhs;
}

ZHWindow ZHWindowComputeFromSphericalGaussian(float sharpness)
{
    ZHWindow zhWindow;
    zhWindow.data[0] = ZHWindowComputeFromSphericalGaussianC0(sharpness);
    zhWindow.data[1] = ZHWindowComputeFromSphericalGaussianC1(sharpness);
    zhWindow.data[2] = ZHWindowComputeFromSphericalGaussianC2(sharpness);

    return zhWindow;
}

ZHWindow ZHWindowComputeFromSphericalGaussianCosineWindow(float sharpness)
{
    ZHWindow zhWindow;
    zhWindow.data[0] = ZHWindowComputeFromSphericalGaussianCosineWindowC0(sharpness);
    zhWindow.data[1] = ZHWindowComputeFromSphericalGaussianCosineWindowC1(sharpness);
    zhWindow.data[2] = ZHWindowComputeFromSphericalGaussianCosineWindowC2(sharpness);

    return zhWindow;
}

ZHWindow ZHWindowComputeFromAmbientDiceSharpness(float sharpness)
{
    ZHWindow zhWindow;
    
    float3 coefficients = ComputeZonalHarmonicFromAmbientDiceSharpness(sharpness);
    zhWindow.data[0] = coefficients.x;
    zhWindow.data[1] = coefficients.y;
    zhWindow.data[2] = coefficients.z;

    return zhWindow;
}

#endif