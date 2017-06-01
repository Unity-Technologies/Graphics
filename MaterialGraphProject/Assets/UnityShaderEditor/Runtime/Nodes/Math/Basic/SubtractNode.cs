using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/Subtract")]
    class SubtractNode : CodeFunctionNode
    {
        public SubtractNode()
        {
            name = "Subtract";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Subtract", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Subtract(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = first - second;
}
";
        }
    }
}
