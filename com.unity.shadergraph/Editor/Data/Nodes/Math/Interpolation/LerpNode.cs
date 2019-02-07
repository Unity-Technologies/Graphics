using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Lerp")]
    class LerpNode : CodeFunctionNode
    {
        public LerpNode()
        {
            name = "Lerp";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Lerp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Lerp(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector B,
            [Slot(2, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector T,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = lerp(A, B, T);
}";
        }
    }
}
