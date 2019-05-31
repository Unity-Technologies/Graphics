using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arcsine")]
    class ArcsineNode : CodeFunctionNode
    {
        public ArcsineNode()
        {
            name = "Arcsine";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Arcsine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Arcsine(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = asin(In);
}
";
        }
    }
}
