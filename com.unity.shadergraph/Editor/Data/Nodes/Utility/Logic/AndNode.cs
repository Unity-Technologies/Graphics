using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "And")]
    class AndNode : CodeFunctionNode
    {
        public AndNode()
        {
            name = "And";
        }


        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_And", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_And(
            [Slot(0, Binding.None)] Boolean A,
            [Slot(1, Binding.None)] Boolean B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A && B;
}
";
        }
    }
}
