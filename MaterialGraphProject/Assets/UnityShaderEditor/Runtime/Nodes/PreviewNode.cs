using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Preview Node")]
    public class PreviewNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return true; } }

        public PreviewNode()
        {
            name = "Preview";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Preview", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Preview(
            [Slot(0, Binding.None)] DynamicDimensionVector input,
            [Slot(1, Binding.None)] out DynamicDimensionVector output)
        {
            return
                @"
{
    output = input;
}
";
        }
    }
}
