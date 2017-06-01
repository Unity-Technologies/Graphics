using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/Multiply")]
    public class MultiplyNode : CodeFunctionNode
    {
        public MultiplyNode()
        {
            name = "Multiply";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Multiply", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Multiply(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = first * second;
}
";
        }
    }
}
