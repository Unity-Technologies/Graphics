using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Trigonometry/ArcTan")]
    public class ATanNode : CodeFunctionNode
    {
        public ATanNode()
        {
            name = "ArcTan";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ATan", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ATan(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = atan(argument);
}
";
        }
    }
}
