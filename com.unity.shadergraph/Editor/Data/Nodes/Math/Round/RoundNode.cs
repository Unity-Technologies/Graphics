using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Round")]
    class RoundNode : CodeFunctionNode
    {
        public RoundNode()
        {
            name = "Round";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Round", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Round(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = round(In);
}
";
        }
    }
}
