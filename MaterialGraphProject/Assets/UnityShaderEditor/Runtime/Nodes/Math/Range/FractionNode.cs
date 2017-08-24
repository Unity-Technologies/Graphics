using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Fraction")]
    public class FractionNode : CodeFunctionNode
    {
        public FractionNode()
        {
            name = "Fraction";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Frac", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Frac(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = frac(argument);
}
";
        }
    }
}
