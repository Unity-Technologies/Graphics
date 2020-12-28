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
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDXY", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDXY(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = abs(ddx(In)) + abs(ddy(In));
}
";
        }
    }
}
