using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/OneMinus")]
    public class OneMinusNode : CodeFunctionNode
    {
        public OneMinusNode()
        {
            name = "OneMinus";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_OneMinus", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_OneMinus(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = argument * -1 + 1;;
}
";
        }
    }
}
