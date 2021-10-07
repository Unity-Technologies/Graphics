using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Square Root")]
    class SquareRootNode : CodeFunctionNode
    {
        public SquareRootNode()
        {
            name = "Square Root";
            synonyms = new string[] { "sqrt" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SquareRoot", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SquareRoot(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = sqrt(In);
}
";
        }
    }
}
