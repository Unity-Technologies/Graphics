using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcTan2")]
    public class ATan2Node : CodeFunctionNode
    {
        public ATan2Node()
        {
            name = "ArcTan2";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ATan2", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ATan2(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = atan2(first, second);
}
";
        }
    }
}
