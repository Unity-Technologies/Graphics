using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Polar Coordinates")]
    class PolarCoordinatesNode : CodeFunctionNode
    {
        public PolarCoordinatesNode()
        {
            name = "Polar Coordinates";
        }

        [HlslCodeGen]
        static void Unity_PolarCoordinates(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Float2 Center,
            [Slot(2, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Float RadialScale,
            [Slot(3, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Float LengthScale,
            [Slot(4, Binding.None)] out Float2 Out)
        {
            var delta = UV - Center;
            var radius = length(delta) * 2 * RadialScale;
            var angle = atan2(delta.x, delta.y) * 1.0 / 6.28 * LengthScale;
            Out = Float2(radius, angle);
        }
    }
}
