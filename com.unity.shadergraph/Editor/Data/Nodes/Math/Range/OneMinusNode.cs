using UnityEditor.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "One Minus")]
    class OneMinusNode : CodeFunctionNode
    {
        public OneMinusNode()
        {
            name = "One Minus";
        }

        [HlslCodeGen]
        static void Unity_OneMinus(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = 1 - In;
        }
    }
}
