using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Inverse Lerp")]
    class InverseLerpNode : CodeFunctionNode
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
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector B,
            [Slot(2, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector T,
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
