using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Interpolation/InverseLerp")]
    public class InverseLerpNode : CodeFunctionNode
    {
        public InverseLerpNode()
        {
            name = "InverseLerp";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_InverseLerp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_InverseLerp(
            [Slot(0, Binding.None)] DynamicDimensionVector inputA,
            [Slot(1, Binding.None)] DynamicDimensionVector inputB,
            [Slot(2, Binding.None)] DynamicDimensionVector t,
            [Slot(3, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = (t - inputA)/(inputB - inputA);
}";
        }
    }
}
