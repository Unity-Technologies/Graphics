using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Derivative", "DDY")]
    class DDYNode : CodeFunctionNode
    {
        public DDYNode()
        {
            name = "DDY";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDY", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDY(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = ddy(In);
}
";
        }
    }
}
