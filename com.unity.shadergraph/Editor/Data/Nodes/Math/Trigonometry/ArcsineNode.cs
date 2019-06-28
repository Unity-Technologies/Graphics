using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arcsine")]
    class ArcsineNode : CodeFunctionNode
    {
        public ArcsineNode()
        {
            name = "Arcsine";
        }

        [HlslCodeGen]
        static void Unity_Arcsine(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = asin(In);
        }
    }
}
