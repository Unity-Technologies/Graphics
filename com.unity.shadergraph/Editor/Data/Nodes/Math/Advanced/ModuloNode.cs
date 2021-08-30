using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Modulo")]
    class ModuloNode : CodeFunctionNode
    {
        public ModuloNode()
        {
            name = "Modulo";
            synonyms = new string[] { "fmod" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Modulo", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Modulo(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = fmod(A, B);
}
";
        }
    }
}
