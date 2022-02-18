using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Derivative", "DDXY")]
    class DDXYNode : CodeFunctionNode
    {
        public DDXYNode()
        {
            name = "DDXY";
            synonyms = new string[] { "derivative" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDXY", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        string Unity_DDXY(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
$@"
{{
    {GetRayTracingError()}
    Out = abs(ddx(In)) + abs(ddy(In));
}}
";
        }
    }
}
