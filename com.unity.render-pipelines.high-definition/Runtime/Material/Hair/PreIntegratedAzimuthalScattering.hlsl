#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/PreIntegratedAzimuthalScattering.cs.hlsl"

TEXTURE2D(_PreIntegratedAzimuthalScattering);

// Returns the roughened azimuthal scattering TT distribution term.
float GetPreIntegratedAzimuthalScatteringTransmissionDistribution(float beta, float cosTheta, float cosPhi)
{
    float2 coord;
    coord.x = cosTheta;
    coord.y = FastACos(cosPhi) * INV_FOUR_PI + 0.5;

    // Sample the LUT.
    return SAMPLE_TEXTURE2D_LOD(_PreIntegratedAzimuthalScattering, s_linear_clamp_sampler, coord, 0).x;

    // Evaluate the gaussian with the sampled weights.
    // return weights.x * exp(-Sq(phi - weights.y));
}
