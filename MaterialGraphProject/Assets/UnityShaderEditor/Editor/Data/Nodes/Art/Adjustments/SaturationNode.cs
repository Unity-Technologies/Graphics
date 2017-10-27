using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Art/Adjustments/Saturation")]
    public class SaturationNode : CodeFunctionNode
    {
        public SaturationNode()
        {
            name = "Saturation";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Saturation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Saturation(
            [Slot(0, Binding.None)] Vector3 first,
            [Slot(1, Binding.None)] Vector1 second,
            [Slot(2, Binding.None)] out Vector3 result)
        {
            result = Vector3.zero;

            return @"
{
    // RGB Saturation (closer to a vibrance effect than actual saturation)
    // Recommended workspace: ACEScg (linear)
    // Optimal range: [0.0, 2.0]
    // From PostProcessing
    {precision} luma = dot(first, {precision}3(0.2126729, 0.7151522, 0.0721750));
    result = luma.xxx + first.xxx * (second - luma.xxx);
}
";
        }
    }
}
