using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/Exponential")]
    public class ExponentialNode : CodeFunctionNode
    {
        public ExponentialNode()
        {
            name = "Exponential";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Exp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Exp(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = exp(argument);
}
";
        }
    }
}
