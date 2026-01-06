#include "Common.hlsl"
#include "PatchUtil.hlsl"
#include "TemporalFiltering.hlsl"

void ProcessAndStoreRadianceSample(RWStructuredBuffer<SphericalHarmonics::RGBL1> patchIrradiances, RWStructuredBuffer<PatchUtil::PatchStatisticsSet> patchStatistics, uint patchIdx, SphericalHarmonics::RGBL1 radianceSample, float shortHysteresis)
{
    SphericalHarmonics::CosineConvolve(radianceSample);

    PatchUtil::PatchStatisticsSet oldStats = patchStatistics[patchIdx];
    const uint oldUpdateCount = PatchUtil::GetUpdateCount(oldStats.patchCounters);
    const uint newUpdateCount = min(oldUpdateCount + 1, PatchUtil::updateMax);

    const SphericalHarmonics::RGBL1 oldIrradiance = patchIrradiances[patchIdx];

    float shortIrradianceUpdateWeight;
    if (oldUpdateCount == 0)
        shortIrradianceUpdateWeight = 0;
    else
        shortIrradianceUpdateWeight = min(1.0f - rcp(oldUpdateCount), shortHysteresis);

    const float3 newL0ShortIrradiance = lerp(radianceSample.l0, oldStats.mean, shortIrradianceUpdateWeight);
    const float3 varianceSample = (radianceSample.l0 - newL0ShortIrradiance) * (radianceSample.l0 - oldStats.mean);
    const float3 newVariance = lerp(varianceSample, oldStats.variance, shortHysteresis);

    SphericalHarmonics::RGBL1 output = FilterTemporallyVarianceGuided(shortHysteresis, newUpdateCount, newVariance, newL0ShortIrradiance, radianceSample, oldIrradiance);

    patchIrradiances[patchIdx] = output;

    PatchUtil::PatchStatisticsSet newStats;
    newStats.mean = newL0ShortIrradiance;
    newStats.variance = newVariance;
    newStats.patchCounters = oldStats.patchCounters;
    PatchUtil::SetUpdateCount(newStats.patchCounters, newUpdateCount);
    patchStatistics[patchIdx] = newStats;
}
