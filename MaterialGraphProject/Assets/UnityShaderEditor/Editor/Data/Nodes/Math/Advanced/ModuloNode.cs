using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Modulo")]
    public class ModuloNode : CodeFunctionNode
    {
        public ModuloNode()
        {
            name = "Modulo";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Modulo", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Modulo(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
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
