using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Trigonometry/Tan")]
    public class TanNode : CodeFunctionNode
    {
        public TanNode()
        {
            name = "Tan";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Tan", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Tan(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = tan(argument);
}
";
        }
    }
}
