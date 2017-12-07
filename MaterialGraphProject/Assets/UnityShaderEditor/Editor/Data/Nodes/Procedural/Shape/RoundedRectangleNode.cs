using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Rounded Rectangle")]
    public class RoundedRectangleNode : CodeFunctionNode
    {
        public RoundedRectangleNode()
        {
            name = "Rounded Rectangle";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RoundedRectangle", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RoundedRectangle(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, 0.1f, 0, 0, 0)] Vector1 Radius,
            [Slot(4, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    {precision}2 XMinAndMax = {precision}2(0.5 - Width / 2, 0.5 + Width / 2);
    {precision}2 YMinAndMax = {precision}2(0.5 - Height / 2, 0.5 + Height / 2);
    {precision} x = step( XMinAndMax.x, UV.x ) - step( XMinAndMax.y, UV.x );
    {precision} y = step( YMinAndMax.x, UV.y ) - step( YMinAndMax.y, UV.y );
    {precision} B = x * y;
    {precision} sw = step(length(UV - {precision}2(XMinAndMax.x + Radius, YMinAndMax.x + Radius)), Radius);
    {precision} se = step(length(UV - {precision}2(XMinAndMax.y - Radius, YMinAndMax.x + Radius)), Radius);
    {precision} nw = step(length(UV - {precision}2(XMinAndMax.x + Radius, YMinAndMax.y - Radius)), Radius);
    {precision} ne = step(length(UV - {precision}2(XMinAndMax.y - Radius, YMinAndMax.y - Radius)), Radius);
    {precision} A = saturate(sw + se + nw + ne);
    Out = A;
}";
        }
    }
}
