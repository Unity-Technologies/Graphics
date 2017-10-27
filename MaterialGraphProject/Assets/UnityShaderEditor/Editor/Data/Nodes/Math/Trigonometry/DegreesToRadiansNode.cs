using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Trigonometry/Degrees To Radians")]
    public class DegreesToRadiansNode : CodeFunctionNode
    {
        public DegreesToRadiansNode()
        {
            name = "DegreesToRadians";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DegreesToRadians", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DegreesToRadians(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = radians(argument);
}
";
        }
    }
}
