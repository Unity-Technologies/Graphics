using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Is NaN")]
    class IsNanNode : CodeFunctionNode
    {
        public IsNanNode()
        {
            name = "Is NaN";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_IsNaN", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_IsNaN(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
@"
{
    Out = isnan(In) ? 1 : 0;
}
";
        }
    }
}
