using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Hyperbolic Tan")]
    class HyperbolicTanNode : CodeFunctionNode
    {
        public HyperbolicTanNode()
        {
            name = "Hyperbolic Tan";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HyperbolicTan", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HyperbolicTan(
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
