using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Cosine")]
    class CosineNode : CodeFunctionNode
    {
        public CosineNode()
        {
            name = "Cosine";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Cosine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Cosine(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = cos(In);
}
";
        }
    }
}
