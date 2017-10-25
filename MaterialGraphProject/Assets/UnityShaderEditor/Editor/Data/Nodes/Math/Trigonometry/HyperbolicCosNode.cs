using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Hyperbolic Cos")]
    class HyperbolicCosNode : CodeFunctionNode
    {
        public HyperbolicCosNode()
        {
            name = "Hyperbolic Cos";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HyperbolicCos", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HyperbolicCos(
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
