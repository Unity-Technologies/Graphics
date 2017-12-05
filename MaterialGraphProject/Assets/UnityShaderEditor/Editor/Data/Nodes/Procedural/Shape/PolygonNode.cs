using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Polygon")]
    public class PolygonNode : CodeFunctionNode
    {
        public PolygonNode()
        {
            name = "Polygon";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Polygon", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Polygon(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 6, 6, 6, 6)] Vector1 Sides,
            [Slot(2, Binding.None, 1, 1, 1, 1)] Vector1 Radius,
            [Slot(3, Binding.None)] Vector1 Rotation,
            [Slot(4, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision} tau = 6.28318530718;
    {precision}2 uv = (UV * 2 - 1) * {precision}2(1, -1);
    {precision} pCoord = atan2(uv.x, uv.y) + radians(Rotation);
    {precision} r = tau / Sides;
    {precision} distance = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(uv);
    {precision} value = 1.0 - smoothstep(.4, .41, distance);
    Out = value;
}
";
        }
    }
}
