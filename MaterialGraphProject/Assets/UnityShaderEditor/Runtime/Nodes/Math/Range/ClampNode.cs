using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/Clamp")]
    public class ClampNode : CodeFunctionNode
    {
        public ClampNode()
        {
            name = "Clamp";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Smoothstep", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Smoothstep(
            [Slot(0, Binding.None)] DynamicDimensionVector input,
            [Slot(1, Binding.None)] DynamicDimensionVector min,
            [Slot(2, Binding.None)] DynamicDimensionVector max,
            [Slot(3, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = clamp(input, min, max);
}";
        }
    }
}
