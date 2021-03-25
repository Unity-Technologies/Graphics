using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Degrees To Radians")]
    class DegreesToRadiansNode : CodeFunctionNode
    {
        public DegreesToRadiansNode()
        {
            name = "Degrees To Radians";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DegreesToRadians", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DegreesToRadians(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = radians(In);
}
";
        }
    }
}
