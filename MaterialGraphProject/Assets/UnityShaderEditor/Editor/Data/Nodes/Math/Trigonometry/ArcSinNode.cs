using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcSin")]
    public class ASinNode : CodeFunctionNode
    {
        public ASinNode()
        {
            name = "ArcSin";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ASin", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ASin(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = asin(argument);
}
";
        }
    }
}
