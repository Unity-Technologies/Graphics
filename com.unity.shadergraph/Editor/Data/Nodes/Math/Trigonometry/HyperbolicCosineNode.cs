using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Hyperbolic Cosine")]
    class HyperbolicCosineNode : CodeFunctionNode
    {
        public HyperbolicCosineNode()
        {
            name = "Hyperbolic Cosine";
            synonyms = new string[] { "cosh" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HyperbolicCosine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HyperbolicCosine(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = sinh(In);
}
";
        }
    }
}
