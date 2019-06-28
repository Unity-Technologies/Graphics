using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Twirl")]
    class TwirlNode : CodeFunctionNode
    {
        public TwirlNode()
        {
            name = "Twirl";
        }

        [HlslCodeGen]
        static void Unity_Twirl(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Float2 Center,
            [Slot(2, Binding.None, 10f, 0f, 0f, 0f)] Float Strength,
            [Slot(3, Binding.None)] Float2 Offset,
            [Slot(4, Binding.None)] out Float2 Out)
        {
            var delta = UV - Center;
            var angle = Strength * length(delta);
            var x = cos(angle) * delta.x - sin(angle) * delta.y;
            var y = sin(angle) * delta.x + cos(angle) * delta.y;
            Out = Float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
        }
    }
}
