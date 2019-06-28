using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Random Range")]
    class RandomRangeNode : CodeFunctionNode
    {
        public RandomRangeNode()
        {
            name = "Random Range";
        }

        [HlslCodeGen]
        static void Unity_RandomRange(
            [Slot(0, Binding.None)] Float2 Seed,
            [Slot(1, Binding.None)] [AnyDimension] Float4 Min,
            [Slot(2, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 Max,
            [Slot(3, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Float randomno = frac(sin(dot(Seed, Float2(12.9898f, 78.233f)))) * 43758.5453;
            Out = lerp(Min, Max, randomno);
        }
    }
}
