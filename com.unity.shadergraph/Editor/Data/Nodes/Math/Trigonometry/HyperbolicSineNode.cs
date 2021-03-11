using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Hyperbolic Sine")]
    class HyperbolicSineNode : CodeFunctionNode
    {
        public HyperbolicSineNode()
        {
            name = "Hyperbolic Sine";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HyperbolicSine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HyperbolicSine(
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
