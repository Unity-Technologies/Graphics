using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Reciprocal")]
    public class ReciprocalNode : CodeFunctionNode
    {
        public ReciprocalNode()
        {
            name = "Reciprocal";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Reciprocal", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Reciprocal(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = 1.0/argument;
}
";
        }
    }
}
