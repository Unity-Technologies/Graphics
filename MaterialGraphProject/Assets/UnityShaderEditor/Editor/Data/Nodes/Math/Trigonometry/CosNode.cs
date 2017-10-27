using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Trigonometry/Cos")]
    public class CosNode : CodeFunctionNode
    {
        public CosNode()
        {
            name = "Cos";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Cos", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Cos(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = cos(In);
}
";
        }
    }
}
