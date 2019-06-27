using UnityEditor.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Negate")]
    class NegateNode : CodeFunctionNode
    {
        public NegateNode()
        {
            name = "Negate";
        }

        [HlslCodeGen]
        static void Unity_Negate(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = -In;
        }
    }
}
