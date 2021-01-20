using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "CorneaRefraction (Preview)")]
    class CorneaRefraction : CodeFunctionNode
    {
        public CorneaRefraction()
        {
            name = "Cornea Refraction (Preview)";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_CorneaRefraction", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_CorneaRefraction(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 PositionOS,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector3 ViewDirectionOS,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector3 CorneaNormalOS,
            [Slot(3, Binding.None, 0, 0, 0, 0)] Vector1 CorneaIOR,
            [Slot(4, Binding.None, 0, 0, 0, 0)] Vector1 IrisPlaneOffset,
            [Slot(5, Binding.None)] out Vector3 RefractedPositionOS)
        {
            RefractedPositionOS = Vector3.zero;
            return
@"
                {
                    float eta = 1.0 / (CorneaIOR);
                    CorneaNormalOS = normalize(CorneaNormalOS);
                    ViewDirectionOS = -normalize(ViewDirectionOS);
                    $precision3 refractedViewDirectionOS = refract(ViewDirectionOS, CorneaNormalOS, eta);

                    // Find the distance to intersection point
                    float t = -(PositionOS.z + IrisPlaneOffset) / refractedViewDirectionOS.z;

                    // Output the refracted point in OS
                    RefractedPositionOS = $precision3(refractedViewDirectionOS.z < 0 ? PositionOS.xy + refractedViewDirectionOS.xy * t: float2(1.5, 1.5), 0.0);
                }
                ";
        }
    }
}
