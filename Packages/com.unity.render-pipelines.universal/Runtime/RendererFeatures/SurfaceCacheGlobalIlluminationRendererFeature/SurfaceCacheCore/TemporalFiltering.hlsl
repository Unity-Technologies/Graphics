#include "Common.hlsl"
#include "PatchUtil.hlsl"

SphericalHarmonics::RGBL1 FilterTemporallyVarianceGuided(float shortHysteresis, uint updateCount, float3 shortVariance, float3 shortMean, SphericalHarmonics::RGBL1 newSample, SphericalHarmonics::RGBL1 longMean)
{
    if (updateCount == 0)
    {
        return longMean;
    }
    else
    {
        const float3 stdDevEstimate = sqrt(shortVariance);
        const float3 drift = abs(shortMean - longMean.l0) / stdDevEstimate;
        const float driftScaling = 1.0f / 4.0f; // Empirically chosen. A trade-off between temporal stability and reaction time.
        const float3 driftWeight = smoothstep(0, 1, drift * driftScaling);
        const float longHys = 0.995f;

        float3 updateWeight = lerp(longHys, shortHysteresis, driftWeight);
        if (updateCount != PatchUtil::updateMax)
            updateWeight = 1.0f - rcp(updateCount);

        return SphericalHarmonics::Lerp(newSample, longMean, updateWeight);
    }
}
