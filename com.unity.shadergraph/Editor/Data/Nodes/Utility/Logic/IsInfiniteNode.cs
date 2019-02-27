using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Is Infinite")]
    class IsInfiniteNode : CodeFunctionNode
    {
        public IsInfiniteNode()
        {
            name = "Is Infinite";
        }


        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_IsInfinite", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_IsInfinite(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = isinf(In);
}
";
        }
    }
}
