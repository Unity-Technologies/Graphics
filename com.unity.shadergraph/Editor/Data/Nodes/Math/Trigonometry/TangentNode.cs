using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Tangent")]
    class TangentNode : CodeFunctionNode
    {
        public TangentNode()
        {
            name = "Tangent";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Tangent", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Tangent(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = tan(In);
}
";
        }
    }
}
