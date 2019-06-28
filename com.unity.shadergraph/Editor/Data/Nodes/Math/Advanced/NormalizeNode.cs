using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Normalize")]
    class NormalizeNode : CodeFunctionNode
    {
        public NormalizeNode()
        {
            name = "Normalize";
        }

        [HlslCodeGen]
        static void Unity_Normalize(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = normalize(In);
        }
    }
}
