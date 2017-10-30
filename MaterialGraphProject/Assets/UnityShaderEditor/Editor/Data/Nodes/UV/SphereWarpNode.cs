using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV/Spherize")]
    public class SphereWarpNode : CodeFunctionNode
    {
        public SphereWarpNode()
        {
            name = "Spherize";
        }
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SphereWarp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SphereWarp(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None, 1f, 1f, 1f, 1f)] Vector2 WarpAmount,
            [Slot(3, Binding.None)] Vector2 Offset,
            [Slot(4, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    float2 delta = UV - Center;
    float delta2 = dot(delta.xy, delta.xy);
    float delta4 = delta2 * delta2;
    float2 delta_offset = delta4 * WarpAmount;
    Out = UV + delta * delta_offset + Offset;
}";
        }
    }
}
