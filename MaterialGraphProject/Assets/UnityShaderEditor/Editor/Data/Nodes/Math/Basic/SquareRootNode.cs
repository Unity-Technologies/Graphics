using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Basic/SquareRoot")]
    public class SquareRootNode : CodeFunctionNode
    {
        public SquareRootNode()
        {
            name = "SquareRoot";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Sqrt", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Sqrt(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = sqrt(argument);
}
";
        }
    }
}
