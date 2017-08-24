using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Negate")]
    public class NegateNode : CodeFunctionNode
    {
        public NegateNode()
        {
            name = "Negate";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Negate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Negate(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = -1 * argument;
}
";
        }
    }
}
