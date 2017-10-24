using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Interpolation/Linear Interpolation")]
    public class LinearInterpolationNode : CodeFunctionNode
    {
        public LinearInterpolationNode()
        {
            name = "Linear Interpolation";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_LinearInterpolation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_LinearInterpolation(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] DynamicDimensionVector T,
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
