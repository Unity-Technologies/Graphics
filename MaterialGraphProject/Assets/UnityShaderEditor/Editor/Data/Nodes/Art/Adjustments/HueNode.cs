using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Art/Adjustments/Hue")]
    public class HueNode : CodeFunctionNode
    {
        public HueNode()
        {
            name = "Hue";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Hue", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Hue(
            [Slot(0, Binding.None)] Vector1 argument,
            [Slot(1, Binding.None)] out Vector3 result)
        {
            result = Vector3.zero;
            return
                @"
{
    {precision}4 K = {precision}4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    {precision}3 P = abs(frac(argument.xxx + K.xyz) * 6.0 - K.www);
    result = 1 * lerp(K.xxx, saturate(P - K.xxx), 1);
}
";
        }
    }
}
