using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSystem
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
            float2 deformationUV = EvaluateDecalUV(wsd, positionAWS);
            return SampleTexture2DBilinear(wsd.deformationBuffer, deformationUV, wsd.deformationResolution);
        }

        static float LoadDeformation(WaterSimSearchData wsd, int2 coord)
        {
            return LoadTexture2D(wsd.deformationBuffer, TextureWrapMode.Clamp, TextureWrapMode.Clamp, coord, wsd.deformationResolution);
        }

        static bool IsValidCoord(int2 tapCoord, int2 res)
        {
            return tapCoord.x > 0 && tapCoord.y > 0 && tapCoord.x < (res.x - 1) && tapCoord.y < (res.y - 1);
        }

        internal static float2 EvaluateDeformerNormal(WaterSimSearchData wsd, float3 positionAWS)
        {
            if (wsd.deformationResolution.x == 0.0f && wsd.deformationResolution.y == 0.0f)
                return 0.0f;

            // Apply the deformation data
            float2 deformationUV = EvaluateDecalUV(wsd, positionAWS);
            PrepareCoordinates(deformationUV, wsd.deformationResolution, out int2 centerCoord, out float2 fract);

            // Get the displacement we need for the evaluate (and re-order them)
            float displacementCenter = LoadDeformation(wsd, centerCoord);
            float displacementRight = LoadDeformation(wsd, centerCoord + int2(1, 0));
            float displacementUp = LoadDeformation(wsd, centerCoord + int2(0, 1));

            // Evaluate the displacement normalization factor and pixel size
            float2 pixelSize = 1.0f / (wsd.deformationResolution * wsd.decalRegionScale);

            // We evaluate the displacement without the choppiness as it doesn't behave properly for distance surfaces
            float3 p0 = float3(0, displacementCenter, 0);
            float3 p1 = float3(pixelSize.x, displacementRight, 0);
            float3 p2 = float3(0, displacementUp, pixelSize.y);
            float2 surfaceGradient = EvaluateSurfaceGradients(p0, p1, p2);

            // Make sure the surface gradient is null at the edges
            return IsValidCoord(centerCoord, wsd.deformationResolution) ? surfaceGradient : float2(0.0f);
        }
    }
}
