TEXTURE2D(_PreIntegratedAzimuthalScattering);

// Returns the roughened azimuthal scattering TT distribution term.
float GetPreIntegratedAzimuthalScatteringTransmissionDistribution(float beta, float cosTheta, float cosPhi)
{
    // Sample the coefficients.
    float2 c = SAMPLE_TEXTURE2D_LOD(_PreIntegratedAzimuthalScattering, s_linear_clamp_sampler, float2(beta, cosTheta), 0).xy;

    // The distribution is parameterized by phi, which means we must pay the cost of inverse trig here.
    float phi = FastACos(cosPhi);

    // Evaluate the gaussian with the sampled coefficients.
    // Gaussian denominator is pre-computed in the second coefficient.
    return c.x * exp(-Sq(phi - PI) / c.y);
}
