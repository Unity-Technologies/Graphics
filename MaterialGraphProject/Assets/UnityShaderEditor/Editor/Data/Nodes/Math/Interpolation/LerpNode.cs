using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Interpolation/Lerp")]
    public class LerpNode : CodeFunctionNode
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
            [Slot(0, Binding.None)] DynamicDimensionVector inputA,
            [Slot(1, Binding.None)] DynamicDimensionVector inputB,
            [Slot(2, Binding.None)] DynamicDimensionVector t,
            [Slot(3, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = lerp(inputA, inputB, t);
}";
        }
    }
}
