using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Inverse Lerp")]
    public class InverseLerpNode : CodeFunctionNode
    {
        public InverseLerpNode()
        {
            name = "Inverse Lerp";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_InverseLerp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_InverseLerp(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] DynamicDimensionVector T,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = (T - A)/(B - A);
}";
        }
    }
}
