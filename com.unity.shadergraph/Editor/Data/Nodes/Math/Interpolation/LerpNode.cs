using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Lerp")]
    class LerpNode : CodeFunctionNode
    {
        public LerpNode()
        {
            name = "Lerp";
        }

        [HlslCodeGen]
        static void Unity_Lerp(
            [Slot(0, Binding.None, 0, 0, 0, 0)][AnyDimension] Float4 A,
            [Slot(1, Binding.None, 1, 1, 1, 1)][AnyDimension] Float4 B,
            [Slot(2, Binding.None, 0, 0, 0, 0)][AnyDimension] Float4 T,
            [Slot(3, Binding.None)][AnyDimension] out Float4 Out)
        {
            Out = lerp(A, B, T);
        }
    }
}
