using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Trigonometry/ArcTan2")]
    public class ArcTan2Node : CodeFunctionNode
    {
        public ArcTan2Node()
        {
            name = "ArcTan2";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ArcTan2", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ArcTan2(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = atan2(A, B);
}
";
        }
    }
}
