using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Sine")]
    class SineNode : CodeFunctionNode
    {
        public SineNode()
        {
            name = "Sine";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Sine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Sine(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = sin(In);
}
";
        }
    }
}
