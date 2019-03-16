using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Radians To Degrees")]
    class RadiansToDegreesNode : CodeFunctionNode
    {
        public RadiansToDegreesNode()
        {
            name = "Radians To Degrees";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RadiansToDegrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RadiansToDegrees(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = degrees(In);
}
";
        }
    }
}
