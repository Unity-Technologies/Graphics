using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Negate")]
    class NegateNode : CodeFunctionNode
    {
        public NegateNode()
        {
            name = "Negate";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Negate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Negate(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = -1 * In;
}
";
        }
    }
}
