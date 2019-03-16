using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "Contrast")]
    class ContrastNode : CodeFunctionNode
    {
        public ContrastNode()
        {
            name = "Contrast";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Contrast", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Contrast(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Vector1 Contrast,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    {precision} midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}";
        }
    }
}
