using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Interpolation/SmoothStep")]
    class SmoothStepNode : CodeFunctionNode
    {
        public SmoothStepNode()
        {
            name = "SmoothStep";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Smoothstep", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Smoothstep(
            [Slot(0, Binding.None)] DynamicDimensionVector inputA,
            [Slot(1, Binding.None)] DynamicDimensionVector inputB,
            [Slot(2, Binding.None)] DynamicDimensionVector t,
            [Slot(3, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = smoothstep(inputA, inputB, t);
}";
        }
    }
}
