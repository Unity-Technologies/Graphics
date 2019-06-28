using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Combine")]
    class CombineNode : CodeFunctionNode
    {
        public CombineNode()
        {
            name = "Combine";
        }

        [HlslCodeGen]
        static void Unity_Combine(
            [Slot(0, Binding.None)] Float R,
            [Slot(1, Binding.None)] Float G,
            [Slot(2, Binding.None)] Float B,
            [Slot(3, Binding.None)] Float A,
            [Slot(4, Binding.None)] out Float4 RGBA,
            [Slot(5, Binding.None)] out Float3 RGB,
            [Slot(6, Binding.None)] out Float2 RG)
        {
            RGBA = Float4(R, G, B, A);
            RGB = Float3(R, G, B);
            RG = Float2(R, G);
        }
    }
}
