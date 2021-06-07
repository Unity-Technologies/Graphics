TEXTURE2D(_PreIntegratedAzimuthalScattering);

float SampleAzimuthalScatteringLUT(float beta, float cosTheta, float cosPhi)
{
    // Sample the coefficients.
    float3 c = SAMPLE_TEXTURE2D_LOD(_PreIntegratedAzimuthalScattering, s_point_clamp_sampler, float2(beta, 0.5 * cosTheta + 0.5), 0).xyz;

    // The distribution is parameterized by phi, which means we must pay the cost of inverse trig here.
    float phi = acos(cosPhi);

    // The distribution is symmetrical over the origin, so take the absolute value.
    phi = abs(phi);

    // Evaluate the gaussian with the sampled coefficients.
    return c.x * exp(-Sq(phi - PI) / (2 * c.y * c.y)) + c.z;
}

float SampleAzimuthalScatteringIntegral(float beta, float cosTheta, float cosPhi)
{
    // Integrate azimuthal scattering over the fiber width using a gaussian quadrature.
    // Np(phi) = 0.5 * Int{-1, 1}{A(p, h) * D(phi - Omega)dh} where h is the fiber axis offset.
    float3 D = 0;

    // Quadrature of order 35 is sufficient for all but very smooth hairs (beta < 2º).
    const uint n = 35;

    for (uint i = 0; i < n; i++)
    {
        // Remap h to -1..1
        float h = 2 * ((float)i / n) - 1;
        float omega = AzimuthalDirection(1, ModifiedRefractionIndex(cosTheta), h);
        D += GaussianDetector(beta, acos(cosPhi) - omega);
    }

    D *= 2.0 / n;

    return 0.5 * D;
}

// Returns the roughened azimuthal scattering TT distribution term.
float GetPreIntegratedAzimuthalScatteringTransmissionDistribution(float beta, float cosTheta, float cosPhi)
{
#if 1
    return SampleAzimuthalScatteringLUT(beta, cosTheta, cosPhi);
#else
    // TODO: Remove me after the LUT matches the reference integral.
    return SampleAzimuthalScatteringIntegral(beta, cosTheta, cosPhi);
#endif
}
