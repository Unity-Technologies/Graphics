using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Posterize")]
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
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] DynamicDimensionVector Steps,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = floor(In / (1 / Steps)) * (1 / Steps);
}
";
        }
    }
}
