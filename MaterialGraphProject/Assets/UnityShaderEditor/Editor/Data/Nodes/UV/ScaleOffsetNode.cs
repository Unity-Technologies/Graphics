using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV/Tiling Offset")]
    public class ScaleOffsetNode : CodeFunctionNode
    {
        public ScaleOffsetNode()
        {
            name = "Tiling Offset";
        }
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ScaleAndOffset", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ScaleAndOffset(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector2 Tiling,
            [Slot(2, Binding.None, 0f, 0f, 0f, 0f)] Vector2 Offset,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    float4 xform = float4(Tiling, Offset);
    Out = UV * xform.xy + xform.zw;
}
";
        }
    }
}
