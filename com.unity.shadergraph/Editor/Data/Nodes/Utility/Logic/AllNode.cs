using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "All")]
    class AllNode : CodeFunctionNode
    {
        public AllNode()
        {
            name = "All";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_All", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_All(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
@"
{
    Out = all(In);
}
";
        }
    }
}
