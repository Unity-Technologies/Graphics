TEXTURE3D(_HairAzimuthalScattering);

// Returns the roughened azimuthal scattering distribution term for all three lobes.
float3 GetRoughenedAzimuthalScatteringDistribution(float phi, float cosThetaD, float beta)
{
    const float X = (phi + TWO_PI) / FOUR_PI;
    const float Y = cosThetaD;
    const float Z = beta;

    // TODO: It should be possible to reduce the domain of the integration to 0 -> HALF/PI as it repeats. This will save memory.
    return SAMPLE_TEXTURE3D_LOD(_HairAzimuthalScattering, s_linear_clamp_sampler, float3(X, Y, Z), 0).xyz;
}

TEXTURE3D(_HairLongitudinalScattering);

float3 GetEnergyConservingLongitudinalScattering(float sinThetaI, float sinThetaO, float beta)
{
    const float X = 0.5 + 0.5 * sinThetaI;
    const float Y = 0.5 + 0.5 * sinThetaO;
    const float Z = beta;

    return SAMPLE_TEXTURE3D_LOD(_HairLongitudinalScattering, s_linear_clamp_sampler, float3(X, Y, Z), 0).xyz;
}
