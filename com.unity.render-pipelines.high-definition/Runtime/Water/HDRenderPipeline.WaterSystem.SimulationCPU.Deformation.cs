using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        internal static float EvaluateDeformers(WaterSimSearchData wsd, float3 positionAWS)
        {
            // Apply the deformation data
            float2 deformationUV = (positionAWS.xz - wsd.deformationRegionOffset) * wsd.deformationRegionScale;
            return SampleTexture2DBilinear(wsd.deformationBuffer, deformationUV + 0.5f, wsd.deformationResolution);
        }
    }
}
