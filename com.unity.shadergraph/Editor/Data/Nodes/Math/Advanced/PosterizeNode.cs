using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Posterize")]
    class PosterizeNode : CodeFunctionNode
    {
        public PosterizeNode()
        {
            name = "Posterize";
        }

        [HlslCodeGen]
        static void Unity_Posterize(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None, 4, 4, 4, 4)] [AnyDimension] Float4 Steps,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = floor(In / (1 / Steps)) * (1 / Steps);
        }
    }
}
