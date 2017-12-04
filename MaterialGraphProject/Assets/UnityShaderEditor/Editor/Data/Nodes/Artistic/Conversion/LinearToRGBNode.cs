using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Art", "Conversion", "LineartoRGB")]
    public class LineartoRGBNode : CodeFunctionNode
    {
        public LineartoRGBNode()
        {
            name = "LineartoRGB";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_LinearToRGB", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_LinearToRGB(
            [Slot(0, Binding.None)] Vector3 linearColor,
            [Slot(1, Binding.None)] out Vector3 rgb)
        {
            rgb = Vector3.zero;
            return
                @"
{
    //Reference code from:https://github.com/Unity-Technologies/PostProcessing/blob/master/PostProcessing/Resources/Shaders/ColorGrading.cginc#L175
    {precision}3 sRGBLo = linearColor * 12.92;
    {precision}3 sRGBHi = (pow(max(abs(linearColor), 1.192092896e-07), {precision}3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    rgb = {precision}3(linearColor <= 0.0031308) ? sRGBLo : sRGBHi;
}
";
        }
    }
}
