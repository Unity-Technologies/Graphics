using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Floor")]
    class FloorNode : CodeFunctionNode
    {
        public FloorNode()
        {
            name = "Floor";
        }

        [HlslCodeGen]
        static void Unity_Floor(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = floor(In);
        }
    }
}
