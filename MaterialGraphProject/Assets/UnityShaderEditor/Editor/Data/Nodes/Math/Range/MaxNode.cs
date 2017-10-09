using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Maximum")]
    public class MaximumNode : CodeFunctionNode
    {
        public MaximumNode()
        {
            name = "Maximum";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Max", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Max(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = max(first, second);
}
";
        }
    }
}
