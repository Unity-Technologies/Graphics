using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Radial Shear")]
    class RadialShearNode : CodeFunctionNode
    {
        public RadialShearNode()
        {
            name = "Radial Shear";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RadialShear", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RadialShear(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None, 10f, 10f, 10f, 10f)] Vector2 Strength,
            [Slot(3, Binding.None)] Vector2 Offset,
            [Slot(4, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
@"
{
    $precision2 delta = UV - Center;
    $precision delta2 = dot(delta.xy, delta.xy);
    $precision2 delta_offset = delta2 * Strength;
    Out = UV + $precision2(delta.y, -delta.x) * delta_offset + Offset;
}
";
        }
    }
}
