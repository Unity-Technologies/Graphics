using UnityEditor.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Multiply (Vector)")]
    class MultiplyVectorNode : CodeFunctionNode
    {
        public MultiplyVectorNode()
        {
            name = "Multiply (Vector)";
        }

        [HlslCodeGen]
        static void Unity_Add(
            [Slot(0, Binding.None)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = A * B;
        }
    }
}
