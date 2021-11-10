using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Derivative", "DDX")]
    class DDXNode : CodeFunctionNode
    {
        public DDXNode()
        {
            name = "DDX";
            synonyms = new string[] { "derivative" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDX", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        string Unity_DDX(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
$@"
{{
    {GetRayTracingError()}
    Out = ddx(In);
}}
";
        }
    }
}
