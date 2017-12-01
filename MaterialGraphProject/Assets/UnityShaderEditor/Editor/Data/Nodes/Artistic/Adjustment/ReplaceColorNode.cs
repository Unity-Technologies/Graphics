using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic/Adjustment/Replace Color")]
    public class ReplaceColorNode : CodeFunctionNode
    {
        public ReplaceColorNode()
        {
            name = "Replace Color";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ReplaceColor", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ReplaceColor(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Color From,
            [Slot(2, Binding.None)] Color To,
            [Slot(3, Binding.None)] Vector1 Range,
            [Slot(4, Binding.None)] out Vector3 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    {precision}3 col = In;
    {precision} Distance = distance(From, In);
    if(Distance <= Range)
        col = To;
    Out = col;
}";
        }
    }
}
