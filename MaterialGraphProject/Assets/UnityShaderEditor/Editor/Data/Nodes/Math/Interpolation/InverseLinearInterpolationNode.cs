using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Interpolation/Inverse Linear Interpolation")]
    public class InverseLerpNode : CodeFunctionNode
    {
        public InverseLerpNode()
        {
            name = "Inverse Linear Interpolation";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_InverseLinearInterpolation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_InverseLinearInterpolation(
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
