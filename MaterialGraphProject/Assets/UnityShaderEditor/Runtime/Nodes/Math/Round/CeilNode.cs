using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Ceil")]
    public class CeilNode : CodeFunctionNode
    {
        public CeilNode()
        {
            name = "Ceil";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Ceil", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Ceil(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = ceil(argument);
}
";
        }
    }
}
