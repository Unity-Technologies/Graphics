using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Cross Product")]
    class CrossProductNode : CodeFunctionNode
    {
        public CrossProductNode()
        {
            name = "Cross Product";
        }

        [HlslCodeGen]
        static void Unity_CrossProduct(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Float3 A,
            [Slot(1, Binding.None, 0, 1, 0, 0)] Float3 B,
            [Slot(2, Binding.None)] out Float3 Out)
        {
            Out = cross(A, B);
        }
    }
}
