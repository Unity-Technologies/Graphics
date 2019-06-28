using UnityEngine.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Inverse Lerp")]
    class InverseLerpNode : CodeFunctionNode
    {
        public InverseLerpNode()
        {
            name = "Inverse Lerp";
        }

        [HlslCodeGen]
        static void Unity_InverseLerp(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 T,
            [Slot(3, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = (T - A) / (B - A);
        }
    }
}
