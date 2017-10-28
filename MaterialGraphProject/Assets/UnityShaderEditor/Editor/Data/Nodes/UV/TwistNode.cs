using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("UV/Twirl")]
    public class TwistNode : CodeFunctionNode
    {
        public TwistNode()
        {
            name = "Twirl";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Twist", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Twist(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector1 strength,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 center,
            [Slot(3, Binding.None)] Vector2 offset,
            [Slot(4, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;

            return
                @"
{


    float2 delta = uv - center;
    {precision} angle = strength * length(delta);
    {precision} x = cos(angle) * delta.x - sin(angle) * delta.y;
    {precision} y = sin(angle) * delta.x + cos(angle) * delta.y;
    result = float2(x + center.x + offset.x, y + center.y + offset.y);
}
";
        }
    }
}
