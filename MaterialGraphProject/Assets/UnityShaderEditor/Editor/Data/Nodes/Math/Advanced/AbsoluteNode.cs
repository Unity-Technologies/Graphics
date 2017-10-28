using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Advanced/Absolute")]
    public class AbsoluteNode : CodeFunctionNode
    {
        public AbsoluteNode()
        {
            name = "Absolute";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Absolute", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Absolute(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = abs(argument);
}
";
        }
    }
}
