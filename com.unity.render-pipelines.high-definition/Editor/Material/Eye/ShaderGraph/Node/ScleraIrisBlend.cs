using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "ScleraIrisBlend (Preview)")]
    class ScleraIrisBlend : CodeFunctionNode
    {
        public ScleraIrisBlend()
        {
            name = "Sclera Limbal Ring (Preview)";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ScleraIrisBlend", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ScleraIrisBlend(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 ScleraColor,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector3 ScleraNormal,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector1 ScleraSmoothness,
            [Slot(3, Binding.None, 0, 0, 0, 0)] Vector3 IrisColor,
            [Slot(4, Binding.None, 0, 0, 0, 0)] Vector3 IrisNormal,
            [Slot(5, Binding.None, 0, 0, 0, 0)] Vector1 CorneaSmoothness,
            [Slot(6, Binding.None, 0, 0, 0, 0)] Vector1 IrisRadius,
            [Slot(7, Binding.None, 0, 0, 0, 0)] Vector3 PositionOS,
            [Slot(8, Binding.None, 0, 0, 0, 0)] Vector1 DiffusionProfileSclera,
            [Slot(9, Binding.None, 0, 0, 0, 0)] Vector1 DiffusionProfileIris,
            [Slot(10, Binding.None)] out Vector3 EyeColor,
            [Slot(11, Binding.None)] out Vector1 SurfaceMask,
            [Slot(12, Binding.None)] out Vector3 DiffuseNormal,
            [Slot(13, Binding.None)] out Vector3 SpecularNormal,
            [Slot(14, Binding.None)] out Vector1 EyeSmoothness,
            [Slot(15, Binding.None)] out Vector1 SurfaceDiffusionProfile)
        {
            EyeColor = Vector3.zero;
            SurfaceMask = new Vector1();
            DiffuseNormal = Vector3.zero;
            SpecularNormal = Vector3.zero;
            EyeSmoothness = new Vector1();
            SurfaceDiffusionProfile = new Vector1();
            return
                @"
                {
                    $precision osRadius = length(PositionOS.xy);
                    $precision innerBlendRegionRadius = IrisRadius - 0.02;
                    $precision outerBlendRegionRadius = IrisRadius + 0.02;
                    $precision blendLerpFactor = 1.0 - (osRadius - IrisRadius) / (0.04);
                    blendLerpFactor = pow(blendLerpFactor, 8.0);
                    blendLerpFactor = 1.0 - blendLerpFactor;
                    SurfaceMask = (osRadius > outerBlendRegionRadius) ? 0.0 : ((osRadius < IrisRadius) ? 1.0 : (lerp(1.0, 0.0, blendLerpFactor)));
                    EyeColor = lerp(ScleraColor, IrisColor, SurfaceMask);
                    DiffuseNormal = lerp(ScleraNormal, IrisNormal, SurfaceMask);
                    SpecularNormal = lerp(ScleraNormal, float3(0.0, 0.0, 1.0), SurfaceMask);
                    EyeSmoothness = lerp(ScleraSmoothness, CorneaSmoothness, SurfaceMask);
                    SurfaceDiffusionProfile = lerp(DiffusionProfileSclera, DiffusionProfileIris, floor(SurfaceMask));
                }
                ";
        }
    }
}
