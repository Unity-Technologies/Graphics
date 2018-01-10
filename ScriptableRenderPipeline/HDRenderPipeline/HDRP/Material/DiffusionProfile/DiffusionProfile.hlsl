// ----------------------------------------------------------------------------
// SSS/Transmittance helper
// ----------------------------------------------------------------------------

// Computes the fraction of light passing through the object.
// Evaluate Int{0, inf}{2 * Pi * r * R(sqrt(r^2 + d^2))}, where R is the diffusion profile.
// Note: 'volumeAlbedo' should be premultiplied by 0.25.
// Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar (BSSRDF only).
float3 ComputeTransmittanceDisney(float3 S, float3 volumeAlbedo, float thickness)
{
    // Thickness and SSS mask are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the mask scale of the profile.
    // thickness /= subsurfaceMask;

#if 0
    float3 expOneThird = exp(((-1.0 / 3.0) * thickness) * S);
#else
    // Help the compiler.
    float  k = (-1.0 / 3.0) * LOG2_E;
    float3 p = (k * thickness) * S;
    float3 expOneThird = exp2(p);
#endif

    // Premultiply & optimize: T = (1/4 * A) * (e^(-t * S) + 3 * e^(-1/3 * t * S))
    return volumeAlbedo * (expOneThird * expOneThird * expOneThird + 3 * expOneThird);
}

// Evaluates transmittance for a linear combination of two normalized 2D Gaussians.
// Ref: Real-Time Realistic Skin Translucency (2010), equation 9 (modified).
// Note: 'volumeAlbedo' should be premultiplied by 0.25, correspondingly 'lerpWeight' by 4,
// and 'halfRcpVariance1' should be prescaled by (0.1 * DiffusionProfileConstants.SSS_BASIC_DISTANCE_SCALE)^2.
float3 ComputeTransmittanceJimenez(float3 halfRcpVariance1, float lerpWeight1,
                                   float3 halfRcpVariance2, float lerpWeight2,
                                   float3 volumeAlbedo, float thickness)
{
    // Thickness and SSS mask are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the mask scale of the profile.
    // thickness /= subsurfaceMask;

    float t2 = thickness * thickness;

    // T = A * lerp(exp(-t2 * halfRcpVariance1), exp(-t2 * halfRcpVariance2), lerpWeight2)
    return volumeAlbedo * (exp(-t2 * halfRcpVariance1) * lerpWeight1 + exp(-t2 * halfRcpVariance2) * lerpWeight2);
}
