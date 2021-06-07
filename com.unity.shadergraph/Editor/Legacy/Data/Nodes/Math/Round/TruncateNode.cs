using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Truncate")]
    class TruncateNode : CodeFunctionNode
    {
        public TruncateNode()
        {
            name = "Truncate";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Truncate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Truncate(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = trunc(In);
}
";
        }
    }
}
