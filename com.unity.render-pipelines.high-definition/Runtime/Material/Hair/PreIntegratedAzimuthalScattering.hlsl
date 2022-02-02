TEXTURE3D(_PreIntegratedAverageHairFiberScattering);

// Returns the roughened azimuthal scattering distribution term for all three lobes.
float3 GetRoughenedAzimuthalScatteringDistribution(float phi, float cosThetaD, float beta)
{
    const float X = (phi + TWO_PI) / FOUR_PI;
    const float Y = cosThetaD;
    const float Z = beta;

    // TODO: It should be possible to reduce the domain of the integration to 0 -> HALF/PI as it repeats. This will save memory.
    return SAMPLE_TEXTURE3D_LOD(_PreIntegratedAverageHairFiberScattering, s_linear_clamp_sampler, float3(X, Y, Z), 0).xyz;
}
