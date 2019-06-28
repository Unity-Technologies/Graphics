using UnityEngine.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Divide")]
    class DivideNode : CodeFunctionNode
    {
        public DivideNode()
        {
            name = "Divide";
        }

        [HlslCodeGen]
        static void Unity_Divide(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None, 2, 2, 2, 2)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = A / B;
        }
    }
}
