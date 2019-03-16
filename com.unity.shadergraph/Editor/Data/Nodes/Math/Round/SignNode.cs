using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Sign")]
    class SignNode : CodeFunctionNode
    {
        public SignNode()
        {
            name = "Sign";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Sign", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Sign(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = sign(In);
}
";
        }
    }
}
