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
    Radius = min(abs(Radius), 0.5 * min(abs(Width), abs(Height)));
    {precision}2 XMinAndMax = {precision}2(0.5 - abs(Width) / 2, 0.5 + abs(Width) / 2);
    {precision}2 YMinAndMax = {precision}2(0.5 - abs(Height) / 2, 0.5 + abs(Height) / 2);
    {precision} wide = (step( XMinAndMax.x, UV.x ) - step( XMinAndMax.y, UV.x )) * (step( YMinAndMax.x + Radius, UV.y ) - step( YMinAndMax.y - Radius, UV.y ));
    {precision} tall = (step( XMinAndMax.x + Radius, UV.x ) - step( XMinAndMax.y - Radius, UV.x )) * (step( YMinAndMax.x, UV.y ) - step( YMinAndMax.y, UV.y ));
    {precision} sw = step(length(UV - {precision}2(XMinAndMax.x + Radius, YMinAndMax.x + Radius)), Radius);
    {precision} se = step(length(UV - {precision}2(XMinAndMax.y - Radius, YMinAndMax.x + Radius)), Radius);
    {precision} nw = step(length(UV - {precision}2(XMinAndMax.x + Radius, YMinAndMax.y - Radius)), Radius);
    {precision} ne = step(length(UV - {precision}2(XMinAndMax.y - Radius, YMinAndMax.y - Radius)), Radius);
    Out = saturate(wide + tall + sw + se + nw + ne);
}";
        }
    }
}
