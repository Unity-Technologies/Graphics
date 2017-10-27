using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV/ScaleOffset")]
    public class ScaleOffsetNode : CodeFunctionNode
    {
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ScaleAndOffset", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ScaleAndOffset(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector2 scale,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 scaleCenter,
            [Slot(3, Binding.None)] Vector2 offset,
            [Slot(4, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;
            return
                @"
{
    float4 xform = float4(scale, offset + scaleCenter - scaleCenter * scale);
    result = uv * xform.xy + xform.zw;
}
";
        }
    }
}
