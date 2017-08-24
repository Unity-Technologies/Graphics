using System;
using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Fmod")]
    public class FmodNode : CodeFunctionNode
    {
        public FmodNode()
        {
            name = "Fmod";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Fmod", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Fmod(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = fmod(first, second);
}
";
        }
    }
}
