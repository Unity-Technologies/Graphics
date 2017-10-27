using System.Reflection;

namespace UnityEditor.ShaderGraph
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
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1.0/In;
}
";
        }
    }
}
