using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/Divide")]
    public class DivNode : CodeFunctionNode
    {
        public DivNode()
        {
            name = "Divide";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Div", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Div(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return @"
{
    result = first / second;
}
";
        }
    }
}
