using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arccosine")]
    class ArccosineNode : CodeFunctionNode
    {
        public ArccosineNode()
        {
            name = "Arccosine";
        }

        [HlslCodeGen]
        static void Unity_Arccosine(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = acos(In);
        }
    }
}
