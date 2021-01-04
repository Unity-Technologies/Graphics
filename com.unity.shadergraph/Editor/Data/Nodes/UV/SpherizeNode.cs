using System.Reflection;
using UnityEngine;

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

        static string Unity_Spherize(
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
    $precision delta4 = delta2 * delta2;
    $precision2 delta_offset = delta4 * Strength;
    Out = UV + delta * delta_offset + Offset;
}";
        }
    }
}
