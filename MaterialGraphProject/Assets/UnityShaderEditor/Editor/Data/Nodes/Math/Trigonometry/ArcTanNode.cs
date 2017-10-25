using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcTan")]
    public class ArcTanNode : CodeFunctionNode
    {
        public ArcTanNode()
        {
            name = "ArcTan";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ArcTan", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ArcTan(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = atan(In);
}
";
        }
    }
}
