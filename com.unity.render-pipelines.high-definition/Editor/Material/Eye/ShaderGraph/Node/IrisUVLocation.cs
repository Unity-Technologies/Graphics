using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.ShaderGraph
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Eye", "IrisUVLocation (Preview)")]
    class IrisUVLocation : CodeFunctionNode
    {
        public IrisUVLocation()
        {
            name = "Iris UV Location (Preview)";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_IrisUVLocation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_IrisUVLocation(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 PositionOS,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector1 IrisRadius,
            [Slot(2, Binding.None)] out Vector2 IrisUV)
        {
            IrisUV = Vector3.zero;
            return
                @"
                {
                    $precision2 irisUVCentered = PositionOS.xy / IrisRadius;
                    IrisUV = (irisUVCentered * 0.5 + $precision2(0.5, 0.5));
                }
                ";
        }
    }
}
