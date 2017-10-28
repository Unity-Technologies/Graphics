using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Range/Minimum")]
    public class MinimumNode : CodeFunctionNode
    {
        public MinimumNode()
        {
            name = "Minimum";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Min", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Min(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = min(first, second);
};";
        }
    }
}
