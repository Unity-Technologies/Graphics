using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Rectangle")]
    class RectangleNode : CodeFunctionNode
    {
        public RectangleNode()
        {
            name = "Rectangle";
        }

        [HlslCodeGen]
        static void Unity_Rectangle(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Float Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Float Height,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out Float Out)
        {
            var d = abs(UV * 2 - 1) - Float2(Width, Height);
            d = 1 - d / fwidth(d);
            Out = saturate(min(d.x, d.y));
        }
    }
}
