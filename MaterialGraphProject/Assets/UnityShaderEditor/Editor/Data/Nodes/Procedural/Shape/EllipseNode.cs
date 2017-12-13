using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Ellipse")]
    public class EllipseNode : CodeFunctionNode
    {
        public EllipseNode()
        {
            name = "Ellipse";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Ellipse", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Ellipse(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(4, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    UV = (UV * 2.0 - 1.0);
    UV = UV / {precision}2(Width, Height);
    Out = step(length(UV), 1);
}";
        }
    }
}
