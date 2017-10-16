using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Reciprocal Square Root")]
    public class ReciprocalSqrtNode : CodeFunctionNode
    {
        public ReciprocalSqrtNode()
        {
            name = "ReciprocalSquareRoot";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Rsqrt", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rsqrt(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = rsqrt(argument);
}
";
        }
    }
}
