using Unity.Collections;
using Unity.Burst;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class InstanceCullerBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static unsafe void SetupCullingJobInput(float lodBias, float meshLodThreshold, BatchCullingContext* context,
            ReceiverPlanes* receiverPlanes, ReceiverSphereCuller* receiverSphereCuller, FrustumPlaneCuller* frustumPlaneCuller,
            float* screenRelativeMetric, float* meshLodConstant)
        {
            *receiverPlanes = ReceiverPlanes.Create(*context, Allocator.TempJob);
            *receiverSphereCuller = ReceiverSphereCuller.Create(*context, Allocator.TempJob);
            *frustumPlaneCuller = FrustumPlaneCuller.Create(*context, receiverPlanes->planes.AsArray(), *receiverSphereCuller, Allocator.TempJob);
            *screenRelativeMetric = LODRenderingUtils.CalculateScreenRelativeMetricNoBias(context->lodParameters);
            *meshLodConstant = LODRenderingUtils.CalculateMeshLodConstant(context->lodParameters, *screenRelativeMetric, meshLodThreshold);
            *screenRelativeMetric /= lodBias;
        }
    }
}
