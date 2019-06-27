using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Vector 2 (Simple)")]
    class Vector2SimpleNode : CodeFunctionNode
    {
        public Vector2SimpleNode()
        {
            name = "Vector 2 (Simple)";
        }

        [HlslCodeGen]
        static void Unity_Vec2Simple(
            [Slot(0, Binding.None)] Float A,
            [Slot(1, Binding.None)] Float B,
            [Slot(2, Binding.None)] out Float2 Out)
        {
            Out = Float2(A, B);
        }
    }
}
