using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Sphere Mask")]
    class SphereMaskNode : CodeFunctionNode
    {
        public SphereMaskNode()
        {
            name = "Sphere Mask";
        }

        [HlslCodeGen]
        static void SphereMask(
            [Slot(0, Binding.None)] [AnyDimension] Float4 Coords,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] [AnyDimension] Float4 Center,
            [Slot(2, Binding.None, 0.1f, 0.1f, 0.1f, 0.1f)] Float Radius,
            [Slot(3, Binding.None, 0.8f, 0.8f, 0.8f, 0.8f)] Float Hardness,
            [Slot(4, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = 1 - saturate((distance(Coords, Center) - Radius) / (1 - Hardness));
        }
    }
}
