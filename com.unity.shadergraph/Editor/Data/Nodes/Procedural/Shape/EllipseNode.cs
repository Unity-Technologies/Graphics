using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Ellipse")]
    class EllipseNode : CodeFunctionNode
    {
        public EllipseNode()
        {
            name = "Ellipse";
        }

        [HlslCodeGen]
        static void Unity_Ellipse(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Float Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Float Height,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out Float Out)
        {
            var d = length((UV * 2 - 1) / Float2(Width, Height));
            Out = saturate((1 - d) / fwidth(d));
        }
    }
}
