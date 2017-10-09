using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Radians To Degrees")]
    public class RadiansToDegreesNode : CodeFunctionNode
    {
        public RadiansToDegreesNode()
        {
            name = "RadiansToDegrees";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RadiansToDegrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RadiansToDegrees(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = degrees(argument);
}
";
        }
    }
}
