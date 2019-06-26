using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Radial Shear")]
    class RadialShearNode : CodeFunctionNode
    {
        public RadialShearNode()
        {
            name = "Radial Shear";
        }

        [HlslCodeGen]
        static void Unity_RadialShear(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Float2 Center,
            [Slot(2, Binding.None, 10f, 10f, 10f, 10f)] Float2 Strength,
            [Slot(3, Binding.None)] Float2 Offset,
            [Slot(4, Binding.None)] out Float2 Out)
        {
            var delta = UV - Center;
            var delta2 = dot(delta.xy, delta.xy);
            var delta_offset = delta2 * Strength;
            Out = UV + Float2(delta.y, -delta.x) * delta_offset + Offset;
        }
    }
}
