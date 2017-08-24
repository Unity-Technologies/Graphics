using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/SphereWarpNode")]
    public class SphereWarpNode : CodeFunctionNode
    {
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SphereWarp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SphereWarp(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 center,
            [Slot(2, Binding.None, 1f, 1f, 1f, 1f)] Vector2 warpAmount,
            [Slot(3, Binding.None)] Vector2 offset,
            [Slot(4, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;
            return
                @"
{
    float2 delta = uv - center;
    float delta2 = dot(delta.xy, delta.xy);
    float delta4 = delta2 * delta2;
    float2 delta_offset = delta4 * warpAmount;
    result = uv + delta * delta_offset + offset;
}";
        }
    }
}
