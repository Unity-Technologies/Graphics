using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Radians To Degrees")]
    class RadiansToDegreesNode : CodeFunctionNode
    {
        public RadiansToDegreesNode()
        {
            name = "Radians To Degrees";
        }

        [HlslCodeGen]
        static void Unity_RadiansToDegrees(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = degrees(In);
        }
    }
}
