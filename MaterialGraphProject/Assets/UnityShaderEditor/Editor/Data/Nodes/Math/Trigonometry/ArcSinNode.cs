using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcSin")]
    public class ArcSinNode : CodeFunctionNode
    {
        public ArcSinNode()
        {
            name = "ArcSin";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ArcSin", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ArcSin(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = asin(In);
}
";
        }
    }
}
