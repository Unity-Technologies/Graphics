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
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector1 Strength,
            [Slot(2, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(3, Binding.None)] Vector2 Offset,
            [Slot(4, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;

            return
                @"
{


    float2 delta = UV - Center;
    {precision} angle = Strength * length(delta);
    {precision} x = cos(angle) * delta.x - sin(angle) * delta.y;
    {precision} y = sin(angle) * delta.x + cos(angle) * delta.y;
    Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
}
";
        }
    }
}
