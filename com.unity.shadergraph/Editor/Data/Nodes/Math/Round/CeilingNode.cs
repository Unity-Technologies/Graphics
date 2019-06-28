using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Ceiling")]
    class CeilingNode : CodeFunctionNode
    {
        public CeilingNode()
        {
            name = "Ceiling";
        }

        [HlslCodeGen]
        static void Unity_Ceiling(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = ceil(In);
        }
    }
}
