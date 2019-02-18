using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "One Minus")]
    class OneMinusNode : CodeFunctionNode
    {
        public OneMinusNode()
        {
            name = "One Minus";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_OneMinus", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_OneMinus(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1 - In;
}
";
        }
    }
}
