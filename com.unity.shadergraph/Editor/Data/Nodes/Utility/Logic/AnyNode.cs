using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Any")]
    class AnyNode : CodeFunctionNode
    {
        public AnyNode()
        {
            name = "Any";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        static string Unity_Any(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = any(In);
}
";
        }
    }
}
