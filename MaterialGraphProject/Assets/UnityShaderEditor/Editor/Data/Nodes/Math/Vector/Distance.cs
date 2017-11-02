using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Vector/Distance")]
    public class DistanceNode : CodeFunctionNode
    {
        public DistanceNode()
        {
            name = "Distance";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Distance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Distance(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = distance(first, second);
}
";
        }
    }
}
