using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Normalize")]
    class NormalizeNode : CodeFunctionNode
    {
        public NormalizeNode()
        {
            name = "Normalize";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Normalize", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Normalize(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = normalize(In);
}
";
        }
    }
}
