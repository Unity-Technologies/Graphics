using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Is Infinite")]
    public class IsInfiniteNode : CodeFunctionNode
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
            [Slot(0, Binding.None)] DynamicDimensionVector In,
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
