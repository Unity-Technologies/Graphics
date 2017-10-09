using System.Reflection;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/Twist")]
    public class TwistNode : CodeFunctionNode
    {
        public TwistNode()
        {
            name = "Twist";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Twist", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Twist(
            [Slot(0, Binding.None)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 twist,
            [Slot(2, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;

            return
                @"
{
    {precision} angle = twist * length(uv);
    {precision} x = cos(angle) * uv.x - sin(angle) * uv.y;
    {precision} y = sin(angle) * uv.x + cos(angle) * uv.y;
    result = float2(x, y);
}
";
        }
    }
}
