using System.Reflection;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Art/Conversion/HSVtoRGB")]
    public class HSVtoRGBNode : CodeFunctionNode
    {
        public HSVtoRGBNode()
        {
            name = "HSVtoRGB";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HSVToRGB", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HSVToRGB(
            [Slot(0, Binding.None)] Vector3 hsv,
            [Slot(1, Binding.None)] out Vector3 rgb)
        {
            rgb = Vector3.zero;
            return
                @"
{
    //Reference code from:https://github.com/Unity-Technologies/PostProcessing/blob/master/PostProcessing/Resources/Shaders/ColorGrading.cginc#L175
    {precision}4 K = {precision}4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    {precision}3 P = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    rgb = hsv.z * lerp(K.xxx, saturate(P - K.xxx), hsv.y);
}
";
        }
    }
}
