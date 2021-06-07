using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arccosine")]
    class ArccosineNode : CodeFunctionNode
    {
        public ArccosineNode()
        {
            name = "Arccosine";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Arccosine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Arccosine(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = acos(In);
}
";
        }
    }
}
