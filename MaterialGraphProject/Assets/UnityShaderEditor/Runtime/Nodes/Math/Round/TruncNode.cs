using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Truncate")]
    public class TruncateNode : CodeFunctionNode
    {
        public TruncateNode()
        {
            name = "Truncate";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Truncate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Truncate(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = truncate(argument);
}
";
        }
    }
}
