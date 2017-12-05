using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Art", "Adjustments", "ColorBalance")]
    public class ColorBalanceNode : CodeFunctionNode
    {
        public ColorBalanceNode()
        {
            name = "ColorBalance";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ColorBalance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ColorBalance(
            [Slot(0, Binding.None)] Color inputColor,
            [Slot(1, Binding.None)] Vector3 adjustRGB,
            [Slot(2, Binding.None)] out Vector4 outColor)
        {
            outColor = Vector4.zero;
            return
                @"
{
    float red = 0;
    float green = 0;
    float blue = 0;

    red = 1.00f / (1-adjustRGB.r) * inputColor.r;
    green = 1.00f / (1-adjustRGB.g) * inputColor.g;
    blue = 1.00f / (1-adjustRGB.b) * inputColor.b;

    red = clamp(red,0.00f,1.00f);
    green = clamp(green,0.00f,1.00f);
    blue = clamp(blue,0.00f,1.00f);

    outColor.r = red;
    outColor.g = green;
    outColor.b = blue;
    outColor.a = inputColor.a;
}
";
        }
    }
}*/
