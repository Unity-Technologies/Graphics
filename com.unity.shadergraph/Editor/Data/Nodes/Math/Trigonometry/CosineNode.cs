using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Cosine")]
    class CosineNode : CodeFunctionNode
    {
        public CosineNode()
        {
            name = "Cosine";
        }

        [HlslCodeGen]
        static void Unity_Cosine(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = cos(In);
        }
    }
}
