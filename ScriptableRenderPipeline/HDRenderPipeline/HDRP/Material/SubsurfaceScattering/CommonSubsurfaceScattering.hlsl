// ----------------------------------------------------------------------------
// SSS/Transmittance helper
// ----------------------------------------------------------------------------

// Computes the fraction of light passing through the object.
// Evaluate Int{0, inf}{2 * Pi * r * R(sqrt(r^2 + d^2))}, where R is the diffusion profile.
// Note: 'volumeAlbedo' should be premultiplied by 0.25.
// Ref: Approximate Reflectance Profiles for Efficient Subsurface Scattering by Pixar (BSSRDF only).
float3 ComputeTransmittanceDisney(float3 S, float3 volumeAlbedo, float thickness, float radiusScale)
{
    // Thickness and SSS radius are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the radius scale of the profile.
    // thickness /= radiusScale;

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
// and 'halfRcpVariance1' should be prescaled by (0.1 * SssConstants.SSS_BASIC_DISTANCE_SCALE)^2.
float3 ComputeTransmittanceJimenez(float3 halfRcpVariance1, float lerpWeight1,
                                   float3 halfRcpVariance2, float lerpWeight2,
                                   float3 volumeAlbedo, float thickness, float radiusScale)
{
    // Thickness and SSS radius are decoupled for artists.
    // In theory, we should modify the thickness by the inverse of the radius scale of the profile.
    // thickness /= radiusScale;

    float t2 = thickness * thickness;

    // T = A * lerp(exp(-t2 * halfRcpVariance1), exp(-t2 * halfRcpVariance2), lerpWeight2)
    return volumeAlbedo * (exp(-t2 * halfRcpVariance1) * lerpWeight1 + exp(-t2 * halfRcpVariance2) * lerpWeight2);
}

// In order to support subsurface scattering, we need to know which pixels have an SSS material.
// It can be accomplished by reading the stencil buffer.
// A faster solution (which avoids an extra texture fetch) is to simply make sure that
// all pixels which belong to an SSS material are not black (those that don't always are).
// We choose the blue color channel since it's perceptually the least noticeable.
float3 TagLightingForSSS(float3 subsurfaceLighting)
{
    subsurfaceLighting.b = max(subsurfaceLighting.b, HALF_MIN);
    return subsurfaceLighting;
}

// See TagLightingForSSS() for details.
bool TestLightingForSSS(float3 subsurfaceLighting)
{
    return subsurfaceLighting.b > 0;
}
