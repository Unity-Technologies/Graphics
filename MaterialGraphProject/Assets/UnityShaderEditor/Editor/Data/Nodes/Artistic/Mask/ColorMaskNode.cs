using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic/Mask/Color Mask")]
    public class ColorMaskNode : CodeFunctionNode
    {
        public ColorMaskNode()
        {
            name = "Color Mask";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ColorMask", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ColorMask(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Color MaskColor,
            [Slot(2, Binding.None)] Vector1 Range,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}3 col = {precision}3(0, 0, 0);
    {precision} Distance = distance(MaskColor, In);
    if(Distance <= Range)
        col = {precision}3(1, 1, 1);
    Out = col;
}";
        }
    }
}
