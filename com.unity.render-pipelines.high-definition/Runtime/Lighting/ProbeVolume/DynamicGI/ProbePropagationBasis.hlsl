#ifndef PROBE_PROPAGATION_BASIS
#define PROBE_PROPAGATION_BASIS

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/SphericalGaussians.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/AmbientDice.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbeVolumeSphericalHarmonicsLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbeVolumeSphericalHarmonicsDeringing.hlsl"

// #define BASIS_SPHERICAL_GAUSSIAN
// #define BASIS_SPHERICAL_GAUSSIAN_WINDOWED
// #define BASIS_AMBIENT_DICE_SHARP
// #define BASIS_AMBIENT_DICE_SOFTER
// #define BASIS_AMBIENT_DICE_SUPER_SOFT
// #define BASIS_AMBIENT_DICE_ULTRA_SOFT

// #define BASIS_PROPAGATION_OVERRIDE_NONE
// #define BASIS_PROPAGATION_OVERRIDE_SPHERICAL_GAUSSIAN
// #define BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SOFTER
// #define BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SUPER_SOFT
// #define BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_ULTRA_SOFT

#if defined(BASIS_AMBIENT_DICE_SHARP) \
    || defined(BASIS_AMBIENT_DICE_SOFTER) \
    || defined(BASIS_AMBIENT_DICE_SUPER_SOFT) \
    || defined(BASIS_AMBIENT_DICE_ULTRA_SOFT)

#define BASIS_AMBIENT_DICE 

#endif

#if defined(BASIS_SPHERICAL_GAUSSIAN) || defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
#define BasisAxisHit SphericalGaussian
#elif defined(BASIS_AMBIENT_DICE)
#define BasisAxisHit AmbientDice
#else
#error "Undefined Probe Propagation Basis"
#endif

#if defined(BASIS_PROPAGATION_OVERRIDE_NONE)
#define BasisAxisMiss BasisAxisHit
#elif defined(BASIS_PROPAGATION_OVERRIDE_SPHERICAL_GAUSSIAN)
#define BasisAxisMiss SphericalGaussian
#elif defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SOFTER) || defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SUPER_SOFT) || defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_ULTRA_SOFT)
#define BasisAxisMiss AmbientDiceWrapped
#else
#error "Undefined Probe Propagation Override Basis"
#endif



#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbePropagationBasisInternal.hlsl"

float ComputeBasisAxisHitIntegral(BasisAxisHit basisAxisHit)
{
    float integral = 0.0;

#if defined(BASIS_SPHERICAL_GAUSSIAN)
    integral = SGIntegral(basisAxisHit);
#elif defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    integral = SGClampedCosineWindowIntegral(basisAxisHit);
#elif defined(BASIS_AMBIENT_DICE)
    integral = AmbientDiceIntegral(basisAxisHit);
#else
#error "Undefined Probe Propagation Basis"
#endif

    return integral;
}

BasisAxisHit ComputeBasisAxisHit(float3 axis, float sharpnessIn)
{
    BasisAxisHit basisAxis;
    ZERO_INITIALIZE(BasisAxisHit, basisAxis);

#if defined(BASIS_SPHERICAL_GAUSSIAN) || defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    basisAxis.sharpness = sharpnessIn;
    basisAxis.mean = axis;
#if defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    basisAxis.amplitude = ComputeSGClampedCosineWindowAmplitudeFromSharpnessAndAxisBasis26Fit(sharpnessIn, axis);
#else
    basisAxis.amplitude = ComputeSGAmplitudeFromSharpnessAndAxisBasis26Fit(sharpnessIn, axis);
#endif

#elif defined(BASIS_AMBIENT_DICE)
    float amplitude;
    float sharpness;
    ComputeAmbientDiceHitAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(amplitude, sharpness, axis);
    basisAxis.amplitude = amplitude;
    basisAxis.sharpness = sharpness;
    basisAxis.mean = axis;
#else
#error "Undefined Probe Propagation Basis"
#endif

    return basisAxis;
}

float ComputeBasisAxisHitEvaluateFromDirection(BasisAxisHit basisAxisHit, float3 direction)
{
    float weight = 0.0;

#if defined(BASIS_SPHERICAL_GAUSSIAN)
    weight = SGEvaluateFromDirection(basisAxisHit, direction);
#elif defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    weight = SGClampedCosineWindowEvaluateFromDirection(basisAxisHit, direction);
#elif defined(BASIS_AMBIENT_DICE)
    weight = AmbientDiceEvaluateFromDirection(basisAxisHit, direction);
#else
#error "Undefined Probe Propagation Basis"
#endif

    return weight;
}

BasisAxisMiss ComputeBasisAxisMiss(float3 axis, float sharpness, float propagationSharpness)
{
    BasisAxisMiss basisAxis;
    ZERO_INITIALIZE(BasisAxisMiss, basisAxis);

    basisAxis.mean = axis;

    // Note: For miss propagation, we do not use different amplitudes per axis - since it is more of a radial blur filter than a storage basis.
    // We want to blur all axes the same amount.

#if (defined(BASIS_PROPAGATION_OVERRIDE_NONE) && defined(BASIS_SPHERICAL_GAUSSIAN)) || defined(BASIS_PROPAGATION_OVERRIDE_SPHERICAL_GAUSSIAN)
    basisAxis.sharpness = propagationSharpness;
    basisAxis.amplitude = ComputeSGAmplitudeFromSharpnessBasis26Fit(propagationSharpness);

#elif defined(BASIS_PROPAGATION_OVERRIDE_NONE) && defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    basisAxis.sharpness = propagationSharpness;
    basisAxis.amplitude = ComputeSGClampedCosineWindowAmplitudeFromSharpnessBasis26Fit(propagationSharpness);

#elif defined(BASIS_PROPAGATION_OVERRIDE_NONE) && defined(BASIS_AMBIENT_DICE)
    // Same exact basis as hit.
    basisAxis = ComputeBasisAxisHit(axis, sharpness);

#elif defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SOFTER)
    float basisAmplitude;
    float basisSharpness;
    ComputeAmbientDiceSofterAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(basisAmplitude, basisSharpness, axis);
    basisAxis.amplitude = basisAmplitude * 0.5; // 0.5 scale to convert from a standard ambient dice basis to a wrapped ambient dice basis.
    basisAxis.sharpness = basisSharpness;
#elif defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SUPER_SOFT)
    float basisAmplitude;
    float basisSharpness;
    ComputeAmbientDiceSuperSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(basisAmplitude, basisSharpness, axis);
    basisAxis.amplitude = basisAmplitude * 0.5; // 0.5 scale to convert from a standard ambient dice basis to a wrapped ambient dice basis.
    basisAxis.sharpness = basisSharpness;
#elif defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_ULTRA_SOFT)
    float basisAmplitude;
    float basisSharpness;
    ComputeAmbientDiceUltraSoftAmplitudeAndSharpnessFromAxisDirectionBasis26Fit(basisAmplitude, basisSharpness, axis);
    basisAxis.amplitude = basisAmplitude * 0.5; // 0.5 scale to convert from a standard ambient dice basis to a wrapped ambient dice basis.
    basisAxis.sharpness = basisSharpness;
#else
    #error "Undefined Probe Propagation Basis"
#endif

    return basisAxis;
}

float ComputeBasisAxisMissEvaluateFromDirection(BasisAxisMiss basisAxisMiss, float3 direction)
{
    float weight = 0.0;

#if (defined(BASIS_PROPAGATION_OVERRIDE_NONE) && defined(BASIS_SPHERICAL_GAUSSIAN)) || defined(BASIS_PROPAGATION_OVERRIDE_SPHERICAL_GAUSSIAN)
    weight = SGEvaluateFromDirection(basisAxisMiss, direction);
#elif defined(BASIS_PROPAGATION_OVERRIDE_NONE) && defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    weight = SGClampedCosineWindowEvaluateFromDirection(basisAxisMiss, direction);
#elif defined(BASIS_PROPAGATION_OVERRIDE_NONE) && defined(BASIS_AMBIENT_DICE)
    weight = AmbientDiceEvaluateFromDirection(basisAxisMiss, direction);
#elif defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SOFTER) || defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_SUPER_SOFT) || defined(BASIS_PROPAGATION_OVERRIDE_AMBIENT_DICE_WRAPPED_ULTRA_SOFT)
    weight = AmbientDiceWrappedEvaluateFromDirection(basisAxisMiss, direction);
#else
#error "Undefined Probe Propagation Basis"
#endif

    return weight;
}

float ComputeBasisAxisHitAndClampedCosineProductIntegral(BasisAxisHit basisAxis, float3 surfaceNormal)
{
    float integral = 0.0;

#if defined(BASIS_SPHERICAL_GAUSSIAN)
    integral = SGIrradianceFitted(basisAxis, surfaceNormal) * INV_PI;
#elif defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    integral = SGClampedCosineWindowAndClampedCosineProductIntegral(basisAxis, surfaceNormal) * INV_PI;
#elif defined(BASIS_AMBIENT_DICE)
    integral = AmbientDiceAndClampedCosineProductIntegral(basisAxis, surfaceNormal) * INV_PI;
#else
    #error "Undefined Probe Propagation Basis"
#endif

    return integral;
}

ZHWindow ComputeZHWindowFromBasisAxisHit(BasisAxisHit basisAxisHit)
{
    ZHWindow zhWindow;
    ZERO_INITIALIZE(ZHWindow, zhWindow);

#if defined(BASIS_SPHERICAL_GAUSSIAN)
    zhWindow = ZHWindowComputeFromSphericalGaussian(basisAxisHit.sharpness);
#elif defined(BASIS_SPHERICAL_GAUSSIAN_WINDOWED)
    zhWindow = ZHWindowComputeFromSphericalGaussianCosineWindow(basisAxisHit.sharpness);
#elif defined(BASIS_AMBIENT_DICE)
    zhWindow = ZHWindowComputeFromAmbientDiceSharpness(basisAxisHit.sharpness);
#else
    #error "Undefined Probe Propagation Basis"
#endif

    return zhWindow;
}

#endif // endof PROBE_PROPAGATION_BASIS