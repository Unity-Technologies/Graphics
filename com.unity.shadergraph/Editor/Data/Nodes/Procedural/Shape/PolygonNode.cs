using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Polygon")]
    class PolygonNode : CodeFunctionNode
    {
        public PolygonNode()
        {
            name = "Polygon";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Polygon", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_Polygon(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 6, 0, 0, 0)] Float Sides,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Float Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Float Height,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] [AnyDimension] out Float4 Out)
        {
            var pi = 3.14159265359;
            var aWidth = Width * cos(pi / Sides);
            var aHeight = Height * cos(pi / Sides);
            var uv = (UV * 2 - 1) / Float2(aWidth, aHeight);
            uv.y *= -1;
            var pCoord = atan2(uv.x, uv.y);
            var r = 2 * pi / Sides;
            var distance = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(uv);
            Out = saturate((1 - distance) / fwidth(distance));
        }
    }
}
