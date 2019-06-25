using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Rounded Rectangle")]
    class RoundedRectangleNode : CodeFunctionNode
    {
        public RoundedRectangleNode()
        {
            name = "Rounded Rectangle";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RoundedRectangle", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_RoundedRectangle(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Float Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Float Height,
            [Slot(3, Binding.None, 0.1f, 0, 0, 0)] Float Radius,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out Float Out)
        {
            Radius = max(min(min(abs(Radius * 2), abs(Width)), abs(Height)), 1e-5);
            var uv = abs(UV * 2 - 1) - Float2(Width, Height) + Radius;
            var d = length(max(0, uv)) / Radius;
            Out = saturate((1 - d) / fwidth(d));
        }
    }
}
