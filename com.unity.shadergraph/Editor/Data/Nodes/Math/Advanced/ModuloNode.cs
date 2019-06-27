using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Modulo")]
    class ModuloNode : CodeFunctionNode
    {
        public ModuloNode()
        {
            name = "Modulo";
        }

        [HlslCodeGen]
        static void Unity_Modulo(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = fmod(A, B);
        }
    }
}
