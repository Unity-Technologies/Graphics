using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        static float2 RotateUV(WaterSimSearchData wsd, float2 uv)
        {
            float2 axis1 = wsd.waterForwardXZ;
            float2 axis2 = float2(-axis1.y, axis1.x);
            return float2(dot(uv, axis1), dot(uv, axis2));
        }

        internal static float EvaluateDeformers(WaterSimSearchData wsd, float3 positionAWS)
        {
            if (wsd.deformationResolution.x == 0.0f && wsd.deformationResolution.y == 0.0f)
                return 0.0f;

            // Apply the deformation data
            float2 deformationUV = RotateUV(wsd, positionAWS.xz - wsd.deformationRegionOffset) * wsd.deformationRegionScale;
            return SampleTexture2DBilinear(wsd.deformationBuffer, deformationUV + 0.5f, wsd.deformationResolution);
        }
    }
}
