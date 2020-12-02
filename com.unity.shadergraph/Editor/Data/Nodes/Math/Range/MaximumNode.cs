using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Maximum")]
    class MaximumNode : CodeFunctionNode
    {
        public MaximumNode()
        {
            name = "Maximum";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Maximum", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Maximum(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = max(A, B);
}
";
        }
    }
}
