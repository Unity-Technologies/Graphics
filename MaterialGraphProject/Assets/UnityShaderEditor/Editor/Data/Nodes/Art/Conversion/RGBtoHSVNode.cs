using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Art/Conversion/RGBtoHSV")]
    public class RGBtoHSVNode : CodeFunctionNode
    {
        public RGBtoHSVNode()
        {
            name = "RGBtoHSV";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RGBtoHSV", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RGBtoHSV(
            [Slot(0, Binding.None)] Vector3 rgb,
            [Slot(1, Binding.None)] out Vector3 hsv)
        {
            hsv = Vector3.zero;
            return
                @"
{
    //Reference code from:http://www.chilliant.com/rgb2hsv.html
    {precision}4 K = {precision}4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    {precision}4 P = lerp({precision}4(rgb.bg, K.wz), {precision}4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    {precision}4 Q = lerp({precision}4(P.xyw, rgb.r), {precision}4(rgb.r, P.yzx), step(P.x, rgb.r));
    {precision} D = Q.x - min(Q.w, Q.y);
    {precision}  E = 1e-10;
    hsv = {precision}3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}
";
        }
    }
}
