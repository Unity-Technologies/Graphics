using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arctangent")]
    class ArctangentNode : CodeFunctionNode
    {
        public ArctangentNode()
        {
            name = "Arctangent";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Arctangent", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Arctangent(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = atan(In);
}
";
        }
    }
}
