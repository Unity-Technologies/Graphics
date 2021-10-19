using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arctangent2")]
    class Arctangent2Node : CodeFunctionNode
    {
        public Arctangent2Node()
        {
            name = "Arctangent2";
            synonyms = new string[] { "atan2" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Arctangent2", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Arctangent2(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = atan2(A, B);
}
";
        }
    }
}
