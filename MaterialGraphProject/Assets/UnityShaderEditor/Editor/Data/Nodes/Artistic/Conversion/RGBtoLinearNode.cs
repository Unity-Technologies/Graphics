using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Art", "Conversion", "RGBtoLinear")]
    public class RGBtoLinearNode : CodeFunctionNode
    {
        public RGBtoLinearNode()
        {
            name = "RGBtoLinear";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RGBToLinear", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RGBToLinear(
            [Slot(0, Binding.None)] Vector3 rgb,
            [Slot(1, Binding.None)] out Vector3 linearColor)
        {
            linearColor = Vector3.zero;
            return
                @"
{
    //Reference code from:http://www.chilliant.com/rgb2hsv.html
    {precision}3 linearRGBLo = rgb / 12.92;;
    {precision}3 linearRGBHi = pow(max(abs((rgb + 0.055) / 1.055), 1.192092896e-07), {precision}3(2.4, 2.4, 2.4));;
    linearColor = {precision}3(rgb <= 0.04045) ? linearRGBLo : linearRGBHi;
}
";
        }
    }
}
