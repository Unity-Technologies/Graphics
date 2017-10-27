using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Trigonometry/ArcCos")]
    public class ArcCosNode : CodeFunctionNode
    {
        public ArcCosNode()
        {
            name = "ArcCos";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ArcCos", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ArcCos(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = acos(In);
}
";
        }
    }
}
