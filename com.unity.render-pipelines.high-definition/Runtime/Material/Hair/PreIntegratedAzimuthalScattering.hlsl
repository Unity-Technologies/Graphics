#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/PreIntegratedAzimuthalScattering.cs.hlsl"

TEXTURE2D(_PreIntegratedAzimuthalScattering);

// Returns the azimuthal scattering distribution term.
float GetPreIntegratedAzimuthalScattering(float beta, float theta, float phi)
{
    // TODO: Evaluate a gaussian with the sampled weights from the LUT and Phi.
    return 0;
}
