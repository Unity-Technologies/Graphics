using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Advanced/Posterize")]
    class PosterizeNode : CodeFunctionNode
    {
        public PosterizeNode()
        {
            name = "Posterize";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Posterize", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Posterize(
            [Slot(0, Binding.None)] DynamicDimensionVector input,
            [Slot(1, Binding.None)] DynamicDimensionVector stepsize,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = floor(input / stepsize) * stepsize;;
}
";
        }
    }
}
