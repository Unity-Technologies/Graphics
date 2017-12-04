using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Smoothstep")]
    class SmoothstepNode : CodeFunctionNode
    {
        public SmoothstepNode()
        {
            name = "Smoothstep";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Smoothstep", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Smoothstep(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] DynamicDimensionVector T,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = smoothstep(A, B, T);
}";
        }
    }
}
