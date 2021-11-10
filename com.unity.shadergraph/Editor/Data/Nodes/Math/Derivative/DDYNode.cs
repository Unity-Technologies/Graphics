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
            synonyms = new string[] { "derivative" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDY", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        string Unity_DDY(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
$@"
{{
    {GetRayTracingError()}
    Out = ddy(In);
}}
";
        }
    }
}
