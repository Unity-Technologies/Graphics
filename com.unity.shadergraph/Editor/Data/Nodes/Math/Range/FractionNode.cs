using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Fraction")]
    class FractionNode : CodeFunctionNode
    {
        public FractionNode()
        {
            name = "Fraction";
        }

        [HlslCodeGen]
        static void Unity_Fraction(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = frac(In);
        }
    }
}
