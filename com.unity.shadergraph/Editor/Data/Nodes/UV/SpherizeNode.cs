using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Spherize")]
    class SpherizeNode : CodeFunctionNode
    {
        public SpherizeNode()
        {
            name = "Spherize";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Spherize", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_Spherize(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Float2 Center,
            [Slot(2, Binding.None, 10f, 10f, 10f, 10f)] Float2 Strength,
            [Slot(3, Binding.None)] Float2 Offset,
            [Slot(4, Binding.None)] out Float2 Out)
        {
            var delta = UV - Center;
            var delta2 = dot(delta.xy, delta.xy);
            var delta4 = delta2 * delta2;
            var delta_offset = delta4 * Strength;
            Out = UV + delta * delta_offset + Offset;
        }
    }
}
