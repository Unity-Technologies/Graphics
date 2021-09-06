using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Length")]
    class LengthNode : CodeFunctionNode
    {
        public LengthNode()
        {
            name = "Length";
            synonyms = new string[] { "measure" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Length", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Length(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
@"
{
    Out = length(In);
}
";
        }
    }
}
