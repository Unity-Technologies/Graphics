using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Trigonometry/Hyperbolic Sin")]
    class HyperbolicSinNode : CodeFunctionNode
    {
        public HyperbolicSinNode()
        {
            name = "Hyperbolic Sin";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HyperbolicSin", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HyperbolicSin(
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
