#include "Common.hlsl"
#include "PatchUtil.hlsl"
#include "TemporalFiltering.hlsl"

void ProcessAndStoreRadianceSample(RWStructuredBuffer<SphericalHarmonics::RGBL1> patchIrradiances, RWStructuredBuffer<PatchUtil::PatchStatisticsSet> patchStatistics, RWStructuredBuffer<PatchUtil::PatchCounterSet> patchCounterSets, uint patchIdx, SphericalHarmonics::RGBL1 radianceSample, float shortHysteresis)
{
    SphericalHarmonics::CosineConvolve(radianceSample);

    PatchUtil::PatchStatisticsSet oldStats = patchStatistics[patchIdx];

    const SphericalHarmonics::RGBL1 oldIrradiance = patchIrradiances[patchIdx];
    const float3 newL0ShortIrradiance = lerp(radianceSample.l0, oldStats.mean, shortHysteresis);
    const float3 varianceSample = (radianceSample.l0 - newL0ShortIrradiance) * (radianceSample.l0 - oldStats.mean);
    const float3 newVariance = lerp(varianceSample, oldStats.variance, shortHysteresis);

    const PatchUtil::PatchCounterSet oldCounterSet = patchCounterSets[patchIdx];
    const uint oldUpdateCount = PatchUtil::GetUpdateCount(patchCounterSets[patchIdx]);
    const uint newUpdateCount = min(oldUpdateCount + 1, PatchUtil::updateMax);

    PatchUtil::PatchCounterSet newCounterSet = oldCounterSet;
    PatchUtil::SetUpdateCount(newCounterSet, newUpdateCount);

    SphericalHarmonics::RGBL1 output = FilterTemporallyVarianceGuided(shortHysteresis, newUpdateCount, newVariance, newL0ShortIrradiance, radianceSample, oldIrradiance);

    patchIrradiances[patchIdx] = output;
    if (!PatchUtil::IsEqual(oldCounterSet, newCounterSet))
        patchCounterSets[patchIdx] = newCounterSet;

    PatchUtil::PatchStatisticsSet newStats;
    newStats.mean = newL0ShortIrradiance;
    newStats.variance = newVariance;
    patchStatistics[patchIdx] = newStats;
}
