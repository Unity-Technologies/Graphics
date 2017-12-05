using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Rectangle")]
    public class RectangleNode : CodeFunctionNode
    {
        public RectangleNode()
        {
            name = "Rectangle";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Rectangle", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rectangle(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    {precision}2 XMinAndMax = {precision}2(0.5 - Width / 2, 0.5 + Width / 2);
    {precision}2 YMinAndMax = {precision}2(0.5 - Height / 2, 0.5 + Height / 2);
    {precision} x = step( XMinAndMax.x, UV.x ) - step( XMinAndMax.y, UV.x );
    {precision} y = step( YMinAndMax.x, UV.y ) - step( YMinAndMax.y, UV.y );
    Out = x * y;
}";
        }
    }
}
