using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Sign")]
    public class SignNode : CodeFunctionNode
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
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = sign(argument);
}
";
        }
    }
}
