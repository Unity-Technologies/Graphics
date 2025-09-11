using System;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Lightmapping
{
    internal static class AdaptiveSamplingHelpers
    {
        internal const float AdaptiveThreshold_00 = 0.0f; // Accept no error
        internal const float AdaptiveThreshold_01 = 0.01f / 2.58f; // Accept 1% error using 99% confidence interval
        internal const float AdaptiveThreshold_05 = 0.05f / 1.96f; // Accept 5% error using 95% confidence interval
        internal const float AdaptiveThreshold_10 = 0.10f / 1.645f; // Accept 10% error using 90% confidence interval
        internal const float AdaptiveThreshold_20 = 0.20f / 1.285f; // Accept 20% error using 80% confidence interval
        internal const float AdaptiveThreshold_30 = 0.30f / 1.035f; // Accept 30% error using 70% confidence interval
        internal const float AdaptiveThreshold_40 = 0.40f / 0.845f; // Accept 40% error using 60% confidence interval
        internal const float AdaptiveThreshold_50 = 0.50f / 0.675f; // Accept 50% error using 50% confidence interval
    }
}
