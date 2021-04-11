#ifndef RAY_TRACING_REFLECTION_COMMON_HLSL
#define RAY_TRACING_REFLECTION_COMMON_HLSL

// When doing RTR with one reflection bounce, this defines the max smoothness beyond which we won't sample
// (used in both raygen and the reflection denoiser)
#define REFLECTION_SAMPLING_MAX_ROUGHNESS_THRESHOLD (1.0-0.99)
// TODO note: kept the value as before merge, as a perceptual smoothness value == 0.99, but this means
// that eg with CLEAR_COAT_ROUGHNESS defined for Lit in CommonMaterial to be 0.01,
// perceptualRoughness(CLEAR_COAT_ROUGHNESS) is 0.01^0.5 = 0.1, so perceptualSmoothness is 0.9.
// This means that clear coat wouldn't be considered "smooth" with the threshold above.

bool SkipReflectionDenoiserHistoryAccumulation(bool affectSmoothSurfaces, bool singleReflectionBounce, float perceptualRoughness)
{
    bool isSmooth = perceptualRoughness < REFLECTION_SAMPLING_MAX_ROUGHNESS_THRESHOLD;
    bool dontAllowAccumulation = (!affectSmoothSurfaces && isSmooth && singleReflectionBounce);
    return dontAllowAccumulation;
}

#endif // RAY_TRACING_REFLECTION_COMMON_HLSL
