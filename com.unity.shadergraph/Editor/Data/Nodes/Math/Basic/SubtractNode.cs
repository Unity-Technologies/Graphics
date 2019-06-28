using UnityEngine.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Subtract")]
    class SubtractNode : CodeFunctionNode
    {
        public SubtractNode()
        {
            name = "Subtract";
        }

        [HlslCodeGen]
        static void Unity_Subtract(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = A - B;
        }
    }
}
