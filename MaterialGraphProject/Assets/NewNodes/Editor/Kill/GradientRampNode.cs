using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "Gradient Ramp")]
    public class GradientRampNode : CodeFunctionNode
    {
        public GradientRampNode()
        {
            name = "GradientRamp";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Gradientramp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Gradientramp(
            [Slot(0, Binding.None)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 stripeCount,
            [Slot(2, Binding.None)] out Vector1 result)
        {
            return @"
{
    {precision} widthOfEachStripe = 1.0 / stripeCount;
    {precision} t = fmod(floor(uv.x / widthOfEachStripe), stripeCount);
    result = lerp(0.0, 1.0, t / (stripeCount - 1.0));;
}
";
        }
    }
}*/
