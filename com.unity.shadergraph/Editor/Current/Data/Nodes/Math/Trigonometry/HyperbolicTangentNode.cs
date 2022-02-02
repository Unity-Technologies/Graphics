using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Hyperbolic Tangent")]
    class HyperbolicTangentNode : CodeFunctionNode
    {
        public HyperbolicTangentNode()
        {
            name = "Hyperbolic Tangent";
            synonyms = new string[] { "tanh" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HyperbolicTangent", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HyperbolicTangent(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = tanh(In);
}
";
        }
    }
}
