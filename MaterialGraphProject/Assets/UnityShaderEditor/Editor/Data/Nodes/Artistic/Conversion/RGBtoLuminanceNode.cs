using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Art", "Conversion", "RGBtoLuminance")]
    public class RGBtoLuminanceNode : CodeFunctionNode
    {
        public RGBtoLuminanceNode()
        {
            name = "RGBtoLuminance";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RGBToLuminance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RGBToLuminance(
            [Slot(0, Binding.None)] Vector3 rgb,
            [Slot(1, Binding.None)] out Vector1 luminance)
        {
            return
                @"
{
    luminance = dot(rgb, {precision}3(0.2126729, 0.7151522, 0.0721750));
}
";
        }
    }
}
