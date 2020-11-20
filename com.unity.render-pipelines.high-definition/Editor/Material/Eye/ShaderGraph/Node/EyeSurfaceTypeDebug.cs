using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "High Definition Render Pipeline", "Eye", "EyeSurfaceTypeDebug (Preview)")]
    class EyeSurfaceTypeDebug : CodeFunctionNode
    {
        public EyeSurfaceTypeDebug()
        {
            name = "Eye Surface Type Debug (Preview)";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_EyeSurfaceTypeDebug", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_EyeSurfaceTypeDebug(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 PositionOS,
            [Slot(1, Binding.None, 0, 0, 0, 0)] Vector3 EyeColor,
            [Slot(2, Binding.None, 0, 0, 0, 0)] Vector1 IrisRadius,
            [Slot(3, Binding.None, 0, 0, 0, 0)] Vector1 PupilRadius,
            [Slot(4, Binding.None, 0, 0, 0, 0)] Boolean IsActive,
            [Slot(5, Binding.None)] out Vector3 SurfaceColor)
        {
            SurfaceColor = Vector3.zero;
            return
@"
                {
                    $precision pixelRadius = length(PositionOS.xy);
                    bool isSclera = pixelRadius > IrisRadius;
                    bool isPupil = !isSclera && length(PositionOS.xy / IrisRadius) < PupilRadius;
                    SurfaceColor = IsActive ? (isSclera ? 0.0 : (isPupil ? 1.0 : EyeColor)) : EyeColor;
                }
                ";
        }
    }
}
