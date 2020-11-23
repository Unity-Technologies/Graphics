using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Saturate")]
    class SaturateNode : CodeFunctionNode
    {
        public SaturateNode()
        {
            name = "Saturate";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Saturate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Saturate(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = saturate(In);
}
";
        }
    }
}
